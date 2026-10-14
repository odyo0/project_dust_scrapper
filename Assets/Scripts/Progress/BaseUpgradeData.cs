using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 기지 시설·업그레이드 하나: 이름, 효과 한 줄, 단계마다 드는 돈. 단계 수 = 값 목록 길이(시설 짓기는 1단계).
    /// 효과 자체는 이 데이터를 보는 쪽이 낸다(시설 켜기 UpgradeUnlocks, 보관함 칸 BaseProfileLink).
    /// 새 업그레이드는 코드가 아니라 이 데이터 파일을 만들고 건설판 목록에 넣는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Progress/Base Upgrade", fileName = "Upgrade")]
    public sealed class BaseUpgradeData : ScriptableObject
    {
        [Tooltip("저장 파일에 적는 이름표. 한 번 정하면 바꾸지 않는다. 비우면 파일 이름.")]
        [SerializeField] string saveId;
        [SerializeField] string displayName;
        [Tooltip("건설판에 보이는 효과 한 줄")]
        [SerializeField] string effect;
        [Tooltip("단계마다 드는 돈 (1단계, 2단계, …)")]
        [SerializeField] int[] prices = { 100 };

        public string SaveId => string.IsNullOrEmpty(saveId) ? name : saveId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Effect => effect;
        public int MaxLevel => prices != null ? prices.Length : 0;

        /// <summary>지금 단계에서 다음 단계로 가는 값. 다 지었으면 -1.</summary>
        public int PriceAt(int currentLevel) => currentLevel >= 0 && currentLevel < MaxLevel ? prices[currentLevel] : -1;
    }
}
