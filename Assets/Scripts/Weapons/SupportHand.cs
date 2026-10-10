using System;
using DustScrapper.Actors;
using DustScrapper.Items;
using DustScrapper.Magic;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 보조 손(왼손잡이 기준 오른손). 보조 칸에 든 장비를 손에 들고 있다. 든 장비의 기본 스킬은 우클릭, 특수 스킬은 F다.
    /// 보조 칸이 비었을 때는 빈손 장비(기본 스킬 주먹질 + 특수 스킬 기본 공격 마법)를 든다.
    /// 무기 손의 빈손(주먹질만)과는 다른 장비다. 한 손으로는 늘 마법을 쏠 수 있게 마법은 보조 손 빈손에만 있다.
    /// 지금은 빈손만 있고, 글러브·스태프를 넣는 칸(가방 화면)은 그런 장비가 생길 때 붙인다.
    /// 장비를 만들고 꺼내는 방법은 무기 손(WeaponLoadout)과 같다(Weapon.Spawn).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SupportHand : MonoBehaviour, IWeaponHolder
    {
        [Tooltip("장비를 붙일 보조 손 위치. 상체 아래에 두어 조준을 따라 돌게 한다. 손잡이는 이 위치와 무기 손 위치를 맞바꿔 정한다.")]
        [SerializeField] Transform hand;
        [SerializeField] BodyAim aim;
        [Tooltip("마법이 쓸 MP. 비워 두면 같은 물체에서 찾는다.")]
        [SerializeField] Mana mana;

        [Tooltip("보조 칸이 비었을 때 드는 빈손 장비 (우클릭 주먹질, F 기본 공격 마법).")]
        [SerializeField] WeaponData bareHand;

        bool primaryHeld;
        bool specialHeld;

        /// <summary>지금 보조 손에 든 장비. 빈손이면 빈손 장비.</summary>
        public Weapon Current { get; private set; }

        public Vector2 AimDirection => aim.AimDirection;

        /// <summary>보조 손 장비는 탄을 쓰지 않는다.</summary>
        public Inventory AmmoSource => null;

        public Mana Mana => mana;

        /// <summary>보조 손에 든 장비가 바뀌었을 때.</summary>
        public event Action<Weapon> CurrentChanged;

        void Awake()
        {
            if (aim == null) aim = GetComponent<BodyAim>();
            if (mana == null) mana = GetComponent<Mana>();
            if (bareHand != null) Hold(Weapon.Spawn(bareHand, hand, this));
        }

        void OnEnable()
        {
            // 죽었다 살아나는 등 꺼졌다 켜졌을 때 다시 꺼낸다.
            if (Current != null && !Current.gameObject.activeSelf) Current.Draw(false, false);
        }

        void OnDisable()
        {
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

        void Hold(Weapon weapon)
        {
            if (Current != null) Current.Holster();
            Current = weapon;
            if (Current != null) Current.Draw(primaryHeld, specialHeld);
            CurrentChanged?.Invoke(Current);
        }
    }
}
