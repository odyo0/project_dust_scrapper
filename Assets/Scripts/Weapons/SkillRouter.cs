using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 키마다 어느 손의 어느 스킬이 나갈지 정하는 한 곳. 두뇌는 누른 키만 넘기고, 손 사이의 규칙은 모두 여기서 정한다.
    /// - 좌클릭·Q·R → 무기 손(WeaponLoadout), 우클릭·E → 보조 손(SupportHand).
    /// - 무기 손에 두손 무기를 들면, "두손 무기와 같이 못 씀" 장비(스태프)를 낀 보조 손은 빈손을 든다.
    /// - 각 손 장비에 반대 손에 지금 든 장비의 속성을 알려 준다(양방향). 이펙트를 어떻게 바꿀지는 장비의 마법 스킬이 정한다.
    /// - 한 손이 마법을 시전하는 동안 반대 손은 공격하지 못한다(재장전처럼 하던 일은 계속한다). (기획 잠정 규칙)
    /// 손 사이 조합 규칙이 늘면 여기에 더한다.
    /// 적은 손이 하나뿐이라 이것 없이 WeaponLoadout에 바로 요청한다.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class SkillRouter : MonoBehaviour
    {
        [SerializeField] WeaponLoadout weaponHand;
        [SerializeField] SupportHand supportHand;

        void Awake()
        {
            if (weaponHand == null) weaponHand = GetComponent<WeaponLoadout>();
            if (supportHand == null) supportHand = GetComponent<SupportHand>();
        }

        void OnDisable() => ReleaseAll();

        /// <summary>무기 손의 기본 스킬(좌클릭)·특수 스킬(Q) 키.</summary>
        public void SetWeaponHand(bool primaryHeld, bool specialHeld)
        {
            if (weaponHand == null) return;
            weaponHand.SetPrimary(primaryHeld);
            weaponHand.SetSpecial(specialHeld);
        }

        /// <summary>보조 손의 기본 스킬(우클릭)·특수 스킬(E) 키.</summary>
        public void SetSupportHand(bool primaryHeld, bool specialHeld)
        {
            if (supportHand == null) return;
            supportHand.SetPrimary(primaryHeld);
            supportHand.SetSpecial(specialHeld);
        }

        /// <summary>R. 지금은 무기 손의 재장전.</summary>
        public void Reload()
        {
            if (weaponHand != null) weaponHand.Reload();
        }

        /// <summary>모든 키를 뗀 것으로 한다(꺼질 때, 죽을 때).</summary>
        public void ReleaseAll()
        {
            SetWeaponHand(false, false);
            SetSupportHand(false, false);
        }

        void Update()
        {
            // 두뇌(-50) 다음, 장비(0) 전에 돌아서 이번 프레임 장비 동작에 규칙이 바로 걸린다.
            Weapon weapon = weaponHand != null ? weaponHand.Current : null;

            if (supportHand != null)
            {
                WeaponData supportItem = supportHand.EquippedData;
                bool twoHanded = weapon != null && weapon.Data.Grip == GripType.TwoHanded;
                supportHand.SetForcedBare(twoHanded && supportItem != null && supportItem.ConflictsWithTwoHanded);
            }
            Weapon support = supportHand != null ? supportHand.Current : null;

            if (weapon != null) weapon.OppositeElement = support != null ? support.Data.Element : null;
            if (support != null) support.OppositeElement = weapon != null ? weapon.Data.Element : null;

            if (weapon != null) weapon.AttackBlocked = support != null && support.IsCasting;
            if (support != null) support.AttackBlocked = weapon != null && weapon.IsCasting;
        }
    }
}
