using System;
using DustScrapper.Items;
using DustScrapper.Progress;
using UnityEngine;

namespace DustScrapper.Crafting
{
    /// <summary>
    /// 제작법 하나: 재료(물건·개수 목록) → 결과(물건·개수). 배워야 하는 제작법은 업그레이드(건설판에서 돈으로 배움)를 가리킨다.
    /// 새 제작법은 이 데이터를 만들어 제작 목록(Workbench)에 넣는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Crafting/Recipe", fileName = "Recipe")]
    public sealed class RecipeData : ScriptableObject
    {
        [Serializable]
        public struct Ingredient
        {
            public ItemData item;
            [Min(1)] public int count;
        }

        [SerializeField] Ingredient[] inputs;
        [SerializeField] ItemData output;
        [SerializeField, Min(1)] int outputCount = 1;

        [Tooltip("이 업그레이드(1단계 이상)를 지어야 만들 수 있다. 비우면 처음부터 안다.")]
        [SerializeField] BaseUpgradeData unlock;

        public Ingredient[] Inputs => inputs;
        public ItemData Output => output;
        public int OutputCount => outputCount;
        public BaseUpgradeData Unlock => unlock;

        public bool IsKnown => unlock == null || BaseUpgrades.LevelOf(unlock) > 0;
    }
}
