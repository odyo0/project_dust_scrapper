using DustScrapper.Interaction;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 뒤질 수 있는 상자. 내용물은 같은 물체의 Inventory(칸 보관함)에 있다.
    /// 처음에는 오래 뒤져야 열리고(searchTime), 한 번 뒤진 상자는 짧게 눌러 다시 연다.
    /// 열면 플레이어에게 칸을 넘기기만 하고, 물건을 옮기는 건 가방 화면이 한다.
    /// 물건 표(LootTable)를 넣어 두면 판이 시작될 때 내용물을 새로 뽑는다(Inventory의 처음 물건 대신).
    /// </summary>
    [RequireComponent(typeof(Inventory))]
    public sealed class LootContainer : Interactable
    {
        [SerializeField] string displayName = "상자";

        [Tooltip("처음 뒤질 때 F를 누르고 있어야 하는 시간 (초). 한 번 뒤진 뒤에는 holdTime을 쓴다.")]
        [SerializeField, Min(0f)] float searchTime = 1.5f;

        [Tooltip("판마다 내용물을 뽑을 표. 비워 두면 Inventory의 처음 물건 그대로(보관함 등).")]
        [SerializeField] LootTable table;

        Inventory contents;
        bool searched;

        /// <summary>상자 이름(보관함 등). 가방 화면의 상자 쪽 제목으로 쓴다.</summary>
        public string DisplayName => displayName;

        public override float HoldTime => searched ? base.HoldTime : searchTime;
        public override string ActionName => searched ? "열기" : "뒤지기";
        public override string TargetName => searched && contents.IsEmpty ? $"{displayName} (빈 상자)" : displayName;

        void Awake()
        {
            contents = GetComponent<Inventory>();
            if (table == null) return;
            for (int i = contents.SlotCount - 1; i >= 0; i--) contents.TakeSlot(i);
            foreach (var stack in table.Roll()) contents.Add(stack);
        }

        public override string BlockedReason(PlayerInteractor interactor) => null;

        public override void Interact(PlayerInteractor interactor)
        {
            searched = true;
            interactor.OpenLoot(contents);
        }
    }
}
