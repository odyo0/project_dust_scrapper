using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Equipment
{
    /// <summary>
    /// 몸에 입는 장비(모자, 상의, 하의, 망토, 신발, 악세서리, 가방). 끼는 부위와 능력치 보너스 목록을 가진다.
    /// 주워서 들고 다니는 물건이라 ItemData를 이어받는다(이름·가치·임시 색은 거기에, 겹쳐 담지 않음).
    /// 장비를 늘릴 때는 코드가 아니라 이 데이터 파일을 새로 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Items/Body Gear", fileName = "BodyGear")]
    public sealed class BodyGearData : ItemData
    {
        [SerializeField] BodySlot slot;
        [Tooltip("끼고 있는 동안 올려 주는 능력치")]
        [SerializeField] StatModifier[] modifiers;

        public override int MaxStack => 1;
        public BodySlot Slot => slot;
        public StatModifier[] Modifiers => modifiers;

        /// <summary>이 장비가 그 능력치에 더하는 양의 합.</summary>
        public float Sum(StatType stat)
        {
            float total = 0f;
            if (modifiers == null) return total;
            foreach (StatModifier modifier in modifiers)
            {
                if (modifier.Stat == stat) total += modifier.Amount;
            }
            return total;
        }
    }
}
