using DustScrapper.Cameras;
using DustScrapper.Combat;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Player
{
    /// <summary>
    /// 플레이어의 전투 사건을 화면 반응으로 잇는다.
    /// - 총을 쏘면 화면이 쏜 방향 반대로 튄다(무기마다 세기가 다르다: GunData.RecoilKick).
    /// - 근접 무기로 맞히면 화면이 짧게 떨린다.
    /// - 맞으면 화면이 크게 떨린다.
    /// 적이 쏘거나 맞을 때는 화면이 움직이면 안 되므로, 이 연결은 플레이어에게만 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombatFeedback : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] WeaponLoadout loadout;
        [SerializeField] CameraShake cameraShake;

        [Tooltip("맞았을 때 더할 화면 충격 (0~1)")]
        [SerializeField, Range(0f, 1f)] float damagedTrauma = 0.45f;
        [Tooltip("근접 무기로 맞혔을 때 더할 화면 충격 (0~1)")]
        [SerializeField, Range(0f, 1f)] float meleeHitTrauma = 0.25f;

        Gun currentGun;
        MeleeWeapon currentMelee;

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (loadout == null) loadout = GetComponent<WeaponLoadout>();
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            loadout.CurrentChanged += OnWeaponChanged;
            OnWeaponChanged(loadout.Current);
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            loadout.CurrentChanged -= OnWeaponChanged;
            OnWeaponChanged(null);
        }

        void OnWeaponChanged(Weapon weapon)
        {
            if (currentGun != null) currentGun.Fired -= OnGunFired;
            if (currentMelee != null) currentMelee.Hit -= OnMeleeHit;

            currentGun = weapon as Gun;
            currentMelee = weapon as MeleeWeapon;

            if (currentGun != null) currentGun.Fired += OnGunFired;
            if (currentMelee != null) currentMelee.Hit += OnMeleeHit;
        }

        void OnGunFired(Vector2 direction)
        {
            if (cameraShake != null) cameraShake.Kick(-direction, currentGun.GunData.RecoilKick);
        }

        void OnMeleeHit(DamageInfo info)
        {
            if (cameraShake != null) cameraShake.AddTrauma(meleeHitTrauma);
        }

        void OnDamaged(DamageInfo info)
        {
            if (cameraShake != null) cameraShake.AddTrauma(damagedTrauma);
        }
    }
}
