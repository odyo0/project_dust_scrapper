using System;
using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.Actors
{
    /// <summary>
    /// 구르기: 정한 방향으로 짧게 빠르게 구르고, 구르는 동안만 무적이다(구르기가 끝나면 무적도 끝난다).
    /// 움직이는 일은 ActorMotor의 강제 이동에, 무적은 Health에 맡긴다. 누가 구르라고 시키는지(플레이어 Space, 나중에 적 AI)는 모른다.
    /// 소음은 따로 내지 않는다: 발소리가 실제 속도로 정해지고 달리기 속도에서 더 커지지 않으므로 구르는 소리는 저절로 달리기와 같다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DodgeRoll : MonoBehaviour
    {
        [SerializeField] ActorMotor motor;
        [SerializeField] Health health;

        [Tooltip("구르는 빠르기 (칸/초). 달리기보다 빠르게 둔다. AI 임시값")]
        [SerializeField, Min(0f)] float speed = 9f;
        [Tooltip("구르는 시간 (초). 이 동안 무적이다. AI 임시값")]
        [SerializeField, Min(0.05f)] float duration = 0.3f;
        [Tooltip("구르기가 끝난 뒤 다시 구를 수 있을 때까지 (초). AI 임시값")]
        [SerializeField, Min(0f)] float cooldown = 0.35f;

        float cooldownLeft;

        public bool IsRolling { get; private set; }

        /// <summary>구르기 시작. 굴러가는 방향(길이 1)을 넘긴다. 연출·소리가 듣는다.</summary>
        public event Action<Vector2> Started;

        /// <summary>구르기가 끝났을 때.</summary>
        public event Action Ended;

        void Awake()
        {
            if (motor == null) motor = GetComponent<ActorMotor>();
            if (health == null) health = GetComponent<Health>();
        }

        void OnDisable()
        {
            if (IsRolling) End();
            cooldownLeft = 0f;
        }

        void Update()
        {
            if (IsRolling)
            {
                if (!motor.IsForcedMoving) End();
                return;
            }
            if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;
        }

        /// <summary>이 방향으로 구른다. 이미 구르는 중이거나, 쉬는 시간이 남았거나, 방향이 없으면 false.</summary>
        public bool TryRoll(Vector2 direction)
        {
            if (IsRolling || cooldownLeft > 0f || direction.sqrMagnitude < 0.0001f) return false;
            if (health != null && health.IsDead) return false;

            Vector2 dir = direction.normalized;
            IsRolling = true;
            motor.StartForcedMove(dir * speed, duration);
            if (health != null) health.SetInvulnerable(this, true);
            Started?.Invoke(dir);
            return true;
        }

        void End()
        {
            IsRolling = false;
            cooldownLeft = cooldown;
            motor.StopForcedMove();
            if (health != null) health.SetInvulnerable(this, false);
            Ended?.Invoke();
        }
    }
}
