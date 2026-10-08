using System;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 총. 방아쇠 상태를 받아 연사 방식에 맞게 탄을 쏘고, 탄창·재장전·탄 퍼짐을 관리한다.
    /// 재장전할 때는 주인(WeaponLoadout)의 탄 공급원(가방)에서 이 총에 맞는 탄을 꺼낸다. 꺼낼 탄이 없으면 장전하지 못한다.
    /// 화면 반동이나 총구 불꽃 같은 연출은 하지 않고 Fired 이벤트만 알린다(연출은 GunFireVisual 등이 맡는다).
    /// </summary>
    public sealed class Gun : Weapon
    {
        /// <summary>단발 총에서 쿨다운 중에 누른 클릭을 이 시간(초) 동안 기억했다가 쏜다. 빠른 연타가 씹히지 않게.</summary>
        const float PressBufferTime = 0.12f;

        [Tooltip("탄이 나가는 위치")]
        [SerializeField] Transform muzzle;

        GunData gun;
        float cooldown;
        float reloadTimer;
        float pressBuffer;

        public GunData GunData => gun;
        public int AmmoInMagazine { get; private set; }
        public bool IsReloading => reloadTimer > 0f;

        /// <summary>탄창 밖에 남은, 이 총에 넣을 수 있는 탄 수(가방 안).</summary>
        public int ReserveAmmo => Owner.AmmoSource != null ? Owner.AmmoSource.CountOf(gun.Ammo) : 0;

        public override bool IsBusy => base.IsBusy || IsReloading;
        public float ReloadProgress => IsReloading && gun.ReloadTime > 0f ? 1f - reloadTimer / gun.ReloadTime : 0f;

        /// <summary>지금 탄이 조준선에서 벗어날 수 있는 최대 각도(도). 조준점 표시가 읽는다.</summary>
        public float CurrentSpread { get; private set; }

        /// <summary>한 번 쏠 때마다. 값은 조준 방향(퍼짐 적용 전).</summary>
        public event Action<Vector2> Fired;

        /// <summary>재장전을 시작했을 때.</summary>
        public event Action ReloadStarted;

        protected override void OnInitialize()
        {
            gun = Data as GunData;
            if (gun == null)
            {
                Debug.LogError($"{name}: Gun 프리팹에는 GunData를 써야 한다.", this);
                enabled = false;
                return;
            }
            if (gun.Ammo == null) Debug.LogWarning($"{gun.name}: 탄약(Ammo)이 비어 있어 재장전할 수 없다.", gun);
            AmmoInMagazine = gun.MagazineSize;
            CurrentSpread = gun.BaseSpread;
        }

        /// <summary>탄창의 탄 수를 정한다. 주운 총이 버려질 때의 탄창을 그대로 이어받을 때 쓴다.</summary>
        public void SetMagazine(int amount)
        {
            AmmoInMagazine = Mathf.Clamp(amount, 0, gun.MagazineSize);
        }

        protected override void OnHolstered()
        {
            reloadTimer = 0f;
            pressBuffer = 0f;
        }

        public override void Reload()
        {
            if (!IsDrawn || IsReloading || AmmoInMagazine >= gun.MagazineSize) return;
            if (ReserveAmmo <= 0) return;

            reloadTimer = gun.ReloadTime;
            ReloadStarted?.Invoke();
            if (reloadTimer <= 0f) FinishReload();
        }

        protected override void Tick(bool triggerPressed, bool triggerHeld, float deltaTime)
        {
            cooldown -= deltaTime;
            pressBuffer = triggerPressed ? PressBufferTime : pressBuffer - deltaTime;
            CurrentSpread = Mathf.MoveTowards(CurrentSpread, gun.BaseSpread, gun.SpreadRecovery * deltaTime);

            if (IsReloading)
            {
                // 장전하는 동안 쿨다운이 음수로 계속 쌓이면 장전이 끝난 뒤 밀린 만큼 몰아서 쏜다. 0에서 멈춘다.
                if (cooldown < 0f) cooldown = 0f;
                reloadTimer -= deltaTime;
                if (reloadTimer <= 0f) FinishReload();
                return;
            }

            bool wantsToFire = gun.FireMode == FireMode.FullAuto ? triggerHeld : pressBuffer > 0f;
            if (!wantsToFire || cooldown > 0f)
            {
                // 쏘지 않는 동안 쿨다운이 음수로 쌓이면 다음에 한꺼번에 여러 발이 나가므로 0에서 멈춘다.
                if (cooldown < 0f) cooldown = 0f;
                return;
            }

            if (AmmoInMagazine <= 0)
            {
                Reload();
                return;
            }

            // 프레임이 크게 밀렸을 때도 몰아서 쏘지 않게, 넘긴 시간은 한 프레임 분까지만 인정한다.
            cooldown = Mathf.Max(cooldown, -deltaTime);
            Fire();
        }

        void Fire()
        {
            AmmoInMagazine--;
            // 프레임 간격과 상관없이 정확한 연사 속도를 지키려고 쿨다운을 "설정"하지 않고 "더한다".
            cooldown += 1f / gun.ShotsPerSecond;
            pressBuffer = 0f;

            Vector2 aim = Owner.AimDirection;
            Vector2 start = Owner.transform.position;
            Vector2 muzzlePoint = muzzle != null ? (Vector2)muzzle.position : start;
            float muzzleDistance = Vector2.Distance(start, muzzlePoint);

            for (int i = 0; i < gun.PelletsPerShot; i++)
            {
                float angle = UnityEngine.Random.Range(-CurrentSpread, CurrentSpread);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * aim;
                // 탄은 캐릭터 중심에서 출발해 총구까지 한 번에 이동한다.
                // 총구가 벽 너머로 삐져나와 있어도 벽을 뚫지 않게 하려는 것이다.
                Projectile projectile = Instantiate(gun.Projectile, start, Quaternion.identity);
                projectile.Launch(start, direction, muzzleDistance, gun.ProjectileSpeed, gun.Range,
                    gun.Damage, gun.Knockback, Owner.gameObject);
            }

            CurrentSpread = Mathf.Min(gun.MaxSpread, CurrentSpread + gun.SpreadPerShot);
            EmitUseNoise();
            Fired?.Invoke(aim);
        }

        void FinishReload()
        {
            reloadTimer = 0f;
            // 탄은 장전이 끝날 때 꺼낸다. 중간에 무기를 바꿔 장전이 취소되면 탄이 줄지 않는다.
            int wanted = gun.MagazineSize - AmmoInMagazine;
            AmmoInMagazine += Owner.AmmoSource != null ? Owner.AmmoSource.Remove(gun.Ammo, wanted) : 0;
        }
    }
}
