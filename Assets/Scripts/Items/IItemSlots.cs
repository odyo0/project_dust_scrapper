using System;

namespace DustScrapper.Items
{
    /// <summary>
    /// 물건을 칸 단위로 담는 것의 공통 창구. 가방·상자(Inventory)와 무기 슬롯(WeaponLoadout)이 이것을 따른다.
    /// 칸 사이 옮기기(ItemTransfer)와 가방 화면은 이 창구만 보고 일하므로, 무엇끼리 옮기는지 몰라도 된다.
    /// </summary>
    public interface IItemSlots
    {
        int SlotCount { get; }

        ItemStack GetSlot(int index);

        /// <summary>그 칸에 이 물건이 놓일 수 있는가(칸이 비었는지와는 상관없이, 종류만 본다).</summary>
        bool CanPlace(int index, ItemStack stack);

        /// <summary>그 칸을 비우고 들어 있던 것을 돌려준다.</summary>
        ItemStack TakeSlot(int index);

        /// <summary>빈 칸에 놓는다. 칸이 차 있으면 놓지 않는다.</summary>
        void PlaceInEmptySlot(int index, ItemStack stack);

        /// <summary>칸 내용이 바뀔 때마다.</summary>
        event Action Changed;
    }
}
