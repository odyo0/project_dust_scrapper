using System;
using DustScrapper.Items;
using DustScrapper.Magic;
using UnityEngine;

namespace DustScrapper.Equipment
{
    /// <summary>
    /// 몸 장비 칸(모자, 상의, 하의, 망토, 신발, 악세서리 2, 가방). 칸마다 끼는 부위가 정해져 있고, 그 부위 장비만 들어간다.
    /// 장비가 바뀔 때마다 낀 장비의 능력치 보너스를 모두 더해 쓰는 쪽에 알린다: 최대 MP·회복 속도 → Mana, 가방 칸 → Inventory.
    /// 칸 창구(IItemSlots)를 따라서 가방 화면·옮기기 규칙·판 결과가 가방·무기 칸과 같은 방식으로 붙는다.
    /// 가방(배낭)은 늘려 준 칸이 비어 있어야 벗을 수 있다(늘어난 칸의 물건을 어떻게 할지는 기획 빈칸이라 잠정으로 막아 둔다).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BodyEquipment : MonoBehaviour, IItemSlots, ISlotLabels
    {
        [Tooltip("칸마다 끼는 부위. 칸 수 = 배열 칸 수.")]
        [SerializeField] BodySlot[] slotKinds =
        {
            BodySlot.Head, BodySlot.Top, BodySlot.Bottom, BodySlot.Cloak, BodySlot.Shoes,
            BodySlot.Accessory, BodySlot.Accessory, BodySlot.Backpack,
        };

        [Tooltip("최대 MP·회복 속도 보너스를 받을 MP. 비워 두면 같은 물체에서 찾는다.")]
        [SerializeField] Mana mana;
        [Tooltip("가방 칸 보너스를 받을 가방. 비워 두면 같은 물체에서 찾는다.")]
        [SerializeField] Inventory bag;

        ItemStack[] slots;

        public int SlotCount => slotKinds.Length;

        /// <summary>가방 칸 보너스를 받는 가방. 벗은 가방이 자기가 늘려 준 칸에 들어가지 않게 옮기기 규칙이 읽는다.</summary>
        public Inventory Bag => bag;

        public event Action Changed;

        void Awake()
        {
            if (mana == null) mana = GetComponent<Mana>();
            if (bag == null) bag = GetComponent<Inventory>();
            slots = new ItemStack[slotKinds.Length];
            ApplyStats();
        }

        public ItemStack GetSlot(int index) => slots[index];

        /// <summary>그 칸의 부위에 맞는 몸 장비만 놓인다.</summary>
        public bool CanPlace(int index, ItemStack stack) =>
            stack.Item is BodyGearData gear && gear.Slot == slotKinds[index];

        /// <summary>가방 칸을 늘려 주는 장비는 늘어난 칸이 비어 있을 때만 벗을 수 있다.</summary>
        public bool CanTake(int index)
        {
            int granted = BagSlotsGrantedBy(index);
            if (granted <= 0 || bag == null) return true;
            return bag.IsEmptyFrom(bag.SlotCount - granted);
        }

        /// <summary>그 칸의 장비가 늘려 준 가방 칸 수.</summary>
        public int BagSlotsGrantedBy(int index) =>
            slots[index].Item is BodyGearData gear ? Mathf.RoundToInt(gear.Sum(StatType.BagSlots)) : 0;

        public ItemStack TakeSlot(int index)
        {
            ItemStack stack = slots[index];
            if (stack.IsEmpty) return stack;
            slots[index] = ItemStack.Empty;
            ApplyStats();
            Changed?.Invoke();
            return stack;
        }

        public void PlaceInEmptySlot(int index, ItemStack stack)
        {
            if (!slots[index].IsEmpty || !CanPlace(index, stack)) return;
            slots[index] = stack;
            ApplyStats();
            Changed?.Invoke();
        }

        /// <summary>그 물건을 낄 수 있는 빈 칸이 있는가.</summary>
        public bool HasEmptySlotFor(ItemStack stack)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty && CanPlace(i, stack)) return true;
            }
            return false;
        }

        /// <summary>그 부위의 빈 칸에 낀다(바닥에서 주울 때). 빈 칸이 없으면 끼지 않고 false.</summary>
        public bool TryEquip(ItemStack stack)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty || !CanPlace(i, stack)) continue;
                PlaceInEmptySlot(i, stack);
                return true;
            }
            return false;
        }

        public string GetSlotLabel(int index) => slotKinds[index] switch
        {
            BodySlot.Head => "모자",
            BodySlot.Top => "상의",
            BodySlot.Bottom => "하의",
            BodySlot.Cloak => "망토",
            BodySlot.Shoes => "신발",
            BodySlot.Accessory => "악세서리",
            BodySlot.Backpack => "가방",
            _ => string.Empty,
        };

        /// <summary>낀 장비의 보너스를 능력치마다 더해서 쓰는 쪽에 알린다.</summary>
        void ApplyStats()
        {
            if (mana != null) mana.SetBonuses(Sum(StatType.MaxMana), Sum(StatType.ManaRegen));
            if (bag != null) bag.SetExtraSlots(Mathf.RoundToInt(Sum(StatType.BagSlots)));
        }

        float Sum(StatType stat)
        {
            float total = 0f;
            foreach (ItemStack stack in slots)
            {
                if (stack.Item is BodyGearData gear) total += gear.Sum(stat);
            }
            return total;
        }
    }
}
