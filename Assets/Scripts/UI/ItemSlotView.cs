using DustScrapper.Equipment;
using DustScrapper.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 가방 화면의 칸 하나. 담긴 물건을 그리고(임시 색 네모 + 개수 + 내구도가 있으면 아래쪽 막대), 마우스 동작(올리기, 끌기, 놓기, 우클릭)을
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

        [Tooltip("내구도 막대(칸 아래쪽). 길이는 anchorMax.x로 정한다. 내구도가 없는 물건이면 숨긴다.")]
        [SerializeField] Image durabilityBar;
        [SerializeField] Color durabilityFull = new(0.45f, 0.85f, 0.95f);
        [SerializeField] Color durabilityLow = new(0.95f, 0.35f, 0.3f);

        [Tooltip("끌고 있는 동안 원래 칸의 물건을 흐리게 하는 정도")]
        [SerializeField, Range(0f, 1f)] float draggingAlpha = 0.35f;
        [Tooltip("다 닳은 장비를 흐리게 하는 정도")]
        [SerializeField, Range(0f, 1f)] float brokenAlpha = 0.45f;

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

        /// <summary>번호 자리에 칸 이름을 보인다(몸 장비 칸: 모자, 상의 …).</summary>
        public void SetLabel(string text)
        {
            if (numberLabel == null) return;
            numberLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
            numberLabel.text = text;
        }

        public void Show(ItemStack stack)
        {
            Stack = stack;
            icon.enabled = !stack.IsEmpty;
            if (!stack.IsEmpty) icon.color = IconColor(1f);
            countLabel.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
            ShowDurability(stack);
        }

        void ShowDurability(ItemStack stack)
        {
            if (durabilityBar == null) return;
            bool show = stack.Item is BodyGearData gear && gear.HasDurability;
            durabilityBar.gameObject.SetActive(show);
            if (!show) return;

            var data = (BodyGearData)stack.Item;
            float ratio = data.DurabilityOf(stack) / data.MaxDurability;
            RectTransform rect = durabilityBar.rectTransform;
            rect.anchorMax = new Vector2(ratio, rect.anchorMax.y);
            durabilityBar.color = Color.Lerp(durabilityLow, durabilityFull, ratio);
        }

        /// <summary>물건의 임시 색. 다 닳은 장비는 흐리게.</summary>
        Color IconColor(float alpha)
        {
            Color color = Stack.Item.PlaceholderColor;
            if (Stack.Item is BodyGearData gear && gear.IsBroken(Stack)) alpha *= brokenAlpha;
            color.a = alpha;
            return color;
        }

        /// <summary>이 칸의 물건을 끌고 있는 동안 흐리게 보인다.</summary>
        public void SetDragging(bool dragging)
        {
            if (Stack.IsEmpty) return;
            icon.color = IconColor(dragging ? draggingAlpha : 1f);
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
