using System.Text;
using DustScrapper.Crafting;
using DustScrapper.Interaction;
using DustScrapper.Items;
using DustScrapper.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 제작 창(C): 제작법 줄("결과 ← 재료 N, 재료 N"), 누르면 만든다. 재료가 모자라면 흐리고, 아직 모르는 제작법은 "(건설판에서 배움)"으로 보인다.
    /// 가방이 바뀌거나 업그레이드를 지으면 다시 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CraftPanel : StationPanel
    {
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] Button rowTemplate;
        [SerializeField] RectTransform rowParent;

        ButtonRows rows;
        Workbench bench;
        Inventory bag;

        void Awake() => rows = new ButtonRows(rowTemplate, rowParent);

        void OnEnable() => BaseUpgrades.Changed += MarkDirty;
        void OnDisable() => BaseUpgrades.Changed -= MarkDirty;

        public override bool Shows(Station s) => s is Workbench;

        public override void Bind(Station s, PlayerInteractor player)
        {
            bench = s as Workbench;
            bag = bench != null && player != null && player.Gear != null ? player.Gear.Bag : null;
            Watch(bag);
            ShowMessage(string.Empty);
            MarkDirty();
        }

        protected override void Redraw()
        {
            rows.Clear();
            if (bench == null) return;

            titleLabel.text = bench.TargetName;
            foreach (var recipe in bench.Recipes)
            {
                if (recipe == null || recipe.Output == null) continue;
                bool known = recipe.IsKnown;

                var text = new StringBuilder();
                text.Append(recipe.Output.DisplayName);
                if (recipe.OutputCount > 1) text.Append(" ×").Append(recipe.OutputCount);
                text.Append("  ←  ");
                for (int i = 0; i < recipe.Inputs.Length; i++)
                {
                    var input = recipe.Inputs[i];
                    if (input.item == null) continue;
                    if (i > 0) text.Append(", ");
                    text.Append(input.item.DisplayName).Append(' ').Append(input.count);
                }
                if (!known) text.Append("   (건설판에서 배움)");

                RecipeData target = recipe;
                Button row = rows.Add(text.ToString(), () => Craft(target));
                SetUsable(row, known && Workbench.HasInputs(recipe, bag));
            }
        }

        void Craft(RecipeData recipe)
        {
            string failed = bench.TryCraft(recipe, bag);
            ShowMessage(failed ?? $"{recipe.Output.DisplayName} 만들었다");
        }
    }
}
