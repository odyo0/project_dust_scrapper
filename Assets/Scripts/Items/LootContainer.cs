using DustScrapper.Interaction;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 뒤질 수 있는 상자. 내용물은 같은 물체의 Inventory(칸 보관함)에 있다.
    /// 처음에는 오래 뒤져야 열리고(searchTime), 한 번 뒤진 상자는 짧게 눌러 다시 연다.
    /// 열면 플레이어에게 칸을 넘기기만 하고, 물건을 옮기는 건 가방 화면이 한다.
    /// </summary>
    [RequireComponent(typeof(Inventory))]
    public sealed class LootContainer : Interactable
    {
        [SerializeField] string displayName = "상자";

        [Tooltip("처음 뒤질 때 E를 누르고 있어야 하는 시간 (초). 한 번 뒤진 뒤에는 holdTime을 쓴다.")]
        [SerializeField, Min(0f)] float searchTime = 1.5f;

        Inventory contents;
        bool searched;

        public override float HoldTime => searched ? base.HoldTime : searchTime;
        public override string ActionName => searched ? "열기" : "뒤지기";
        public override string TargetName => searched && contents.IsEmpty ? $"{displayName} (빈 상자)" : displayName;

        void Awake() => contents = GetComponent<Inventory>();

        public override string BlockedReason(PlayerInteractor interactor) => null;

        public override void Interact(PlayerInteractor interactor)
        {
            searched = true;
            interactor.OpenLoot(contents);
        }
    }
}
