using System.Collections.Generic;
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
    public sealed class TradePanel : MonoBehaviour
    {
        [SerializeField] TMP_Text titleLabel;
        [Tooltip("파는 물건 한 줄의 견본. 꺼 둔 채로 두면 이것을 복제해 줄을 만든다. 자식에 글자(TMP_Text)가 하나 있어야 한다.")]
        [SerializeField] Button offerTemplate;
        [SerializeField] RectTransform offerParent;
        [Tooltip("판매 칸 (칸 하나)")]
        [SerializeField] ItemGridView sellGrid;
        [Tooltip("방금 거래 결과 (판 값, 못 산 이유)")]
        [SerializeField] TMP_Text messageLabel;
        [SerializeField, Min(0f)] float messageDuration = 2f;
        [Tooltip("돈이 모자란 줄의 글자 진하기")]
        [SerializeField, Range(0f, 1f)] float unaffordableTextAlpha = 0.4f;

        readonly List<Button> rows = new();
        Trader trader;
        Inventory bag;
        float messageTimer;

        void Awake() => offerTemplate.gameObject.SetActive(false);

        void OnEnable() => Wallet.Changed += RefreshAffordable;
        void OnDisable() => Wallet.Changed -= RefreshAffordable;

        void Update()
        {
            if (messageTimer <= 0f) return;
            // 게임 시간이 아니라 실제 시간으로 센다(창은 게임이 멈춰도 쓸 수 있게).
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f) messageLabel.text = string.Empty;
        }

        /// <summary>보여 줄 상인을 바꾼다. null이면 비운다. 산 물건은 bag에 먼저 넣는다.</summary>
        public void Bind(Trader newTrader, Inventory buyerBag)
        {
            if (trader != null) trader.Counter.Sold -= OnSold;
            trader = newTrader;
            bag = buyerBag;
            if (trader != null) trader.Counter.Sold += OnSold;

            sellGrid.Bind(trader != null ? trader.Counter : null);
            ShowMessage(string.Empty);
            BuildRows();
        }

        void BuildRows()
        {
            foreach (var row in rows) Destroy(row.gameObject);
            rows.Clear();
            if (trader == null) return;

            titleLabel.text = trader.Data.DisplayName;
            IReadOnlyList<TraderData.Offer> offers = trader.Data.Offers;
            for (int i = 0; i < offers.Count; i++)
            {
                TraderData.Offer offer = offers[i];
                if (offer.item == null) continue;
                Button row = Instantiate(offerTemplate, offerParent);
                row.gameObject.SetActive(true);
                string count = offer.count > 1 ? $" ×{offer.count}" : string.Empty;
                row.GetComponentInChildren<TMP_Text>(true).text = $"{offer.item.DisplayName}{count}   {trader.Data.PriceOf(offer)}";
                row.onClick.AddListener(() => Buy(offer));
                rows.Add(row);
            }
            RefreshAffordable();
        }

        void RefreshAffordable()
        {
            if (trader == null) return;
            int r = 0;
            foreach (var offer in trader.Data.Offers)
            {
                if (offer.item == null) continue;
                bool affordable = Wallet.CanAfford(trader.Data.PriceOf(offer));
                Button row = rows[r++];
                row.interactable = affordable;
                // 버튼 색은 바탕만 바뀌므로 글자도 함께 흐리게 해서 못 사는 줄이 한눈에 보이게 한다.
                TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
                label.alpha = affordable ? 1f : unaffordableTextAlpha;
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

        void ShowMessage(string text)
        {
            messageLabel.text = text;
            messageTimer = string.IsNullOrEmpty(text) ? 0f : messageDuration;
        }
    }
}
