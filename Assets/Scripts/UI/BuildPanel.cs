using DustScrapper.Interaction;
using DustScrapper.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 건설 창: 시설·업그레이드 줄("이름 단계  효과   값" 또는 "완료"), 누르면 짓는다. 돈이 모자라거나 다 지은 줄은 흐리다.
    /// 돈이나 단계가 바뀌면 다시 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildPanel : StationPanel
    {
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] Button rowTemplate;
        [SerializeField] RectTransform rowParent;

        ButtonRows rows;
        BuildBoard board;

        void Awake() => rows = new ButtonRows(rowTemplate, rowParent);

        void OnEnable()
        {
            Wallet.Changed += MarkDirty;
            BaseUpgrades.Changed += MarkDirty;
        }

        void OnDisable()
        {
            Wallet.Changed -= MarkDirty;
            BaseUpgrades.Changed -= MarkDirty;
        }

        public override bool Shows(Station s) => s is BuildBoard;

        public override void Bind(Station s, PlayerInteractor player)
        {
            board = s as BuildBoard;
            ShowMessage(string.Empty);
            MarkDirty();
        }

        protected override void Redraw()
        {
            rows.Clear();
            if (board == null) return;

            titleLabel.text = board.TargetName;
            foreach (var upgrade in board.Upgrades)
            {
                if (upgrade == null) continue;
                int level = BaseUpgrades.LevelOf(upgrade);
                int price = upgrade.PriceAt(level);

                string step = upgrade.MaxLevel > 1 ? $" {level}/{upgrade.MaxLevel}" : string.Empty;
                string state = price >= 0 ? price.ToString() : "완료";
                BaseUpgradeData target = upgrade;
                Button row = rows.Add($"{upgrade.DisplayName}{step}  {upgrade.Effect}   {state}", () => Build(target));
                SetUsable(row, price >= 0 && Wallet.CanAfford(price));
            }
        }

        void Build(BaseUpgradeData upgrade)
        {
            string failed = BaseUpgrades.TryUpgrade(upgrade);
            ShowMessage(failed ?? $"{upgrade.DisplayName} 지었다");
        }
    }
}
