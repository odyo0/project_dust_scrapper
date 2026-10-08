using System.Collections.Generic;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 칸 묶음(가방, 상자, 무기 슬롯) 하나를 칸 그림들로 보여 준다. 연결된 칸 수만큼 칸 그림을 만들고,
    /// 칸 내용이 바뀌면(Changed) 다시 그린다. 칸 배치는 칸 부모의 GridLayoutGroup이 맡는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemGridView : MonoBehaviour
    {
        [SerializeField] ItemSlotView slotPrefab;
        [SerializeField] RectTransform cellParent;
        [SerializeField] ItemDragController dragController;

        [Tooltip("칸에 번호(1부터)를 표시한다. 무기 슬롯은 숫자키와 맞춘다.")]
        [SerializeField] bool showSlotNumbers;

        readonly List<ItemSlotView> cells = new();
        IItemSlots source;

        public IItemSlots Source => source;

        /// <summary>보여 줄 칸 묶음을 바꾼다. null이면 아무것도 보여 주지 않는다.</summary>
        public void Bind(IItemSlots newSource)
        {
            if (source != null) source.Changed -= Refresh;
            source = newSource;
            if (source != null) source.Changed += Refresh;

            int count = source != null ? source.SlotCount : 0;
            while (cells.Count < count)
            {
                ItemSlotView cell = Instantiate(slotPrefab, cellParent);
                cell.Setup(this, cells.Count, dragController, showSlotNumbers);
                cells.Add(cell);
            }
            for (int i = 0; i < cells.Count; i++) cells[i].gameObject.SetActive(i < count);

            Refresh();
        }

        void OnDestroy()
        {
            if (source != null) source.Changed -= Refresh;
        }

        void Refresh()
        {
            if (source == null) return;
            for (int i = 0; i < source.SlotCount; i++) cells[i].Show(source.GetSlot(i));
        }
    }
}
