using DustScrapper.Items;
using DustScrapper.Weapons;
using TMPro;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 가방 화면에서 마우스를 올린 칸의 물건 정보를 한 줄로 보여 준다: 이름(개수), 총이면 탄창, 가치.
    /// 칸 그림에는 이름을 쓸 자리가 없어서, 무엇인지와 가져갈 만한지를 여기서 읽는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemInfoLabel : MonoBehaviour
    {
        [SerializeField] ItemDragController dragController;
        [SerializeField] TMP_Text label;

        ItemStack shown;

        void OnDisable()
        {
            shown = ItemStack.Empty;
            label.text = string.Empty;
        }

        void LateUpdate()
        {
            ItemSlotView slot = dragController.Hovered;
            // 총의 탄창은 쏘면 바뀌므로 칸 그림의 마지막 상태가 아니라 칸 묶음에서 바로 읽는다.
            ItemStack stack = slot != null && slot.isActiveAndEnabled ? slot.Grid.Source.GetSlot(slot.Index) : ItemStack.Empty;

            // 글자는 보여 줄 물건이 바뀔 때만 새로 만든다.
            if (stack.Item == shown.Item && stack.Count == shown.Count && stack.LoadedAmmo == shown.LoadedAmmo) return;
            shown = stack;

            string text = string.Empty;
            if (!stack.IsEmpty)
            {
                text = stack.Count > 1 ? $"{stack.Item.DisplayName} ×{stack.Count}" : stack.Item.DisplayName;
                if (stack.Item is GunData gun) text += $"   탄창 {stack.LoadedAmmo}/{gun.MagazineSize}";
                text += $"   가치 {stack.Item.Value * stack.Count}";
            }

            label.text = text;
        }
    }
}
