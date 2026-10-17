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

        ButtonRows rows;
        ChargeStation station;
        PlayerInteractor player;

        Inventory Bag => player != null && player.Gear != null ? player.Gear.Bag : null;

        void Awake() => rows = new ButtonRows(rowTemplate, rowParent);

        public override bool Shows(Station s) => s is ChargeStation;

        public override void Bind(Station s, PlayerInteractor who)
        {
            station = s as ChargeStation;
            player = station != null ? who : null;
            if (station != null) Watch(player != null && player.Gear != null ? player.Gear.Body : null, Bag, station.Storage);
            else Watch();
            ShowMessage(string.Empty);
            MarkDirty();
        }

        protected override void Redraw()
        {
            rows.Clear();
            if (station == null) return;

            titleLabel.text = station.TargetName;
            int material = station.MaterialCount(Bag);
            materialLabel.text = $"{station.ChargeMaterial.DisplayName} {material}";

            List<ChargeStation.GearSlot> gear = station.FindGear(player);
            emptyLabel.gameObject.SetActive(gear.Count == 0);
            foreach (var slot in gear)
            {
                ItemStack stack = slot.Stack;
                int cost = slot.Gear.ChargeCostOf(stack);
                string state = cost > 0 ? $"충전 · {cost}개" : "새것";
                ChargeStation.GearSlot target = slot;
                Button row = rows.Add($"{stack.Item.DisplayName}  {slot.Gear.DurabilityOf(stack):0}/{slot.Gear.MaxDurability:0}   {state}", () => Charge(target));
                SetUsable(row, cost > 0 && material >= cost);
            }
        }

        void Charge(ChargeStation.GearSlot slot)
        {
            string name = slot.Stack.Item.DisplayName;
            string failed = station.TryCharge(slot, Bag);
            ShowMessage(failed ?? $"{name} 충전했다");
        }
    }
}
