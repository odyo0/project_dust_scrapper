using DustScrapper.Items;
using DustScrapper.Lantern;
using TMPro;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 랜턴을 끼었을 때 랜턴 이름과 배터리 남은 양을 보인다("빛 랜턴  3 / 5  +2" — 끝의 +2는 가방 속 같은 배터리 수).
    /// 랜턴을 안 꼈으면(Space = 구르기) 통째로 숨긴다. 배터리 칸이 비었거나 다 떨어지면 경고색.
    /// 칸이나 가방이 바뀔 때만 다시 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LanternCounter : MonoBehaviour
    {
        [SerializeField] LanternSlots slots;
        [SerializeField] Inventory bag;
        [Tooltip("랜턴이 없을 때 숨길 묶음. 이 스크립트가 붙은 물체 자신이면 안 된다(꺼지면 다시 켤 수 없다).")]
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text label;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color emptyColor = new(1f, 0.35f, 0.3f);

        bool dirty = true;

        void Awake()
        {
            if (root == gameObject) Debug.LogError($"{name}: root에 자기 자신을 넣으면 한 번 숨긴 뒤 다시 보이지 않는다. 자식 묶음을 넣는다.", this);
        }

        void OnEnable()
        {
            if (slots != null) slots.Changed += MarkDirty;
            if (bag != null) bag.Changed += MarkDirty;
            dirty = true;
        }

        void OnDisable()
        {
            if (slots != null) slots.Changed -= MarkDirty;
            if (bag != null) bag.Changed -= MarkDirty;
        }

        void MarkDirty() => dirty = true;

        void LateUpdate()
        {
            if (!dirty) return;
            dirty = false;

            LanternData lantern = slots != null ? slots.Lantern : null;
            if (root != null && root.activeSelf != (lantern != null)) root.SetActive(lantern != null);
            if (lantern == null) return;

            ItemStack battery = slots.Battery;
            if (!(battery.Item is LanternBatteryData data))
            {
                label.text = $"{lantern.DisplayName}  배터리 없음";
                label.color = emptyColor;
                return;
            }

            int spare = 0;
            if (bag != null)
            {
                for (int i = 0; i < bag.SlotCount; i++)
                {
                    if (bag.GetSlot(i).Item == data) spare++;
                }
            }
            float charge = data.ChargeOf(battery);
            label.text = spare > 0
                ? $"{lantern.DisplayName}  {charge:0.#} / {data.Capacity:0.#}  +{spare}"
                : $"{lantern.DisplayName}  {charge:0.#} / {data.Capacity:0.#}";
            label.color = charge > 0f ? normalColor : emptyColor;
        }
    }
}
