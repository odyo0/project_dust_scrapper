using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>무기를 쥐는 방식. 나중에 상체·팔 그림 레이어를 고를 때 쓴다(그림 수 = 쥐는 방식 × 동작).</summary>
    public enum GripType
    {
        OneHanded,
        TwoHanded,
    }

    /// <summary>
    /// 모든 무기가 공통으로 갖는 수치. 총은 GunData, 근접 무기는 MeleeData가 이것을 이어받아 자기 수치를 더한다.
    /// 무기를 늘릴 때는 코드가 아니라 이 데이터 파일을 새로 만든다.
    /// </summary>
    public abstract class WeaponData : ScriptableObject
    {
        [SerializeField] string displayName;

        [Tooltip("손에 들 때 만들 무기 프리팹. 프리팹의 무기 종류(Gun/MeleeWeapon)가 이 데이터 종류와 맞아야 한다.")]
        [SerializeField] Weapon prefab;

        [SerializeField] GripType grip = GripType.OneHanded;

        [Tooltip("한 번 맞힐 때 피해")]
        [SerializeField, Min(0f)] float damage = 10f;

        [Tooltip("맞은 대상을 밀어내는 힘")]
        [SerializeField, Min(0f)] float knockback = 1f;

        [Tooltip("꺼낸 뒤 쓸 수 있게 되기까지 걸리는 시간 (초)")]
        [SerializeField, Min(0f)] float drawTime = 0.25f;

        [Tooltip("쓸 때(쏘거나 휘두를 때) 나는 소리가 적에게 들리는 거리 (칸). 0이면 소리가 나지 않는다.")]
        [SerializeField, Min(0f)] float noiseRadius;

        public string DisplayName => displayName;
        public Weapon Prefab => prefab;
        public GripType Grip => grip;
        public float Damage => damage;
        public float Knockback => knockback;
        public float DrawTime => drawTime;
        public float NoiseRadius => noiseRadius;
    }
}
