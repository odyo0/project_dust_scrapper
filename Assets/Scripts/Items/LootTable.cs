using System;
using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 상자·바닥에서 나올 물건의 목록과 확률. 판마다 Roll로 새로 뽑아서 같은 맵이어도 나오는 물건이 달라진다.
    /// 무게(weight)가 클수록 자주 나오고, 한 번 뽑힐 때 개수는 최소~최대 사이에서 정한다.
    /// 구역마다 다른 표를 두어 "멀고 위험한 곳일수록 좋은 물건"을 데이터로 정한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Items/Loot Table", fileName = "LootTable")]
    public sealed class LootTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public ItemData item;
            [Min(0)] public int weight;
            [Min(1)] public int minCount;
            [Min(1)] public int maxCount;
        }

        [Tooltip("한 상자에서 뽑는 횟수 (최소~최대). 같은 물건이 또 뽑히면 겹쳐 담긴다.")]
        [SerializeField, Min(0)] int minRolls = 1;
        [SerializeField, Min(0)] int maxRolls = 3;

        [SerializeField] List<Entry> entries = new();

        /// <summary>물건 묶음들을 새로 뽑는다(최대 rollsOverride번, 0 이하이면 표의 횟수).</summary>
        public List<ItemStack> Roll(int rollsOverride = 0)
        {
            var result = new List<ItemStack>();
            int rolls = rollsOverride > 0 ? rollsOverride : UnityEngine.Random.Range(minRolls, maxRolls + 1);
            for (int i = 0; i < rolls; i++)
            {
                ItemStack stack = RollOne();
                if (!stack.IsEmpty) result.Add(stack);
            }
            return result;
        }

        /// <summary>물건 한 묶음을 뽑는다. 표가 비었으면 빈 묶음.</summary>
        public ItemStack RollOne()
        {
            int total = 0;
            foreach (var entry in entries)
            {
                if (entry.item != null) total += entry.weight;
            }
            if (total <= 0) return ItemStack.Empty;

            int pick = UnityEngine.Random.Range(0, total);
            foreach (var entry in entries)
            {
                if (entry.item == null) continue;
                pick -= entry.weight;
                if (pick >= 0) continue;
                int count = UnityEngine.Random.Range(entry.minCount, Mathf.Max(entry.minCount, entry.maxCount) + 1);
                return new ItemStack(entry.item, Mathf.Min(count, entry.item.MaxStack));
            }
            return ItemStack.Empty;
        }
    }
}
