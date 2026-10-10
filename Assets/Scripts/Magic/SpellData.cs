using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Magic
{
    /// <summary>
    /// 탄을 쏘는 마법 하나의 수치. MP, 시전 시간, 다음 시전까지 기다리는 시간, 날아가는 탄.
    /// 물건이 아니라 스킬의 수치라서 ItemData를 이어받지 않는다. 같은 마법을 여러 장비(빈손, 글러브, 스태프)가 나눠 쓸 수 있다.
    /// 마법을 늘릴 때는 코드가 아니라 이 데이터 파일을 새로 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Magic/Spell", fileName = "Spell")]
    public sealed class SpellData : ScriptableObject
    {
        [Header("MP·시간")]
        [Tooltip("한 번 쓸 때 드는 MP")]
        [SerializeField, Min(0f)] float manaCost = 15f;
        [Tooltip("누른 뒤 탄이 나갈 때까지 (초). 그동안 반대 손은 공격하지 못한다.")]
        [SerializeField, Min(0f)] float castTime = 0.12f;
        [Tooltip("탄이 나간 뒤 다음 시전까지 기다리는 시간 (초). 누르고 있으면 이 간격으로 계속 나간다.")]
        [SerializeField, Min(0f)] float cooldown = 0.35f;

        [Header("탄")]
        [SerializeField] Projectile projectile;
        [Tooltip("탄 속도 (칸/초)")]
        [SerializeField, Min(0.1f)] float projectileSpeed = 14f;
        [Tooltip("탄이 날아가는 최대 거리 (칸)")]
        [SerializeField, Min(0.1f)] float range = 12f;
        [Tooltip("한 번 맞힐 때 피해")]
        [SerializeField, Min(0f)] float damage = 15f;
        [Tooltip("맞은 대상을 밀어내는 힘")]
        [SerializeField, Min(0f)] float knockback = 1.5f;

        [Header("소리")]
        [Tooltip("탄이 나갈 때 나는 소리가 적에게 들리는 거리 (칸). 0이면 소리가 나지 않는다.")]
        [SerializeField, Min(0f)] float noiseRadius = 4f;

        public float ManaCost => manaCost;
        public float CastTime => castTime;
        public float Cooldown => cooldown;
        public Projectile Projectile => projectile;
        public float ProjectileSpeed => projectileSpeed;
        public float Range => range;
        public float Damage => damage;
        public float Knockback => knockback;
        public float NoiseRadius => noiseRadius;
    }
}
