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

        [Tooltip("마법진 내구도. MP를 올려 주는 장비는 마법을 쓸수록 닳고, 다 닳으면 보너스가 사라진다. 0이면 닳지 않는다.")]
        [SerializeField, Min(0f)] float maxDurability;

        [Tooltip("다 닳은 것을 새것으로 충전하는 데 드는 정제된 글로우 개수. 덜 닳았으면 닳은 비율만큼 든다(최소 1).")]
        [SerializeField, Min(0)] int fullChargeCost = 1;

        public override int MaxStack => 1;
        public float MaxDurability => maxDurability;
        public bool HasDurability => maxDurability > 0f;

        /// <summary>남은 내구도.</summary>
        public float DurabilityOf(ItemStack stack) => HasDurability ? Mathf.Max(0f, maxDurability - stack.Wear) : 0f;

        public override bool TryGetGauge(ItemStack stack, out float ratio)
        {
            ratio = HasDurability ? DurabilityOf(stack) / maxDurability : 0f;
            return HasDurability;
        }

        /// <summary>다 닳아서 보너스를 주지 못하는가.</summary>
        public bool IsBroken(ItemStack stack) => HasDurability && stack.Wear >= maxDurability;

        /// <summary>이 묶음을 새것으로 충전하는 데 드는 정제된 글로우 개수. 닳지 않았으면 0.</summary>
        public int ChargeCostOf(ItemStack stack)
        {
            if (!HasDurability || stack.Wear <= 0f) return 0;
            float ratio = Mathf.Clamp01(stack.Wear / maxDurability);
            return Mathf.Max(1, Mathf.CeilToInt(fullChargeCost * ratio));
        }
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
