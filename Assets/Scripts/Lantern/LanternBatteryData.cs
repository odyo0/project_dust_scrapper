using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Lantern
{
    /// <summary>
    /// 글로우를 채운 랜턴 배터리. 랜턴 옆 배터리 칸에 넣으면 랜턴을 쓸 때마다 닳는다.
    /// 쓸 수 있는 양은 담은 글로우의 효율이다(기획: 여러 가지인 것은 배터리가 아니라 글로우 — 지금은 정제된 글로우 한 종류).
    /// 남은 양은 물건마다 들고 다니는 "닳은 양"(ItemStack.Wear)에 둔다. 그래서 버렸다 주워도, 저장해도 그대로이고 칸에 막대로 보인다.
    /// 다 쓰면 산화되어 다 쓴 배터리(spentItem)로 바뀐다. 다 쓴 배터리는 다시 채울 수 없고 팔 수만 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Lantern/Battery", fileName = "Battery")]
    public sealed class LanternBatteryData : ItemData
    {
        [Tooltip("담은 글로우로 쓸 수 있는 양(글로우 효율). AI 임시값")]
        [SerializeField, Min(0.01f)] float capacity = 5f;
        [Tooltip("다 쓰면 바뀌는 물건(다 쓴 랜턴 배터리)")]
        [SerializeField] ItemData spentItem;

        /// <summary>남은 양이 저마다 달라서 겹쳐 담지 않는다.</summary>
        public override int MaxStack => 1;
        public float Capacity => capacity;
        public ItemData SpentItem => spentItem;

        public float ChargeOf(ItemStack stack) => Mathf.Max(0f, capacity - stack.Wear);

        public override bool TryGetGauge(ItemStack stack, out float ratio)
        {
            ratio = ChargeOf(stack) / capacity;
            return true;
        }
    }
}
