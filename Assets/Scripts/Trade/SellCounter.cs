using System;
using DustScrapper.Items;
using DustScrapper.Progress;

namespace DustScrapper.Trade
{
    /// <summary>
    /// 상인의 판매 칸. 늘 비어 있는 칸 하나이고, 물건을 놓는 순간 상인이 사들여 돈이 들어온다.
    /// 칸 창구(IItemSlots)를 따라서, 가방 화면의 끌어 놓기·우클릭 옮기기(ItemTransfer)가 그대로 "팔기"가 된다.
    /// 값이 0인 물건은 놓이지 않는다(팔 수 없다).
    /// </summary>
    public sealed class SellCounter : IItemSlots
    {
        readonly TraderData data;

        public SellCounter(TraderData data) => this.data = data;

        public int SlotCount => 1;

        /// <summary>물건을 팔았을 때. 값은 판 묶음과 받은 돈.</summary>
        public event Action<ItemStack, int> Sold;

        // 칸 내용은 늘 비어 있어서 바뀔 일이 없다. 창구를 따르려고만 둔다.
        public event Action Changed { add { } remove { } }

        public ItemStack GetSlot(int index) => ItemStack.Empty;

        public bool CanPlace(int index, ItemStack stack) => data.SellPriceOf(stack) > 0;

        public ItemStack TakeSlot(int index) => ItemStack.Empty;

        public void PlaceInEmptySlot(int index, ItemStack stack)
        {
            int price = data.SellPriceOf(stack);
            if (price <= 0) return;
            Wallet.Earn(price);
            Sold?.Invoke(stack, price);
        }
    }
}
