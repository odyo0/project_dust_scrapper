using DustScrapper.Interaction;
using DustScrapper.Items;
using DustScrapper.Progress;
using UnityEngine;

namespace DustScrapper.Trade
{
    /// <summary>
    /// 기지의 상인(시설). E로 거래 창을 연다(가방 화면이 상인 창과 함께 열린다).
    /// 팔기는 판매 칸(SellCounter)에 물건을 놓는 것이고(거래 중 우클릭도 판매 칸으로), 사기는 TryBuy다. 무엇을 얼마에 파는지는 TraderData에 있다.
    /// 보관함을 연결해 두면 거래하는 동안 보관함도 같이 열려서, 보관함 물건을 바로 팔고 산 물건이 가방에 다 안 들어가면 보관함으로 간다.
    /// </summary>
    public sealed class Trader : Station
    {
        [SerializeField] TraderData data;

        SellCounter counter;

        public TraderData Data => data;
        public SellCounter Counter => counter ??= new SellCounter(data);
        public override IItemSlots QuickMoveTarget => Counter;

        public override string ActionName => "거래";
        public override string TargetName => data != null ? data.DisplayName : name;

        public override string BlockedReason(PlayerInteractor interactor) => data == null ? "상인 데이터가 없다" : null;

        /// <summary>
        /// 물건을 산다. 돈이 모자라거나 가방(+ 보관함)에 다 들어갈 자리가 없으면 사지 않는다.
        /// 산 물건은 가방부터 채우고 남으면 보관함에 넣는다. 못 사면 그 이유를, 샀으면 null.
        /// </summary>
        public string TryBuy(TraderData.Offer offer, Inventory bag)
        {
            if (offer.item == null) return "팔 물건이 없다";
            int price = data.PriceOf(offer);
            if (!Wallet.CanAfford(price)) return "돈이 모자란다";

            Inventory storage = Storage;
            int space = (bag != null ? bag.SpaceFor(offer.item) : 0) + (storage != null ? storage.SpaceFor(offer.item) : 0);
            if (space < offer.count) return "넣을 자리가 없다";

            Wallet.TrySpend(price);
            int left = bag != null ? bag.Add(new ItemStack(offer.item, offer.count)) : offer.count;
            if (left > 0 && storage != null) storage.Add(new ItemStack(offer.item, left));
            return null;
        }
    }
}
