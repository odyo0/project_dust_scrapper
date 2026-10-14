using System;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 소모품 단축 칸(키 4·5·6). 소모품만 놓이고, 키를 누르면 ItemUser가 그 칸의 소모품을 바로 쓴다.
    /// 칸 창구(IItemSlots)라 가방 화면·옮기기·판 결과·저장(PlayerLoadout)이 다른 칸과 같은 방식으로 붙는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuickSlots : MonoBehaviour, IItemSlots, ISlotLabels
    {
        [SerializeField, Min(1)] int slotCount = 3;
        [Tooltip("첫 칸의 키 번호 (4면 4·5·6)")]
        [SerializeField] int firstKeyNumber = 4;

        ItemStack[] slots;

        ItemStack[] Slots => slots ??= new ItemStack[slotCount];

        public int SlotCount => Slots.Length;
        public event Action Changed;

        public ItemStack GetSlot(int index) => Slots[index];

        public bool CanPlace(int index, ItemStack stack) => stack.Item is ConsumableData;

        public ItemStack TakeSlot(int index)
        {
            ItemStack stack = Slots[index];
            if (stack.IsEmpty) return stack;
            Slots[index] = ItemStack.Empty;
            Changed?.Invoke();
            return stack;
        }

        public void PlaceInEmptySlot(int index, ItemStack stack)
        {
            if (!Slots[index].IsEmpty || !CanPlace(index, stack)) return;
            Slots[index] = stack;
            Changed?.Invoke();
        }

        public string GetSlotLabel(int index) => (firstKeyNumber + index).ToString();
    }
}
