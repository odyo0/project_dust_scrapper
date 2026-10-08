using System;
using DustScrapper.Items;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Interaction
{
    /// <summary>
    /// 손이 닿는 상호작용 대상 중 가장 가까운 하나를 고르고, E를 누르고 있는 시간을 잰다.
    /// 다 누르면 대상에게 맡기고, 대상은 이것을 통해 플레이어의 가방·무기 슬롯에 접근한다.
    /// 입력은 두뇌(PlayerBrain)가 SetHeld로 알려 준다. 고른 대상과 진행도는 화면 표시(InteractionPrompt)가 읽는다.
    /// 상자를 열면 LootOpened로 알리고, 가방 화면이 그것을 듣고 상자 칸을 띄운다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Inventory inventory;
        [SerializeField] WeaponLoadout loadout;

        [Tooltip("손이 닿는 거리 (칸)")]
        [SerializeField, Min(0.1f)] float reach = 1.2f;

        [Tooltip("이 레이어(벽) 너머의 것은 손이 닿지 않는다")]
        [SerializeField] LayerMask blockers;

        bool held;
        bool needsRelease;
        float holdTimer;

        public Inventory Inventory => inventory;
        public WeaponLoadout Loadout => loadout;

        /// <summary>지금 고른 대상. 없으면 null.</summary>
        public Interactable Target { get; private set; }

        /// <summary>고른 대상을 지금 할 수 없는 이유. 할 수 있으면 null.</summary>
        public string BlockedReason { get; private set; }

        /// <summary>누르고 있는 진행도 (0~1).</summary>
        public float Progress => Target != null && Target.HoldTime > 0f ? Mathf.Clamp01(holdTimer / Target.HoldTime) : 0f;

        /// <summary>상자를 열었을 때. 값은 연 상자의 칸.</summary>
        public event Action<Inventory> LootOpened;

        public void SetHeld(bool value) => held = value;

        /// <summary>상자 같은 대상이 자기 칸을 플레이어에게 보여 달라고 할 때 부른다.</summary>
        public void OpenLoot(Inventory loot) => LootOpened?.Invoke(loot);

        void Awake()
        {
            if (inventory == null) inventory = GetComponent<Inventory>();
            if (loadout == null) loadout = GetComponent<WeaponLoadout>();
        }

        void OnDisable()
        {
            held = false;
            needsRelease = false;
            holdTimer = 0f;
            Target = null;
            BlockedReason = null;
        }

        void Update()
        {
            Interactable next = FindTarget();
            if (next != Target)
            {
                Target = next;
                holdTimer = 0f;
            }
            BlockedReason = Target != null ? Target.BlockedReason(this) : null;

            if (!held)
            {
                needsRelease = false;
                holdTimer = 0f;
                return;
            }
            if (Target == null || BlockedReason != null || needsRelease)
            {
                holdTimer = 0f;
                return;
            }

            holdTimer += Time.deltaTime;
            if (holdTimer < Target.HoldTime) return;

            // 한 번 끝내면 키를 뗐다 다시 눌러야 다음 것을 한다.
            // 무기를 바꿔 주울 때 떨어뜨린 무기를 곧바로 다시 줍는 식으로 계속 반복되지 않게.
            holdTimer = 0f;
            needsRelease = true;
            Target.Interact(this);
        }

        Interactable FindTarget()
        {
            Vector2 origin = transform.position;
            Interactable best = null;
            float bestDistance = reach;

            foreach (var candidate in Interactable.Active)
            {
                float distance = Vector2.Distance(origin, candidate.Position);
                if (distance > bestDistance) continue;
                // 상자처럼 대상 자신이 벽 레이어에 있으면 첫 번째로 부딪히는 게 대상이다. 그건 막힌 게 아니다.
                Collider2D hit = Physics2D.Linecast(origin, candidate.Position, blockers).collider;
                if (hit != null && !hit.transform.IsChildOf(candidate.transform)) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }
    }
}
