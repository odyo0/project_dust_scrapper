using System;
using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.Lantern
{
    /// <summary>
    /// 랜턴 키(Space)를 받아 무엇을 할지 정한다. 랜턴 칸이 비어 있으면 구르기, 랜턴이 있으면 배터리를 써서 그 랜턴의 효과.
    /// 랜턴은 꼈는데 배터리가 없거나 다 떨어졌으면 아무것도 하지 않는다(구르기로 돌아가지 않는다 — 기획).
    /// 두뇌는 "Space를 눌렀다"만 알리고, 어느 쪽인지는 여기서 정해서 랜턴 종류가 늘어도 두뇌는 그대로다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LanternUser : MonoBehaviour
    {
        [SerializeField] LanternSlots slots;
        [SerializeField] DodgeRoll dodge;

        /// <summary>랜턴 효과를 냈을 때(배터리가 닳은 뒤). 연출·소리가 듣는다.</summary>
        public event Action<LanternData> Used;

        /// <summary>랜턴은 꼈는데 배터리가 없어 아무것도 못 했을 때. 연출·소리가 듣는다.</summary>
        public event Action<LanternData> OutOfBattery;

        void Awake()
        {
            if (slots == null) slots = GetComponent<LanternSlots>();
            if (dodge == null) dodge = GetComponent<DodgeRoll>();
        }

        /// <summary>Space를 눌렀다. 구르게 되면 rollDirection 쪽으로 구른다.</summary>
        public void Press(Vector2 rollDirection)
        {
            LanternData lantern = slots != null ? slots.Lantern : null;
            if (lantern == null)
            {
                if (dodge != null) dodge.TryRoll(rollDirection);
                return;
            }

            if (lantern.Effect == null) return;
            if (!slots.PrepareBattery(lantern.ChargePerUse))
            {
                OutOfBattery?.Invoke(lantern);
                return;
            }
            if (!lantern.Effect.Activate(gameObject)) return;
            slots.Consume(lantern.ChargePerUse);
            Used?.Invoke(lantern);
        }
    }
}
