using System.Collections.Generic;
using DustScrapper.Interaction;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>기지의 건설판(시설). E로 열면 지을 수 있는 시설·업그레이드 목록이 나온다(창은 BuildPanel). 사는 규칙은 BaseUpgrades.</summary>
    public sealed class BuildBoard : Station
    {
        [SerializeField] string displayName = "건설판";
        [Tooltip("건설판에 보일 시설·업그레이드 (위에서부터)")]
        [SerializeField] List<BaseUpgradeData> upgrades = new();

        public IReadOnlyList<BaseUpgradeData> Upgrades => upgrades;

        public override string ActionName => "건설";
        public override string TargetName => displayName;
        public override string BlockedReason(PlayerInteractor interactor) => null;
    }
}
