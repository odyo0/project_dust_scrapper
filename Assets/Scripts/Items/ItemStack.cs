using System;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 물건 한 묶음: 무엇이 몇 개인지. 가방 한 칸, 바닥 물건 하나가 이것 하나다.
    /// 총은 탄창에 남은 탄 수도 함께 들고 다녀서, 버렸다 다시 주워도 탄창이 그대로다.
    /// 값을 바꿀 때는 새 묶음을 만든다(칸끼리 같은 묶음을 나눠 갖다가 한쪽만 바뀌는 일이 없게).
    /// </summary>
    [Serializable]
    public struct ItemStack
    {
        [SerializeField] ItemData item;
        [SerializeField, Min(1)] int count;

        [Tooltip("총이면 탄창에 든 탄 수. 다른 물건은 쓰지 않는다.")]
        [SerializeField, Min(0)] int loadedAmmo;

        public ItemStack(ItemData item, int count, int loadedAmmo = 0)
        {
            this.item = item;
            this.count = count;
            this.loadedAmmo = loadedAmmo;
        }

        public static ItemStack Empty => default;

        public ItemData Item => item;
        public int Count => count;
        public int LoadedAmmo => loadedAmmo;
        public bool IsEmpty => item == null || count <= 0;

        /// <summary>개수만 바꾼 묶음. 0 이하이면 빈 묶음.</summary>
        public ItemStack WithCount(int newCount) => newCount > 0 ? new ItemStack(item, newCount, loadedAmmo) : Empty;
    }
}
