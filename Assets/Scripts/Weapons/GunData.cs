using UnityEngine;

namespace DustScrapper.Weapons
{
    public enum FireMode
    {
        /// <summary>한 번 누르면 한 발.</summary>
        SemiAuto,
        /// <summary>누르고 있으면 계속 나간다.</summary>
        FullAuto,
    }

    /// <summary>총의 수치. 연사 방식, 탄창, 탄 퍼짐, 반동 등. 단위: 거리는 칸, 각도는 도.</summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Weapons/Gun", fileName = "Gun")]
    public sealed class GunData : WeaponData
    {
        [Header("발사")]
        [SerializeField] FireMode fireMode = FireMode.FullAuto;

        [Tooltip("1초에 쏠 수 있는 최대 발 수")]
        [SerializeField, Min(0.1f)] float shotsPerSecond = 10f;

        [Tooltip("한 번 쏠 때 나가는 탄 수. 샷건이면 여러 발.")]
        [SerializeField, Min(1)] int pelletsPerShot = 1;

        [Header("탄")]
        [SerializeField] Projectile projectile;
        [Tooltip("탄 속도 (칸/초)")]
        [SerializeField, Min(0.1f)] float projectileSpeed = 25f;
        [Tooltip("탄이 날아가는 최대 거리 (칸)")]
        [SerializeField, Min(0.1f)] float range = 14f;

        [Header("탄창")]
        [SerializeField, Min(1)] int magazineSize = 30;
        [Tooltip("재장전 시간 (초)")]
        [SerializeField, Min(0f)] float reloadTime = 1.6f;

        [Header("탄 퍼짐")]
        [Tooltip("가만히 한 발 쏠 때 조준선에서 벗어날 수 있는 최대 각도 (도)")]
        [SerializeField, Min(0f)] float baseSpread = 1.5f;
        [Tooltip("한 발 쏠 때마다 퍼짐이 늘어나는 양 (도)")]
        [SerializeField, Min(0f)] float spreadPerShot = 1f;
        [Tooltip("퍼짐의 최대 각도 (도)")]
        [SerializeField, Min(0f)] float maxSpread = 8f;
        [Tooltip("쏘지 않을 때 퍼짐이 줄어드는 빠르기 (도/초)")]
        [SerializeField, Min(0f)] float spreadRecovery = 18f;

        [Header("반동")]
        [Tooltip("한 발 쏠 때 화면이 쏜 방향 반대로 튀는 거리 (칸)")]
        [SerializeField, Min(0f)] float recoilKick = 0.08f;

        public FireMode FireMode => fireMode;
        public float ShotsPerSecond => shotsPerSecond;
        public int PelletsPerShot => pelletsPerShot;
        public Projectile Projectile => projectile;
        public float ProjectileSpeed => projectileSpeed;
        public float Range => range;
        public int MagazineSize => magazineSize;
        public float ReloadTime => reloadTime;
        public float BaseSpread => baseSpread;
        public float SpreadPerShot => spreadPerShot;
        public float MaxSpread => Mathf.Max(maxSpread, baseSpread);
        public float SpreadRecovery => spreadRecovery;
        public float RecoilKick => recoilKick;
    }
}
