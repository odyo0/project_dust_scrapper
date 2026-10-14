using System;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 저장 파일에 적는 물건 한 묶음. ItemStack에서 물건 데이터만 이름표로 바꾼 것이다(탄창 탄 수, 닳은 양도 그대로).
    /// ItemStack에 물건별 상태(탄창 등)가 늘어나면 여기에도 같은 칸을 늘린다.
    /// </summary>
    [Serializable]
    public struct SavedStack
    {
        public string id;
        public int count;
        public int loadedAmmo;
        public float wear;

        public bool IsEmpty => string.IsNullOrEmpty(id) || count <= 0;

        public static SavedStack From(ItemStack stack)
        {
            if (stack.IsEmpty) return default;
            ItemCatalog catalog = ItemCatalog.Instance;
            if (catalog != null && !catalog.Contains(stack.Item))
            {
                Debug.LogWarning($"{stack.Item.name}이(가) 물건 목록(ItemCatalog)에 없어 저장하지 못했다.");
                return default;
            }
            return new SavedStack { id = stack.Item.SaveId, count = stack.Count, loadedAmmo = stack.LoadedAmmo, wear = stack.Wear };
        }

        /// <summary>물건 묶음으로 되돌린다. 목록에 없는 이름표(지워진 물건 등)면 빈 묶음.</summary>
        public ItemStack ToStack()
        {
            if (IsEmpty) return ItemStack.Empty;
            ItemData item = ItemCatalog.Instance != null ? ItemCatalog.Instance.Find(id) : null;
            if (item == null)
            {
                Debug.LogWarning($"저장된 물건 \"{id}\"을(를) 물건 목록에서 찾지 못해 버린다.");
                return ItemStack.Empty;
            }
            return new ItemStack(item, count, loadedAmmo, wear);
        }
    }
}
