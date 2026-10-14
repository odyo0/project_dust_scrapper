using DustScrapper.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>플레이 화면의 소모품 단축 칸(4·5·6) 표시: 칸마다 물건 색 네모와 개수. 단축 칸이 바뀔 때만 다시 그린다.</summary>
    [DisallowMultipleComponent]
    public sealed class QuickSlotHud : MonoBehaviour
    {
        [SerializeField] QuickSlots slots;
        [SerializeField] Image[] icons;
        [SerializeField] TMP_Text[] counts;

        void OnEnable()
        {
            slots.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => slots.Changed -= Refresh;

        void Refresh()
        {
            for (int i = 0; i < icons.Length; i++)
            {
                ItemStack stack = i < slots.SlotCount ? slots.GetSlot(i) : ItemStack.Empty;
                icons[i].enabled = !stack.IsEmpty;
                if (!stack.IsEmpty) icons[i].color = stack.Item.PlaceholderColor;
                if (i < counts.Length) counts[i].text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
            }
        }
    }
}
