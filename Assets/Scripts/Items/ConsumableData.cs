using DustScrapper.Items.Use;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 써서 없어지는 물건(글로우 가루, 연막 …). 쓰면 효과 목록(UseEffect)을 차례로 내고 하나 줄어든다.
    /// 단축 칸(4·5·6)에 끼우거나 가방에서 우클릭해 쓴다(ItemUser).
    /// 마법진 장비 위에 끌어 놓으면 내구도를 채우는 물건(글로우 가루 = 원정 중 급한 충전)은 chargeDurability를 채운다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Items/Consumable", fileName = "Consumable")]
    public sealed class ConsumableData : ItemData
    {
        [Tooltip("쓸 때 낼 효과들 (위에서부터)")]
        [SerializeField] UseEffect[] effects;

        [Header("마법진 장비에 끌어 놓았을 때 (급한 충전)")]
        [Tooltip("채우는 내구도. 0이면 장비에 쓸 수 없다.")]
        [SerializeField, Min(0f)] float chargeDurability;
        [Tooltip("그때 오르는 글로우병 (%)")]
        [SerializeField, Min(0f)] float chargeSickness;

        public UseEffect[] Effects => effects;
        public float ChargeDurability => chargeDurability;
        public float ChargeSickness => chargeSickness;
        public bool CanChargeGear => chargeDurability > 0f;
    }
}
