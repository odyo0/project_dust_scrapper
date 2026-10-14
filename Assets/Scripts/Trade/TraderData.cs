using System;
using System.Collections.Generic;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Trade
{
    /// <summary>
    /// 상인 한 명의 장사 내용: 무엇을 몇 개씩 얼마에 파는지, 사들일 때 가치의 몇 배를 쳐 주는지.
    /// 상인을 늘리거나 파는 목록·값을 바꿀 때는 코드가 아니라 이 데이터 파일을 고친다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Trade/Trader", fileName = "Trader")]
    public sealed class TraderData : ScriptableObject
    {
        [Serializable]
        public struct Offer
        {
            public ItemData item;

            [Tooltip("한 번 살 때 받는 개수")]
            [Min(1)] public int count;

            [Tooltip("한 번 사는 값. 0이면 (가치 × 개수 × 사는 값 배율)로 정한다.")]
            [Min(0)] public int price;
        }

        [SerializeField] string displayName = "상인";

        [Tooltip("파는 물건 목록 (위에서부터 보인다)")]
        [SerializeField] List<Offer> offers = new();

        [Tooltip("값을 따로 정하지 않은 물건을 팔 때, 가치의 몇 배를 받는가")]
        [SerializeField, Min(0f)] float buyPriceMultiplier = 2f;

        [Tooltip("플레이어에게서 사들일 때, 가치의 몇 배를 쳐 주는가")]
        [SerializeField, Min(0f)] float sellPriceMultiplier = 1f;

        public string DisplayName => displayName;
        public IReadOnlyList<Offer> Offers => offers;

        /// <summary>플레이어가 이 물건을 사는 값.</summary>
        public int PriceOf(Offer offer)
        {
            if (offer.item == null) return 0;
            if (offer.price > 0) return offer.price;
            return Mathf.CeilToInt(offer.item.Value * offer.count * buyPriceMultiplier);
        }

        /// <summary>플레이어가 이 묶음을 팔면 받는 값. 0이면 팔 수 없다(가치 0인 보급 단검 등).</summary>
        public int SellPriceOf(ItemStack stack)
        {
            if (stack.IsEmpty) return 0;
            return Mathf.FloorToInt(stack.Item.Value * stack.Count * sellPriceMultiplier);
        }
    }
}
