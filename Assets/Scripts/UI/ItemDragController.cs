using DustScrapper.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 가방 화면에서 마우스로 물건을 다룬다.
    /// 끌어서 다른 칸에 놓으면 옮기기·겹치기·바꾸기, 창 밖(빈 화면)에 놓으면 발밑에 버리기,
    /// 우클릭은 빠른 옮기기(상자 ↔ 가방, 무기 슬롯 → 가방, 상자가 없을 때 가방의 무기 → 무기 슬롯). 상인과 거래 중이면 우클릭은 팔기다.
    /// 옮기는 규칙 자체는 ItemTransfer에 있고, 여기서는 "어디서 어디로"만 정한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemDragController : MonoBehaviour
    {
        [SerializeField] InventoryScreen screen;
        [SerializeField] ItemDropper dropper;
        [Tooltip("끄는 동안 커서를 따라다니는 물건 그림")]
        [SerializeField] Image ghost;

        ItemSlotView dragging;
        bool dropHandled;

        /// <summary>지금 마우스가 올라가 있는 칸. 물건 정보 표시가 읽는다.</summary>
        public ItemSlotView Hovered { get; private set; }

        void Awake()
        {
            ghost.raycastTarget = false;
            ghost.gameObject.SetActive(false);
        }

        void OnDisable() => CancelDrag();

        public void Hover(ItemSlotView slot) => Hovered = slot;

        public void Unhover(ItemSlotView slot)
        {
            if (Hovered == slot) Hovered = null;
        }

        public void BeginDrag(ItemSlotView slot, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || slot.Stack.IsEmpty) return;

            dragging = slot;
            dropHandled = false;
            slot.SetDragging(true);
            ghost.color = slot.Stack.Item.PlaceholderColor;
            ghost.gameObject.SetActive(true);
            ghost.transform.position = eventData.position;
        }

        public void Drag(PointerEventData eventData)
        {
            if (dragging != null) ghost.transform.position = eventData.position;
        }

        public void DropOn(ItemSlotView target)
        {
            if (dragging == null) return;
            dropHandled = true;
            ItemTransfer.Move(dragging.Grid.Source, dragging.Index, target.Grid.Source, target.Index);
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (dragging == null) return;

            // 칸이 아닌 곳이라도 창 위(패널)에 놓았으면 아무 일도 없다. 창 밖 빈 화면에 놓았을 때만 버린다.
            if (!dropHandled && eventData.pointerCurrentRaycast.gameObject == null)
            {
                IItemSlots source = dragging.Grid.Source;
                if (source.CanTake(dragging.Index)) dropper.Drop(source.TakeSlot(dragging.Index));
            }
            CancelDrag();
        }

        public void QuickMove(ItemSlotView slot)
        {
            if (dragging != null || slot.Stack.IsEmpty) return;

            IItemSlots from = slot.Grid.Source;
            // 시설 창이 우클릭으로 받을 칸을 가지면(상인의 판매 칸) 어느 칸에서든 그리로 보낸다.
            IItemSlots stationTarget = screen.Station != null ? screen.Station.QuickMoveTarget : null;
            if (stationTarget != null && from != stationTarget)
            {
                ItemTransfer.QuickMove(from, slot.Index, stationTarget);
                return;
            }
            if (from == screen.Loot || from == screen.Weapons || from == screen.Support || from == screen.Body)
            {
                ItemTransfer.QuickMove(from, slot.Index, screen.Bag);
                return;
            }
            if (from != screen.Bag) return;

            // 가방에서: 상자를 열었으면 상자로. 아니면 장비 칸으로(몸 장비는 몸 칸, 손 장비는 무기 칸 먼저, 자리가 없거나 맞지 않으면 보조 칸).
            if (screen.Loot != null)
            {
                ItemTransfer.QuickMove(from, slot.Index, screen.Loot);
                return;
            }
            if (screen.Body != null && ItemTransfer.QuickMove(from, slot.Index, screen.Body)) return;
            if (screen.Weapons.CanPlace(0, slot.Stack) && ItemTransfer.QuickMove(from, slot.Index, screen.Weapons)) return;
            if (screen.Support != null && screen.Support.CanPlace(0, slot.Stack)) ItemTransfer.QuickMove(from, slot.Index, screen.Support);
        }

        void CancelDrag()
        {
            if (dragging != null) dragging.SetDragging(false);
            dragging = null;
            if (ghost != null) ghost.gameObject.SetActive(false);
        }
    }
}
