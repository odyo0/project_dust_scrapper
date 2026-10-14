using System.Collections.Generic;
using DustScrapper.Equipment;
using DustScrapper.Interaction;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 기지의 충전대(시설). 닳은 마법진 장비를 정제된 글로우로 새것처럼 되돌린다.
    /// 몸에 낀 것·가방·보관함 어디에 있든 충전할 수 있고(장비를 옮길 필요 없음), 글로우는 가방에서 먼저, 모자라면 보관함에서 꺼낸다.
    /// 드는 개수는 장비 데이터(BodyGearData.ChargeCostOf: 닳은 비율만큼)에 있다.
    /// </summary>
    public sealed class ChargeStation : Station
    {
        /// <summary>충전할 수 있는 장비 하나의 자리: 어느 칸 묶음의 몇 번 칸.</summary>
        public readonly struct GearSlot
        {
            public GearSlot(IItemSlots slots, int index)
            {
                Slots = slots;
                Index = index;
            }

            public IItemSlots Slots { get; }
            public int Index { get; }
            public ItemStack Stack => Slots.GetSlot(Index);
            public BodyGearData Gear => Stack.Item as BodyGearData;
        }

        [SerializeField] string displayName = "충전대";

        [Tooltip("충전에 쓰는 물건 (정제된 글로우)")]
        [SerializeField] ItemData chargeMaterial;

        public ItemData ChargeMaterial => chargeMaterial;

        public override string ActionName => "충전";
        public override string TargetName => displayName;

        public override string BlockedReason(PlayerInteractor interactor) => chargeMaterial == null ? "충전 재료가 정해지지 않았다" : null;

        /// <summary>플레이어 몸·가방과 보관함에 있는, 내구도가 있는 장비 자리들.</summary>
        public List<GearSlot> FindGear(PlayerInteractor player)
        {
            var found = new List<GearSlot>();
            Collect(player != null ? player.Body : null, found);
            Collect(player != null ? player.Inventory : null, found);
            Collect(Storage, found);
            return found;
        }

        /// <summary>가방 + 보관함에 있는 충전 재료 개수.</summary>
        public int MaterialCount(Inventory bag) =>
            (bag != null ? bag.CountOf(chargeMaterial) : 0) + (Storage != null ? Storage.CountOf(chargeMaterial) : 0);

        /// <summary>그 장비를 새것으로 충전한다. 못 하면 이유를, 했으면 null.</summary>
        public string TryCharge(GearSlot slot, Inventory bag)
        {
            BodyGearData gear = slot.Gear;
            if (gear == null || !gear.HasDurability) return "충전할 수 없는 물건이다";
            ItemStack stack = slot.Stack;
            int cost = gear.ChargeCostOf(stack);
            if (cost <= 0) return "닳지 않았다";
            if (MaterialCount(bag) < cost) return $"{chargeMaterial.DisplayName}이(가) 모자란다";

            int taken = bag != null ? bag.Remove(chargeMaterial, cost) : 0;
            if (taken < cost && Storage != null) Storage.Remove(chargeMaterial, cost - taken);

            // 칸에서 빼고 새것으로 다시 놓는다. 몸 장비 칸이면 놓는 순간 보너스가 다시 계산된다.
            slot.Slots.TakeSlot(slot.Index);
            slot.Slots.PlaceInEmptySlot(slot.Index, stack.WithWear(0f));
            return null;
        }

        static void Collect(IItemSlots slots, List<GearSlot> into)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.SlotCount; i++)
            {
                if (slots.GetSlot(i).Item is BodyGearData gear && gear.HasDurability) into.Add(new GearSlot(slots, i));
            }
        }
    }
}
