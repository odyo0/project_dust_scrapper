using System.Collections.Generic;
using DustScrapper.Interaction;
using DustScrapper.Items;
using DustScrapper.Progress;
using DustScrapper.Trade;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 상인 창: 상인 이름, 파는 물건 목록(눌러서 사기), 판매 칸(끌어 놓거나 우클릭으로 팔기), 방금 거래 결과 한 줄.
    /// 목록 줄은 숨겨 둔 견본 줄(offerTemplate)을 복제해 만든다. 돈이 모자란 줄은 누를 수 없게 흐려진다.
    /// 가방 화면(InventoryScreen)이 상인과 거래할 때 Bind로 상인을 넘기고 켠다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TradePanel : StationPanel
    {
        [SerializeField] TMP_Text titleLabel;
        [Tooltip("파는 물건 한 줄의 견본. 꺼 둔 채로 두면 이것을 복제해 줄을 만든다. 자식에 글자(TMP_Text)가 하나 있어야 한다.")]
        [SerializeField] Button offerTemplate;
        [SerializeField] RectTransform offerParent;
        [Tooltip("판매 칸 (칸 하나)")]
        [SerializeField] ItemGridView sellGrid;

        ButtonRows rows;
        Trader trader;
        Inventory bag;

        void Awake() => rows = new ButtonRows(offerTemplate, offerParent);

        void OnEnable() => Wallet.Changed += MarkDirty;
        void OnDisable() => Wallet.Changed -= MarkDirty;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (trader != null) trader.Counter.Sold -= OnSold;
        }

        public override bool Shows(Station station) => station is Trader;

        public override void Bind(Station station, PlayerInteractor player)
        {
            if (trader != null) trader.Counter.Sold -= OnSold;
            trader = station as Trader;
            bag = player != null && player.Gear != null ? player.Gear.Bag : null;
            if (trader != null) trader.Counter.Sold += OnSold;

            sellGrid.Bind(trader != null ? trader.Counter : null);
            ShowMessage(string.Empty);
            MarkDirty();
        }

        protected override void Redraw()
        {
            rows.Clear();
            if (trader == null) return;

            titleLabel.text = trader.Data.DisplayName;
            IReadOnlyList<TraderData.Offer> offers = trader.Data.Offers;
            for (int i = 0; i < offers.Count; i++)
            {
                TraderData.Offer offer = offers[i];
                if (offer.item == null) continue;
                int price = trader.Data.PriceOf(offer);
                string count = offer.count > 1 ? $" ×{offer.count}" : string.Empty;
                Button row = rows.Add($"{offer.item.DisplayName}{count}   {price}", () => Buy(offer));
                SetUsable(row, Wallet.CanAfford(price));
            }
        }

        void Buy(TraderData.Offer offer)
        {
            string failed = trader.TryBuy(offer, bag);
            ShowMessage(failed ?? $"{offer.item.DisplayName} 샀다  -{trader.Data.PriceOf(offer)}");
        }

        void OnSold(ItemStack stack, int price)
        {
            string count = stack.Count > 1 ? $" ×{stack.Count}" : string.Empty;
            ShowMessage($"{stack.Item.DisplayName}{count} 팔았다  +{price}");
        }
    }
}
