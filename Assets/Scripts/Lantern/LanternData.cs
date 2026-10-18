using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Lantern
{
    /// <summary>
    /// 랜턴 한 종류(빛 랜턴 …). 랜턴 칸에 끼우면 Space가 이 랜턴의 효과가 된다.
    /// 무엇을 하는지는 효과 데이터(LanternEffect)를 꽂아서 정하고, 한 번 쓸 때 배터리를 얼마나 닳게 하는지를 가진다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Lantern/Lantern", fileName = "Lantern")]
    public sealed class LanternData : ItemData
    {
        [SerializeField] LanternEffect effect;
        [Tooltip("한 번 쓸 때 닳는 배터리 양. AI 임시값")]
        [SerializeField, Min(0.01f)] float chargePerUse = 1f;

        public override int MaxStack => 1;
        public LanternEffect Effect => effect;
        public float ChargePerUse => chargePerUse;
    }
}
