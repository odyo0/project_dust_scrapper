using System.Collections.Generic;
using DustScrapper.Interaction;
using DustScrapper.Items;
using DustScrapper.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 정제 창: 가진 원석·정제된 글로우 개수, 비율("원석 2 → 정제된 글로우 1"), "1개 정제"·"모두 정제" 버튼, 결과 한 줄.
    /// 원석이 모자라면 버튼이 흐려진다. 가방·보관함이 바뀌면 다음 프레임 끝에 다시 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RefinePanel : StationPanel
    {
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text countLabel;
        [SerializeField] TMP_Text rateLabel;
        [SerializeField] Button refineOneButton;
        [SerializeField] Button refineAllButton;
        [SerializeField] TMP_Text messageLabel;
        [SerializeField, Min(0f)] float messageDuration = 2f;
        [SerializeField, Range(0f, 1f)] float disabledTextAlpha = 0.4f;

        readonly List<IItemSlots> watched = new();
        RefineStation station;
        Inventory bag;
        bool dirty;
        float messageTimer;

        void Awake()
        {
            refineOneButton.onClick.AddListener(() => Refine(1));
            refineAllButton.onClick.AddListener(() => Refine(station != null ? station.MaxBatches(bag) : 0));
        }

        void OnDestroy() => Watch(null);

        public override bool Shows(Station s) => s is RefineStation;

        public override void Bind(Station s, PlayerInteractor player)
        {
            station = s as RefineStation;
            bag = station != null && player != null && player.Gear != null ? player.Gear.Bag : null;
            Watch(station);
            ShowMessage(string.Empty);
            if (station != null) Refresh();
        }

        void LateUpdate()
        {
            if (dirty && station != null) Refresh();

            if (messageTimer <= 0f) return;
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f) messageLabel.text = string.Empty;
        }

        void Watch(RefineStation s)
        {
            foreach (var slots in watched) slots.Changed -= MarkDirty;
            watched.Clear();
            if (s == null) return;
            if (bag != null) watched.Add(bag);
            if (s.Storage != null) watched.Add(s.Storage);
            foreach (var slots in watched) slots.Changed += MarkDirty;
        }

        void MarkDirty() => dirty = true;

        void Refresh()
        {
            dirty = false;
            titleLabel.text = station.TargetName;
            int outputs = (bag != null ? bag.CountOf(station.Output) : 0) + (station.Storage != null ? station.Storage.CountOf(station.Output) : 0);
            countLabel.text = $"{station.Input.DisplayName} {station.InputCount(bag)}   {station.Output.DisplayName} {outputs}";
            rateLabel.text = $"{station.Input.DisplayName} {station.InputPerOutput}개 → {station.Output.DisplayName} 1개";

            int max = station.MaxBatches(bag);
            SetButton(refineOneButton, "1개 정제", max >= 1);
            SetButton(refineAllButton, max > 0 ? $"모두 정제 ({max}개)" : "모두 정제", max >= 1);
        }

        void SetButton(Button button, string text, bool usable)
        {
            button.interactable = usable;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = text;
            label.alpha = usable ? 1f : disabledTextAlpha;
        }

        void Refine(int batches)
        {
            if (station == null) return;
            string failed = station.TryRefine(batches, bag);
            ShowMessage(failed ?? $"{station.Output.DisplayName} {batches}개 만들었다");
        }

        void ShowMessage(string text)
        {
            messageLabel.text = text;
            messageTimer = string.IsNullOrEmpty(text) ? 0f : messageDuration;
        }
    }
}
