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
    /// 충전 창: 가진 정제된 글로우 개수, 내구도가 있는 장비 목록(몸·가방·보관함, "이름  남은/최대   충전 · 글로우 N"), 결과 한 줄.
    /// 줄을 누르면 충전한다. 닳지 않았거나 글로우가 모자란 줄은 누를 수 없고 흐리다.
    /// 장비·글로우가 있는 칸이 바뀌면 목록을 다시 만든다(장비를 옮기거나 닳아도 맞게 보인다).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChargePanel : StationPanel
    {
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text materialLabel;
        [Tooltip("장비 한 줄의 견본. 꺼 둔 채로 두면 이것을 복제해 줄을 만든다. 자식에 글자(TMP_Text)가 하나 있어야 한다.")]
        [SerializeField] Button rowTemplate;
        [SerializeField] RectTransform rowParent;
        [Tooltip("장비가 하나도 없을 때 보이는 글")]
        [SerializeField] TMP_Text emptyLabel;
        [SerializeField] TMP_Text messageLabel;
        [SerializeField, Min(0f)] float messageDuration = 2f;
        [SerializeField, Range(0f, 1f)] float disabledTextAlpha = 0.4f;

        readonly List<Button> rows = new();
        readonly List<IItemSlots> watched = new();
        ChargeStation station;
        PlayerInteractor player;
        bool dirty;
        float messageTimer;

        void Awake() => rowTemplate.gameObject.SetActive(false);

        void OnDestroy() => Watch(null);

        public override bool Shows(Station s) => s is ChargeStation;

        public override void Bind(Station s, PlayerInteractor who)
        {
            station = s as ChargeStation;
            player = station != null ? who : null;
            Watch(station);
            ShowMessage(string.Empty);
            dirty = true;
            if (station != null) Rebuild();
        }

        void LateUpdate()
        {
            if (dirty && station != null) Rebuild();

            if (messageTimer <= 0f) return;
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f) messageLabel.text = string.Empty;
        }

        /// <summary>장비·글로우가 있을 수 있는 칸 묶음(몸, 가방, 보관함)이 바뀌면 다음 프레임 끝에 목록을 다시 만든다.</summary>
        void Watch(ChargeStation s)
        {
            foreach (var slots in watched) slots.Changed -= MarkDirty;
            watched.Clear();
            if (s == null) return;
            if (player != null && player.Gear != null && player.Gear.Body != null) watched.Add(player.Gear.Body);
            if (player != null && player.Gear != null && player.Gear.Bag != null) watched.Add(player.Gear.Bag);
            if (s.Storage != null) watched.Add(s.Storage);
            foreach (var slots in watched) slots.Changed += MarkDirty;
        }

        void MarkDirty() => dirty = true;

        void Rebuild()
        {
            dirty = false;
            foreach (var row in rows) Destroy(row.gameObject);
            rows.Clear();

            titleLabel.text = station.TargetName;
            Inventory bag = player != null && player.Gear != null ? player.Gear.Bag : null;
            int material = station.MaterialCount(bag);
            materialLabel.text = $"{station.ChargeMaterial.DisplayName} {material}";

            List<ChargeStation.GearSlot> gear = station.FindGear(player);
            emptyLabel.gameObject.SetActive(gear.Count == 0);
            foreach (var slot in gear)
            {
                ItemStack stack = slot.Stack;
                int cost = slot.Gear.ChargeCostOf(stack);
                bool usable = cost > 0 && material >= cost;

                Button row = Instantiate(rowTemplate, rowParent);
                row.gameObject.SetActive(true);
                TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
                string state = cost > 0 ? $"충전 · {cost}개" : "새것";
                label.text = $"{stack.Item.DisplayName}  {slot.Gear.DurabilityOf(stack):0}/{slot.Gear.MaxDurability:0}   {state}";
                label.alpha = usable ? 1f : disabledTextAlpha;
                row.interactable = usable;
                ChargeStation.GearSlot target = slot;
                row.onClick.AddListener(() => Charge(target));
                rows.Add(row);
            }
        }

        void Charge(ChargeStation.GearSlot slot)
        {
            string name = slot.Stack.Item.DisplayName;
            string failed = station.TryCharge(slot, player != null && player.Gear != null ? player.Gear.Bag : null);
            ShowMessage(failed ?? $"{name} 충전했다");
        }

        void ShowMessage(string text)
        {
            messageLabel.text = text;
            messageTimer = string.IsNullOrEmpty(text) ? 0f : messageDuration;
        }
    }
}
