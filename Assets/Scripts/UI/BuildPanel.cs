using System.Collections.Generic;
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
        [SerializeField] TMP_Text messageLabel;
        [SerializeField, Min(0f)] float messageDuration = 2f;
        [SerializeField, Range(0f, 1f)] float disabledTextAlpha = 0.4f;

        readonly List<Button> rows = new();
        BuildBoard board;
        float messageTimer;

        void Awake() => rowTemplate.gameObject.SetActive(false);

        void OnEnable()
        {
            Wallet.Changed += Rebuild;
            BaseUpgrades.Changed += Rebuild;
        }

        void OnDisable()
        {
            Wallet.Changed -= Rebuild;
            BaseUpgrades.Changed -= Rebuild;
        }

        public override bool Shows(Station s) => s is BuildBoard;

        public override void Bind(Station s, PlayerInteractor player)
        {
            board = s as BuildBoard;
            ShowMessage(string.Empty);
            Rebuild();
        }

        void Update()
        {
            if (messageTimer <= 0f) return;
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f) messageLabel.text = string.Empty;
        }

        void Rebuild()
        {
            foreach (var row in rows) Destroy(row.gameObject);
            rows.Clear();
            if (board == null) return;

            titleLabel.text = board.TargetName;
            foreach (var upgrade in board.Upgrades)
            {
                if (upgrade == null) continue;
                int level = BaseUpgrades.LevelOf(upgrade);
                int price = upgrade.PriceAt(level);
                bool usable = price >= 0 && Wallet.CanAfford(price);

                string step = upgrade.MaxLevel > 1 ? $" {level}/{upgrade.MaxLevel}" : string.Empty;
                string state = price >= 0 ? price.ToString() : "완료";

                Button row = Instantiate(rowTemplate, rowParent);
                row.gameObject.SetActive(true);
                TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
                label.text = $"{upgrade.DisplayName}{step}  {upgrade.Effect}   {state}";
                label.alpha = usable ? 1f : disabledTextAlpha;
                row.interactable = usable;
                BaseUpgradeData target = upgrade;
                row.onClick.AddListener(() => Build(target));
                rows.Add(row);
            }
        }

        void Build(BaseUpgradeData upgrade)
        {
            string failed = BaseUpgrades.TryUpgrade(upgrade);
            ShowMessage(failed ?? $"{upgrade.DisplayName} 지었다");
        }

        void ShowMessage(string text)
        {
            messageLabel.text = text;
            messageTimer = string.IsNullOrEmpty(text) ? 0f : messageDuration;
        }
    }
}
