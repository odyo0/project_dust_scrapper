using System;
using UnityEngine;

namespace DustScrapper.Equipment
{
    /// <summary>장비가 올려 주는 능력치 종류. 능력치가 늘면 여기에 하나 더하고, 그 능력치를 쓰는 쪽에서 받는다.</summary>
    public enum StatType
    {
        /// <summary>최대 MP</summary>
        MaxMana,
        /// <summary>MP 회복 속도 (1초에 차는 양)</summary>
        ManaRegen,
        /// <summary>가방 칸 수</summary>
        BagSlots,
    }

    /// <summary>
    /// 장비의 능력치 보너스 한 줄: 어떤 능력치를 얼마나. 지금은 더하기만 있다(기획: 더하기·배율 둘 다 만들되 일단 더하기만).
    /// 배율이 생기면 여기에 "더하기/배율" 칸을 두고, 모으는 쪽(BodyEquipment)에서 더하기를 먼저 더한 뒤 배율을 곱한다.
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        [SerializeField] StatType stat;
        [Tooltip("더하는 양")]
        [SerializeField] float amount;

        public StatType Stat => stat;
        public float Amount => amount;
    }
}
