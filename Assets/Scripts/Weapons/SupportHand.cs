using System;
using DustScrapper.Actors;
using DustScrapper.Combat;
using DustScrapper.Items;
using DustScrapper.Magic;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 보조 손(왼손잡이 기준 오른손)과 보조 칸 1칸. 보조 칸에 낀 장비(오른손 글러브, 스태프 등)를 손에 들고,
    /// 든 장비의 기본 스킬은 우클릭, 특수 스킬은 F다.
    /// 보조 칸이 비었거나, 낀 장비를 지금 쓸 수 없으면(스태프 + 두손 무기, SkillRouter가 정함) 빈손 장비(우클릭 주먹질, F 기본 공격 마법)를 든다.
    /// 무기 손의 빈손(주먹질만)과는 다른 장비다. 한 손으로는 늘 마법을 쏠 수 있게 마법은 보조 손 빈손에만 있다.
    /// 칸 창구(IItemSlots)를 따라서 가방 화면에서 가방·무기 칸과 같은 방식으로 옮겨진다.
    /// 장비를 만들고 꺼내는 방법은 무기 손(WeaponLoadout)과 같다(Weapon.Spawn).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SupportHand : MonoBehaviour, IWeaponHolder, IItemSlots
    {
        [Tooltip("장비를 붙일 보조 손 위치. 상체 아래에 두어 조준을 따라 돌게 한다. 손잡이는 이 위치와 무기 손 위치를 맞바꿔 정한다.")]
        [SerializeField] Transform hand;
        [SerializeField] BodyAim aim;
        [Tooltip("마법이 쓸 MP. 비워 두면 같은 물체에서 찾는다.")]
        [SerializeField] Mana mana;
        [Tooltip("디버프. 공격을 막는 디버프가 걸리면 쓰지 못한다. 비워 두면 같은 물체에서 찾는다.")]
        [SerializeField] StatusEffects statusEffects;

        [Tooltip("보조 칸이 비었을 때 드는 빈손 장비 (우클릭 주먹질, F 기본 공격 마법).")]
        [SerializeField] WeaponData bareHand;

        Weapon bareWeapon;
        Weapon equipped;
        bool forcedBare;
        bool primaryHeld;
        bool specialHeld;

        /// <summary>지금 보조 손에 든 장비. 빈손이면 빈손 장비.</summary>
        public Weapon Current { get; private set; }

        /// <summary>보조 칸에 낀 장비의 데이터. 비었으면 null. 지금 쓰지 못해 빈손을 들고 있어도 칸에는 그대로 있다.</summary>
        public WeaponData EquippedData => equipped != null ? equipped.Data : null;

        public Vector2 AimDirection => aim.AimDirection;

        /// <summary>보조 손 장비는 탄을 쓰지 않는다.</summary>
        public Inventory AmmoSource => null;

        public Mana Mana => mana;
        public bool AttacksSuppressed => statusEffects != null && !statusEffects.CanAttack;

        public int SlotCount => 1;

        /// <summary>보조 손에 든 장비가 바뀌었을 때.</summary>
        public event Action<Weapon> CurrentChanged;

        /// <summary>보조 칸에 장비가 들어오거나 빠졌을 때.</summary>
        public event Action Changed;

        void Awake()
        {
            if (aim == null) aim = GetComponent<BodyAim>();
            if (mana == null) mana = GetComponent<Mana>();
            if (statusEffects == null) statusEffects = GetComponent<StatusEffects>();
            if (bareHand != null) bareWeapon = Weapon.Spawn(bareHand, hand, this);
            RefreshHeld();
        }

        void OnEnable()
        {
            // 죽었다 살아나는 등 꺼졌다 켜졌을 때 다시 꺼낸다.
            if (Current != null && !Current.gameObject.activeSelf) Current.Draw(false, false);
            if (statusEffects != null) statusEffects.AttacksStopped += OnAttacksStopped;
        }

        void OnDisable()
        {
            if (statusEffects != null) statusEffects.AttacksStopped -= OnAttacksStopped;
            primaryHeld = false;
            specialHeld = false;
            if (Current != null) Current.Holster();
        }

        /// <summary>기본 스킬 키(플레이어는 우클릭)를 누르고 있는가.</summary>
        public void SetPrimary(bool held)
        {
            primaryHeld = held;
            if (Current != null) Current.SetPrimary(held);
        }

        /// <summary>특수 스킬 키(플레이어는 F)를 누르고 있는가.</summary>
        public void SetSpecial(bool held)
        {
            specialHeld = held;
            if (Current != null) Current.SetSpecial(held);
        }

        /// <summary>
        /// 보조 칸 장비를 쓰지 못하게 하고 빈손을 들게 한다(스태프 + 두손 무기). 규칙은 SkillRouter가 정하고 여기서는 따르기만 한다.
        /// 장비는 칸에 그대로 남아서, 조건이 풀리면 다시 든다.
        /// </summary>
        public void SetForcedBare(bool value)
        {
            if (forcedBare == value) return;
            forcedBare = value;
            RefreshHeld();
        }

        /// <summary>
        /// 장비를 빈 보조 칸에 끼운다(바닥에서 주울 때). 끼웠으면 빈 묶음, 칸이 차 있거나 낄 수 없으면 그대로 돌려준다.
        /// 낀 장비와 맞바꾸지 않는다(기획: 주운 장비는 가방으로, 그건 부른 쪽 몫이다).
        /// </summary>
        public ItemStack PutItem(ItemStack stack)
        {
            if (equipped != null || !CanPlace(0, stack)) return stack;
            PlaceInEmptySlot(0, stack);
            return ItemStack.Empty;
        }

        /// <summary>보조 손 장비는 아직 던지는 등 손에서 놓는 것이 없다.</summary>
        public bool TryRelease(Weapon weapon, out ItemStack stack)
        {
            stack = ItemStack.Empty;
            return false;
        }

        public ItemStack GetSlot(int index) => index == 0 && equipped != null ? new ItemStack(equipped.Data, 1) : ItemStack.Empty;

        /// <summary>보조 칸에는 보조 칸에 맞는 장비만 놓인다(오른손 글러브, 스태프 등).</summary>
        public bool CanPlace(int index, ItemStack stack) => index == 0 && stack.Item is WeaponData data && data.FitsSupportSlot;

        public ItemStack TakeSlot(int index)
        {
            if (index != 0 || equipped == null) return ItemStack.Empty;

            var stack = new ItemStack(equipped.Data, 1);
            Weapon removed = equipped;
            equipped = null;
            RefreshHeld();
            removed.Holster();
            Destroy(removed.gameObject);
            Changed?.Invoke();
            return stack;
        }

        public void PlaceInEmptySlot(int index, ItemStack stack)
        {
            if (equipped != null || !CanPlace(index, stack)) return;
            equipped = Weapon.Spawn((WeaponData)stack.Item, hand, this);
            RefreshHeld();
            Changed?.Invoke();
        }

        void OnAttacksStopped()
        {
            if (Current != null) Current.Interrupt();
        }

        /// <summary>칸 장비를 쓸 수 있으면 그것을, 아니면 빈손을 든다.</summary>
        void RefreshHeld()
        {
            Weapon wanted = equipped != null && !forcedBare ? equipped : bareWeapon;
            if (wanted == Current) return;

            if (Current != null) Current.Holster();
            Current = wanted;
            if (Current != null) Current.Draw(primaryHeld, specialHeld);
            CurrentChanged?.Invoke(Current);
        }
    }
}
