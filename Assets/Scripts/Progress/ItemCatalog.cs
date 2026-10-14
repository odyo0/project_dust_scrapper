using System.Collections.Generic;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Progress
{
    /// <summary>
    /// 게임에 있는 모든 물건 데이터의 목록. 저장 파일에는 물건 데이터를 그대로 쓸 수 없어서 이름표(ItemData.SaveId)만 적고,
    /// 불러올 때 이 목록에서 이름표로 데이터를 찾아온다.
    /// 물건 데이터를 새로 만들면 여기에 넣어야 저장된다(빠진 물건은 저장할 때 경고하고 버린다).
    /// 씬마다 연결하지 않게 Resources 폴더의 "ItemCatalog" 한 개를 모두가 같이 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Progress/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        const string ResourcePath = "ItemCatalog";

        [SerializeField] List<ItemData> items = new();

        static ItemCatalog instance;
        Dictionary<string, ItemData> byId;

        public static ItemCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ItemCatalog>(ResourcePath);
                    if (instance == null) Debug.LogError($"Resources/{ResourcePath} 물건 목록이 없어 저장·불러오기를 할 수 없다.");
                }
                return instance;
            }
        }

        public IReadOnlyList<ItemData> Items => items;

        /// <summary>이름표로 물건 데이터를 찾는다. 없으면 null.</summary>
        public ItemData Find(string saveId)
        {
            if (string.IsNullOrEmpty(saveId)) return null;
            if (byId == null) BuildIndex();
            return byId.TryGetValue(saveId, out ItemData item) ? item : null;
        }

        /// <summary>이 물건이 목록에 있어서 저장될 수 있는가.</summary>
        public bool Contains(ItemData item) => item != null && Find(item.SaveId) == item;

        void OnEnable() => byId = null;

        void OnValidate()
        {
            byId = null;
            var seen = new HashSet<string>();
            foreach (var item in items)
            {
                if (item != null && !seen.Add(item.SaveId)) Debug.LogWarning($"{name}: 이름표 \"{item.SaveId}\"가 겹친다. 저장된 물건이 뒤바뀔 수 있다.", this);
            }
        }

        void BuildIndex()
        {
            byId = new Dictionary<string, ItemData>();
            foreach (var item in items)
            {
                if (item != null) byId[item.SaveId] = item;
            }
        }
    }
}
