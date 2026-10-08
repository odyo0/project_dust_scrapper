using DustScrapper.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 가방 화면에서 마우스로 물건을 다룬다.
    /// 끌어서 다른 칸에 놓으면 옮기기·겹치기·바꾸기, 창 밖(빈 화면)에 놓으면 발밑에 버리기,
    /// 우클릭은 빠른 옮기기(상자 ↔ 가방, 무기 슬롯 → 가방, 상자가 없을 때 가방의 무기 → 무기 슬롯).
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
                dropper.Drop(source.TakeSlot(dragging.Index));
            }
            CancelDrag();
        }

        public void QuickMove(ItemSlotView slot)
        {
            if (dragging != null || slot.Stack.IsEmpty) return;

            IItemSlots from = slot.Grid.Source;
            IItemSlots to = null;
            if (from == screen.Loot || from == screen.Weapons) to = screen.Bag;
            else if (from == screen.Bag) to = screen.Loot ?? (screen.Weapons.CanPlace(0, slot.Stack) ? screen.Weapons : null);

            if (to != null) ItemTransfer.QuickMove(from, slot.Index, to);
        }

        void CancelDrag()
        {
            if (dragging != null) dragging.SetDragging(false);
            dragging = null;
            if (ghost != null) ghost.gameObject.SetActive(false);
        }
    }
}
