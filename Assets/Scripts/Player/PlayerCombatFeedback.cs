using System;
using DustScrapper.Cameras;
using DustScrapper.Combat;
using DustScrapper.Magic;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Player
{
    /// <summary>
    /// 플레이어의 전투 사건을 화면 반응으로 잇는다. 무기 손과 보조 손을 똑같이 본다.
    /// - 총을 쏘면 화면이 쏜 방향 반대로 튄다(무기마다 세기가 다르다: GunData.RecoilKick).
    /// - 마법 탄이 나가면 화면이 쏜 방향 반대로 살짝 튄다.
    /// - 근접 공격으로 맞히면 화면이 짧게 떨린다.
    /// - 맞으면 화면이 크게 떨린다.
    /// 적이 쏘거나 맞을 때는 화면이 움직이면 안 되므로, 이 연결은 플레이어에게만 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombatFeedback : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] WeaponLoadout loadout;
        [SerializeField] SupportHand supportHand;
        [SerializeField] CameraShake cameraShake;

        [Tooltip("맞았을 때 더할 화면 충격 (0~1)")]
        [SerializeField, Range(0f, 1f)] float damagedTrauma = 0.45f;
        [Tooltip("근접 공격으로 맞혔을 때 더할 화면 충격 (0~1)")]
        [SerializeField, Range(0f, 1f)] float meleeHitTrauma = 0.25f;
        [Tooltip("마법 탄이 나갈 때 화면이 튀는 거리 (칸)")]
        [SerializeField, Min(0f)] float spellKick = 0.05f;

        HandWatch weaponHandWatch;
        HandWatch supportHandWatch;

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (loadout == null) loadout = GetComponent<WeaponLoadout>();
            if (supportHand == null) supportHand = GetComponent<SupportHand>();
            weaponHandWatch = new HandWatch(this);
            supportHandWatch = new HandWatch(this);
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            if (loadout != null)
            {
                loadout.CurrentChanged += weaponHandWatch.Watch;
                weaponHandWatch.Watch(loadout.Current);
            }
            if (supportHand != null)
            {
                supportHand.CurrentChanged += supportHandWatch.Watch;
                supportHandWatch.Watch(supportHand.Current);
            }
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            if (loadout != null) loadout.CurrentChanged -= weaponHandWatch.Watch;
            if (supportHand != null) supportHand.CurrentChanged -= supportHandWatch.Watch;
            weaponHandWatch.Watch(null);
            supportHandWatch.Watch(null);
        }

        void OnGunFired(GunFire gun, Vector2 direction)
        {
            if (cameraShake != null) cameraShake.Kick(-direction, gun.GunData.RecoilKick);
        }

        void OnSpellReleased(Vector2 direction)
        {
            if (cameraShake != null) cameraShake.Kick(-direction, spellKick);
        }

        void OnMeleeHit(DamageInfo info)
        {
            if (cameraShake != null) cameraShake.AddTrauma(meleeHitTrauma);
        }

        void OnDamaged(DamageInfo info)
        {
            if (cameraShake != null) cameraShake.AddTrauma(damagedTrauma);
        }

        /// <summary>
        /// 한 손에 든 장비의 스킬 사건을 듣는다. 장비가 바뀌면 옛 장비에서 떼고 새 장비에 붙는다.
        /// 손마다 하나씩 두어서 두 손이 서로의 구독을 건드리지 않게 한다.
        /// </summary>
        sealed class HandWatch
        {
            readonly PlayerCombatFeedback owner;
            readonly Action<Vector2> onGunFired;
            GunFire gun;
            MeleeAttack melee;
            SpellCast primarySpell;
            SpellCast specialSpell;

            public HandWatch(PlayerCombatFeedback owner)
            {
                this.owner = owner;
                onGunFired = direction => owner.OnGunFired(gun, direction);
            }

            public void Watch(Weapon weapon)
            {
                if (gun != null) gun.Fired -= onGunFired;
                if (melee != null) melee.Hit -= owner.OnMeleeHit;
                if (primarySpell != null) primarySpell.Released -= owner.OnSpellReleased;
                if (specialSpell != null) specialSpell.Released -= owner.OnSpellReleased;

                gun = weapon != null ? weapon.FindSkill<GunFire>() : null;
                melee = weapon != null ? weapon.FindSkill<MeleeAttack>() : null;
                // 마법은 기본·특수 둘 다 마법일 수 있어서(마법서 등) 칸마다 따로 본다.
                primarySpell = weapon != null ? weapon.Primary as SpellCast : null;
                specialSpell = weapon != null ? weapon.Special as SpellCast : null;

                if (gun != null) gun.Fired += onGunFired;
                if (melee != null) melee.Hit += owner.OnMeleeHit;
                if (primarySpell != null) primarySpell.Released += owner.OnSpellReleased;
                if (specialSpell != null) specialSpell.Released += owner.OnSpellReleased;
            }
        }
    }
}
