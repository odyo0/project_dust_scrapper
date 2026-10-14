using DustScrapper.Combat;
using DustScrapper.Equipment;
using DustScrapper.Glow;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 플레이어가 소모품을 쓴다: 어느 칸(단축 칸, 가방)의 소모품이든 효과를 내고 하나 줄인다.
    /// 급한 충전: 소모품을 마법진 장비에 쓰면 내구도를 채우고 글로우병이 오른다.
    /// 어떤 효과인지는 소모품 데이터가 정하고, 여기서는 "쓰고 줄이기"만 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemUser : MonoBehaviour
    {
        [Tooltip("비워 두면 같은 물체에서 찾는다. 죽어 있으면 쓰지 못한다.")]
        [SerializeField] Health health;

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
        }

        bool CanAct => isActiveAndEnabled && (health == null || !health.IsDead);

        /// <summary>그 칸의 소모품을 하나 쓴다. 썼으면 true.</summary>
        public bool Use(IItemSlots slots, int index)
        {
            if (!CanAct || slots == null || index < 0 || index >= slots.SlotCount) return false;
            ItemStack stack = slots.GetSlot(index);
            if (!(stack.Item is ConsumableData consumable) || consumable.Effects == null) return false;

            bool applied = false;
            foreach (var effect in consumable.Effects)
            {
                if (effect != null && effect.Apply(gameObject)) applied = true;
            }
            if (!applied) return false;

            ConsumeOne(slots, index);
            return true;
        }

        /// <summary>
        /// 소모품(from 칸)을 마법진 장비(gear 칸)에 써서 내구도를 채운다. 새것이거나 쓸 수 없는 물건이면 false.
        /// </summary>
        public bool ChargeGear(IItemSlots from, int fromIndex, IItemSlots gearSlots, int gearIndex)
        {
            if (!CanAct) return false;
            ItemStack item = from.GetSlot(fromIndex);
            ItemStack gear = gearSlots.GetSlot(gearIndex);
            if (!(item.Item is ConsumableData consumable) || !consumable.CanChargeGear) return false;
            if (!(gear.Item is BodyGearData gearData) || !gearData.HasDurability || gear.Wear <= 0f) return false;

            // 같은 칸 묶음 안에서 앞 칸을 먼저 바꾸면 뒤 칸 번호가 그대로라 순서는 상관없다(칸 수가 바뀌지 않음).
            gearSlots.TakeSlot(gearIndex);
            gearSlots.PlaceInEmptySlot(gearIndex, gear.WithWear(gear.Wear - consumable.ChargeDurability));
            if (TryGetComponent(out GlowSickness sickness)) sickness.Add(consumable.ChargeSickness);
            ConsumeOne(from, fromIndex);
            return true;
        }

        static void ConsumeOne(IItemSlots slots, int index)
        {
            ItemStack stack = slots.TakeSlot(index);
            if (stack.Count > 1) slots.PlaceInEmptySlot(index, stack.WithCount(stack.Count - 1));
        }
    }
}
