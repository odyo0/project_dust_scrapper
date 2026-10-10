using System;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 던지기 스킬(칼의 Space). 누르면 짧게 준비한 뒤 이 장비를 손에서 놓고(손은 빈손이 된다) 조준 방향으로 던진다.
    /// 던진 장비는 탄(Projectile)으로 날아가 맞히고, 멈춘 자리에 바닥 물건으로 떨어진다(ThrownItem). 다시 주우면 된다.
    /// 이 장비 하나만 쓰는 값이라 데이터 파일 대신 프리팹의 이 부품에 둔다. 여러 장비가 나눠 쓰게 되면 데이터로 뺀다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThrowSkill : Skill
    {
        [Tooltip("던질 때 날아갈 탄. ThrownItem이 붙어 있어야 멈춘 자리에 물건이 떨어진다.")]
        [SerializeField] Projectile thrownPrefab;
        [Tooltip("누른 뒤 손을 떠날 때까지 (초)")]
        [SerializeField, Min(0f)] float windupTime = 0.1f;
        [Tooltip("날아가는 속도 (칸/초)")]
        [SerializeField, Min(0.1f)] float speed = 18f;
        [Tooltip("날아가는 최대 거리 (칸)")]
        [SerializeField, Min(0.1f)] float range = 8f;
        [SerializeField, Min(0f)] float damage = 40f;
        [SerializeField, Min(0f)] float knockback = 2f;
        [Tooltip("던질 때 나는 소리가 적에게 들리는 거리 (칸)")]
        [SerializeField, Min(0f)] float noiseRadius = 2f;

        float windupTimer = -1f;

        public bool IsWindingUp => windupTimer >= 0f;
        public override bool IsBusy => IsWindingUp;

        /// <summary>손을 떠났을 때. 값은 던진 방향.</summary>
        public event Action<Vector2> Thrown;

        protected override void OnCancel() => windupTimer = -1f;

        protected override void Tick(bool pressed, bool held, float deltaTime)
        {
            if (IsWindingUp)
            {
                windupTimer -= deltaTime;
                if (windupTimer <= 0f) Throw();
                return;
            }

            if (!pressed) return;
            windupTimer = windupTime;
            if (windupTimer <= 0f) Throw();
        }

        void Throw()
        {
            windupTimer = -1f;
            if (thrownPrefab == null)
            {
                Debug.LogError($"{name}: 던질 탄 프리팹이 없다.", this);
                return;
            }

            IWeaponHolder holder = Holder;
            Vector2 aim = holder.AimDirection;
            Vector2 start = holder.transform.position;
            float handDistance = Vector2.Distance(start, transform.position);
            if (!holder.TryRelease(Weapon, out ItemStack stack)) return;

            Projectile projectile = Instantiate(thrownPrefab, start, Quaternion.identity);
            if (projectile.TryGetComponent(out ThrownItem item)) item.SetStack(stack);
            projectile.Launch(start, aim, handDistance, speed, range, damage, knockback, holder.gameObject);

            EmitNoise(noiseRadius);
            Thrown?.Invoke(aim);
        }
    }
}
