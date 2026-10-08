using DustScrapper.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 가방 화면의 칸 하나. 담긴 물건을 그리고(임시 색 네모 + 개수), 마우스 동작(올리기, 끌기, 놓기, 우클릭)을
    /// ItemDragController에 넘긴다. 물건을 어떻게 옮길지는 모른다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemSlotView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text countLabel;
        [Tooltip("칸 번호(무기 슬롯의 숫자키). 없으면 비워 둔다.")]
        [SerializeField] TMP_Text numberLabel;

        [Tooltip("끌고 있는 동안 원래 칸의 물건을 흐리게 하는 정도")]
        [SerializeField, Range(0f, 1f)] float draggingAlpha = 0.35f;

        ItemDragController controller;

        public ItemGridView Grid { get; private set; }
        public int Index { get; private set; }
        public ItemStack Stack { get; private set; }

        public void Setup(ItemGridView grid, int index, ItemDragController dragController, bool showNumber)
        {
            Grid = grid;
            Index = index;
            controller = dragController;
            if (numberLabel != null)
            {
                numberLabel.gameObject.SetActive(showNumber);
                numberLabel.text = (index + 1).ToString();
            }
        }

        public void Show(ItemStack stack)
        {
            Stack = stack;
            icon.enabled = !stack.IsEmpty;
            if (!stack.IsEmpty) icon.color = stack.Item.PlaceholderColor;
            countLabel.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
        }

        /// <summary>이 칸의 물건을 끌고 있는 동안 흐리게 보인다.</summary>
        public void SetDragging(bool dragging)
        {
            if (Stack.IsEmpty) return;
            Color color = Stack.Item.PlaceholderColor;
            color.a = dragging ? draggingAlpha : 1f;
            icon.color = color;
        }

        public void OnPointerEnter(PointerEventData eventData) => controller.Hover(this);
        public void OnPointerExit(PointerEventData eventData) => controller.Unhover(this);
        public void OnBeginDrag(PointerEventData eventData) => controller.BeginDrag(this, eventData);
        public void OnDrag(PointerEventData eventData) => controller.Drag(eventData);
        public void OnEndDrag(PointerEventData eventData) => controller.EndDrag(eventData);
        public void OnDrop(PointerEventData eventData) => controller.DropOn(this);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right) controller.QuickMove(this);
        }
    }
}
