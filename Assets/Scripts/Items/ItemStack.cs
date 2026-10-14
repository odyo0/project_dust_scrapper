using System;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 물건 한 묶음: 무엇이 몇 개인지. 가방 한 칸, 바닥 물건 하나가 이것 하나다.
    /// 총은 탄창에 남은 탄 수, 내구도가 있는 장비는 닳은 양도 함께 들고 다녀서, 버렸다 다시 주워도 그대로다.
    /// 값을 바꿀 때는 새 묶음을 만든다(칸끼리 같은 묶음을 나눠 갖다가 한쪽만 바뀌는 일이 없게).
    /// </summary>
    [Serializable]
    public struct ItemStack
    {
        [SerializeField] ItemData item;
        [SerializeField, Min(1)] int count;

        [Tooltip("총이면 탄창에 든 탄 수. 다른 물건은 쓰지 않는다.")]
        [SerializeField, Min(0)] int loadedAmmo;

        [Tooltip("내구도가 있는 장비(마법진)가 닳은 양. 0이면 새것이다. 남은 내구도 = 최대 내구도 - 닳은 양.")]
        [SerializeField, Min(0f)] float wear;

        public ItemStack(ItemData item, int count, int loadedAmmo = 0, float wear = 0f)
        {
            this.item = item;
            this.count = count;
            this.loadedAmmo = loadedAmmo;
            this.wear = wear;
        }

        public static ItemStack Empty => default;

        public ItemData Item => item;
        public int Count => count;
        public int LoadedAmmo => loadedAmmo;

        /// <summary>닳은 양. 새것이면 0. "남은 내구도"가 아니라 "닳은 양"을 들고 있어서, 새로 만든 물건이 따로 채우지 않아도 새것이다.</summary>
        public float Wear => wear;
        public bool IsEmpty => item == null || count <= 0;

        /// <summary>개수만 바꾼 묶음. 0 이하이면 빈 묶음.</summary>
        public ItemStack WithCount(int newCount) => newCount > 0 ? new ItemStack(item, newCount, loadedAmmo, wear) : Empty;

        /// <summary>닳은 양만 바꾼 묶음.</summary>
        public ItemStack WithWear(float newWear) => IsEmpty ? Empty : new ItemStack(item, count, loadedAmmo, Mathf.Max(0f, newWear));

        /// <summary>물건별 상태(탄창, 닳은 양)까지 같은가. 화면이 다시 그릴지 정할 때 쓴다.</summary>
        public bool SameAs(ItemStack other) =>
            item == other.item && count == other.count && loadedAmmo == other.loadedAmmo && Mathf.Approximately(wear, other.wear);
    }
}
