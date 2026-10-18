using System;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Lantern
{
    /// <summary>
    /// 랜턴 칸과 그 옆의 랜턴 배터리 칸(칸 2개). PlayerGear 목록에 들어가서 줍기·빠른 옮기기·저장·판 결과에 저절로 붙는다.
    /// 배터리를 쓰는 일도 여기서 한다: 쓴 만큼 닳게 하고, 다 쓰면 산화된 배터리로 바꿔 가방에 넣고(자리가 없으면 발밑에 떨어뜨림),
    /// 가방에 같은 배터리가 있으면 그중 가장 많이 남은 것을 바로 끼운다(기획: 즉시 교체).
    /// 배터리 칸이 처음부터 비어 있으면 가방에서 저절로 꺼내 끼우지 않는다 — 배터리는 사용자가 칸에 넣어 둔 것만 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LanternSlots : MonoBehaviour, IEquipSlots, ISlotLabels
    {
        public const int LanternIndex = 0;
        public const int BatteryIndex = 1;
        const float Epsilon = 0.0001f;

        [Tooltip("다 쓴 배터리를 넣고, 같은 배터리를 꺼내 오는 가방")]
        [SerializeField] Inventory bag;
        [Tooltip("가방에 자리가 없을 때 다 쓴 배터리를 떨어뜨린다")]
        [SerializeField] ItemDropper dropper;

        readonly ItemStack[] slots = new ItemStack[2];

        public int SlotCount => slots.Length;
        public event Action Changed;

        /// <summary>배터리를 다 써서 산화된 배터리로 바꿨을 때. 가방의 같은 배터리를 끼웠으면 true.</summary>
        public event Action<bool> BatterySpent;

        /// <summary>끼운 랜턴. 없으면 null(Space는 구르기).</summary>
        public LanternData Lantern => slots[LanternIndex].Item as LanternData;

        public ItemStack Battery => slots[BatteryIndex];

        /// <summary>배터리 칸 배터리의 남은 양. 없으면 0.</summary>
        public float BatteryCharge => slots[BatteryIndex].Item is LanternBatteryData data ? data.ChargeOf(slots[BatteryIndex]) : 0f;

        void Awake()
        {
            if (bag == null) bag = GetComponent<Inventory>();
            if (dropper == null) dropper = GetComponent<ItemDropper>();
        }

        /// <summary>
        /// amount만큼 쓸 수 있는 배터리를 칸에 준비한다. 칸 배터리가 모자라면 산화된 배터리로 바꾸고 가방의 같은 배터리로 갈아 끼운 뒤 다시 본다.
        /// 쓸 수 있으면 true. 배터리 칸이 비어 있으면 false(아무것도 하지 않는다).
        /// </summary>
        public bool PrepareBattery(float amount)
        {
            if (HasCharge(amount)) return true;
            if (!(slots[BatteryIndex].Item is LanternBatteryData data)) return false;
            Spend(data);
            Changed?.Invoke();
            return HasCharge(amount);
        }

        /// <summary>배터리를 amount만큼 닳게 한다. 다 쓰면 바로 갈아 끼운다. PrepareBattery가 true일 때 부른다.</summary>
        public void Consume(float amount)
        {
            if (!(slots[BatteryIndex].Item is LanternBatteryData data)) return;
            ItemStack battery = slots[BatteryIndex];
            slots[BatteryIndex] = battery.WithWear(battery.Wear + amount);
            if (data.ChargeOf(slots[BatteryIndex]) <= Epsilon) Spend(data);
            Changed?.Invoke();
        }

        bool HasCharge(float amount) => slots[BatteryIndex].Item is LanternBatteryData && BatteryCharge >= amount - Epsilon;

        /// <summary>칸 배터리를 산화된 배터리로 바꿔 내보내고, 가방의 같은 배터리 중 가장 많이 남은 것을 끼운다.</summary>
        void Spend(LanternBatteryData data)
        {
            slots[BatteryIndex] = ItemStack.Empty;

            // 새 배터리를 먼저 꺼내야 그 빈자리에 다 쓴 배터리가 들어간다(가방이 꽉 차 있어도 떨어뜨리지 않게).
            int best = FindFullestInBag(data);
            if (best >= 0) slots[BatteryIndex] = bag.TakeSlot(best);

            if (data.SpentItem != null)
            {
                var spent = new ItemStack(data.SpentItem, 1);
                int left = bag != null ? bag.Add(spent) : spent.Count;
                if (left > 0 && dropper != null) dropper.Drop(spent.WithCount(left));
            }
            BatterySpent?.Invoke(best >= 0);
        }

        int FindFullestInBag(LanternBatteryData data)
        {
            if (bag == null) return -1;
            int best = -1;
            float bestCharge = Epsilon;
            for (int i = 0; i < bag.SlotCount; i++)
            {
                ItemStack stack = bag.GetSlot(i);
                if (stack.Item != data) continue;
                float charge = data.ChargeOf(stack);
                if (charge <= bestCharge) continue;
                best = i;
                bestCharge = charge;
            }
            return best;
        }

        public ItemStack GetSlot(int index) => index >= 0 && index < slots.Length ? slots[index] : ItemStack.Empty;

        /// <summary>랜턴 칸에는 랜턴만, 배터리 칸에는 채운 랜턴 배터리만 놓인다.</summary>
        public bool CanPlace(int index, ItemStack stack)
        {
            if (stack.IsEmpty) return false;
            return index == LanternIndex ? stack.Item is LanternData
                : index == BatteryIndex && stack.Item is LanternBatteryData;
        }

        public ItemStack TakeSlot(int index)
        {
            if (index < 0 || index >= slots.Length || slots[index].IsEmpty) return ItemStack.Empty;
            ItemStack stack = slots[index];
            slots[index] = ItemStack.Empty;
            Changed?.Invoke();
            return stack;
        }

        public void PlaceInEmptySlot(int index, ItemStack stack)
        {
            if (index < 0 || index >= slots.Length || !slots[index].IsEmpty || !CanPlace(index, stack)) return;
            slots[index] = stack;
            Changed?.Invoke();
        }

        public bool CanAutoEquip(ItemStack stack) => FindEmptyFit(stack) >= 0;

        public ItemStack AutoEquip(ItemStack stack)
        {
            int index = FindEmptyFit(stack);
            if (index < 0) return stack;
            PlaceInEmptySlot(index, stack);
            return ItemStack.Empty;
        }

        int FindEmptyFit(ItemStack stack)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty && CanPlace(i, stack)) return i;
            }
            return -1;
        }

        public string GetSlotLabel(int index) => index == LanternIndex ? "랜턴" : index == BatteryIndex ? "배터리" : string.Empty;
    }
}
