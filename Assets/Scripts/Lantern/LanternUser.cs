using System;
using UnityEngine;

namespace DustScrapper.Lantern
{
    /// <summary>
    /// 랜턴 키(Space)를 받아 무엇을 할지 정한다. 랜턴이 있으면 배터리를 써서 그 랜턴의 효과를 낸다.
    /// 랜턴 칸이 비어 있거나, 랜턴은 꼈는데 배터리가 없거나 다 떨어졌으면 아무것도 하지 않는다(기획).
    /// 두뇌는 "Space를 눌렀다"만 알리고, 무엇을 할지는 여기서 정해서 랜턴 종류가 늘어도 두뇌는 그대로다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LanternUser : MonoBehaviour
    {
        [SerializeField] LanternSlots slots;

        /// <summary>랜턴 효과를 냈을 때(배터리가 닳은 뒤). 연출·소리가 듣는다.</summary>
        public event Action<LanternData> Used;

        /// <summary>랜턴은 꼈는데 배터리가 없어 아무것도 못 했을 때. 연출·소리가 듣는다.</summary>
        public event Action<LanternData> OutOfBattery;

        void Awake()
        {
            if (slots == null) slots = GetComponent<LanternSlots>();
        }

        /// <summary>Space를 눌렀다.</summary>
        public void Press()
        {
            LanternData lantern = slots != null ? slots.Lantern : null;
            if (lantern == null || lantern.Effect == null) return;
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
