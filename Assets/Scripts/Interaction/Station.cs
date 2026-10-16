using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Interaction
{
    /// <summary>
    /// 기지에서 F로 여는 시설의 공통 틀(상인, 충전대, 나중에 정제 시설 등). 열면 가방 화면이 그 시설의 창(StationPanel)과 함께 열린다.
    /// 보관함을 연결해 두면 시설을 쓰는 동안 보관함도 같이 열려서, 보관함 물건을 바로 쓰거나 결과물을 받을 수 있다.
    /// 시설마다 다른 것은 "무엇을 하는지"뿐이라, 열고 닫기·거리·보관함 같이 열기는 여기와 가방 화면이 한 번만 맡는다.
    /// </summary>
    public abstract class Station : Interactable
    {
        [Tooltip("이 시설을 쓰는 동안 같이 열 보관함. 비워 두면 가방만 쓴다.")]
        [SerializeField] Inventory storage;

        public Inventory Storage => storage;

        /// <summary>시설 창이 열려 있을 때 칸을 우클릭하면 물건을 보낼 곳(상인의 판매 칸 등). 없으면 null(보통 우클릭 그대로).</summary>
        public virtual IItemSlots QuickMoveTarget => null;

        public override void Interact(PlayerInteractor interactor) => interactor.OpenStation(this);
    }
}
