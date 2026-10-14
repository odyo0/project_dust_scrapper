using System.Collections.Generic;
using DustScrapper.Interaction;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Crafting
{
    /// <summary>
    /// 몸에 지니고 다니는 제작(C키). 기지 시설과 같은 틀(Station)을 써서 가방 화면이 제작 창(CraftPanel)과 함께 열린다.
    /// E로 고르는 대상이 아니라서(IsAvailable = false) 손 닿는 물체 목록에 끼지 않는다.
    /// 재료는 가방에서 꺼내고 결과물은 가방에 넣는다. 지금은 바로 만들어진다(제작 시간·소음은 기획 빈칸).
    /// </summary>
    public sealed class Workbench : Station
    {
        [SerializeField] List<RecipeData> recipes = new();

        public IReadOnlyList<RecipeData> Recipes => recipes;

        public override bool IsAvailable => false;
        public override string ActionName => "제작";
        public override string TargetName => "제작";
        public override string BlockedReason(PlayerInteractor interactor) => null;

        /// <summary>재료가 다 있는가.</summary>
        public static bool HasInputs(RecipeData recipe, Inventory bag)
        {
            if (bag == null) return false;
            foreach (var input in recipe.Inputs)
            {
                if (input.item != null && bag.CountOf(input.item) < input.count) return false;
            }
            return true;
        }

        /// <summary>만든다. 모르는 제작법이거나 재료·자리가 모자라면 이유를, 만들었으면 null.</summary>
        public string TryCraft(RecipeData recipe, Inventory bag)
        {
            if (!recipe.IsKnown) return "아직 모르는 제작법이다";
            if (!HasInputs(recipe, bag)) return "재료가 모자란다";

            // 재료를 빼면 자리가 나므로, 결과물이 재료와 같은 칸에 들어갈 수도 있다. 넉넉히 보려고 먼저 빼고, 안 들어가면 되돌린다.
            foreach (var input in recipe.Inputs) bag.Remove(input.item, input.count);
            int left = bag.Add(new ItemStack(recipe.Output, recipe.OutputCount));
            if (left <= 0) return null;

            bag.Remove(recipe.Output, recipe.OutputCount - left);
            foreach (var input in recipe.Inputs) bag.Add(new ItemStack(input.item, input.count));
            return "넣을 자리가 없다";
        }
    }
}
