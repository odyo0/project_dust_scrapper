using DustScrapper.Interaction;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 기지의 출발 문. E를 누르고 있으면 몸에 지닌 것을 들고 원정을 떠난다.
    /// 어느 맵으로 나갈지는 문마다 정한다(위험한 곳 고르기가 생기면 문이 여럿이 되거나 고르는 창이 붙는다).
    /// </summary>
    public sealed class ExpeditionGate : Interactable
    {
        [SerializeField] BaseProfileLink baseLink;

        [Tooltip("나갈 원정 씬 이름 (빌드 설정에 들어 있어야 한다)")]
        [SerializeField] string raidScene = "Raid";

        [SerializeField] string displayName = "원정 출발";

        public override string ActionName => "나가기";
        public override string TargetName => displayName;

        public override string BlockedReason(PlayerInteractor interactor) => null;

        public override void Interact(PlayerInteractor interactor) => baseLink.Depart(raidScene);
    }
}
