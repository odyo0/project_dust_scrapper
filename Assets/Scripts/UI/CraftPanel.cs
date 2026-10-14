using System.Collections.Generic;
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
        [SerializeField] TMP_Text messageLabel;
        [SerializeField, Min(0f)] float messageDuration = 2f;
        [SerializeField, Range(0f, 1f)] float disabledTextAlpha = 0.4f;

        readonly List<Button> rows = new();
        Workbench bench;
        Inventory bag;
        bool dirty;
        float messageTimer;

        void Awake() => rowTemplate.gameObject.SetActive(false);

        void OnEnable() => BaseUpgrades.Changed += MarkDirty;
        void OnDisable() => BaseUpgrades.Changed -= MarkDirty;
        void OnDestroy() => Watch(null);

        public override bool Shows(Station s) => s is Workbench;

        public override void Bind(Station s, PlayerInteractor player)
        {
            Watch(null);
            bench = s as Workbench;
            bag = bench != null && player != null ? player.Inventory : null;
            Watch(bag);
            ShowMessage(string.Empty);
            if (bench != null) Rebuild();
        }

        void Watch(Inventory newBag)
        {
            if (bag != null) bag.Changed -= MarkDirty;
            if (newBag != null) newBag.Changed += MarkDirty;
        }

        void MarkDirty() => dirty = true;

        void LateUpdate()
        {
            if (dirty && bench != null) Rebuild();

            if (messageTimer <= 0f) return;
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f) messageLabel.text = string.Empty;
        }

        void Rebuild()
        {
            dirty = false;
            foreach (var row in rows) Destroy(row.gameObject);
            rows.Clear();
            titleLabel.text = bench.TargetName;

            foreach (var recipe in bench.Recipes)
            {
                if (recipe == null || recipe.Output == null) continue;
                bool known = recipe.IsKnown;
                bool usable = known && Workbench.HasInputs(recipe, bag);

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

                Button row = Instantiate(rowTemplate, rowParent);
                row.gameObject.SetActive(true);
                TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
                label.text = text.ToString();
                label.alpha = usable ? 1f : disabledTextAlpha;
                row.interactable = usable;
                RecipeData target = recipe;
                row.onClick.AddListener(() => Craft(target));
                rows.Add(row);
            }
        }

        void Craft(RecipeData recipe)
        {
            string failed = bench.TryCraft(recipe, bag);
            ShowMessage(failed ?? $"{recipe.Output.DisplayName} 만들었다");
        }

        void ShowMessage(string text)
        {
            messageLabel.text = text;
            messageTimer = string.IsNullOrEmpty(text) ? 0f : messageDuration;
        }
    }
}
