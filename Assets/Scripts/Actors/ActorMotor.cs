using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Actors
{
    /// <summary>
    /// 이동 의도를 받아 Rigidbody2D 속도를 가속·감속으로 맞춘다. 벽 충돌은 물리가 처리한다.
    /// 누가 의도를 넣는지(플레이어 입력, 적 AI)는 모른다. 달리기도 "달리고 싶다"는 의도 하나로 받아서, 적 AI도 같은 방법으로 달린다.
    /// 다른 부품(조준 스킬, 디버프 등)은 SetSpeedModifier로 "느리게 하기"를 걸고 풀 수 있다. 여러 곳이 동시에 걸면 곱한다.
    /// 구르기처럼 의도와 상관없이 정해진 속도로 밀어야 할 때는 StartForcedMove를 쓴다. 끝나면 그 속도에서 평소처럼 감속한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class ActorMotor : MonoBehaviour
    {
        const float IdleThreshold = 0.01f;

        [SerializeField] MovementProfile profile;

        readonly Dictionary<object, SpeedModifier> speedModifiers = new();

        Rigidbody2D body;
        Vector2 moveIntent;
        Vector2 forcedVelocity;
        float forcedTimeLeft;

        /// <summary>가려는 방향과 세기. 길이는 0~1로 잘린다.</summary>
        public Vector2 MoveIntent
        {
            get => moveIntent;
            set => moveIntent = Vector2.ClampMagnitude(value, 1f);
        }

        /// <summary>달리고 싶은가. 켜져 있으면 최고 속도가 걷기 대신 달리기 속도가 된다.</summary>
        public bool Sprinting { get; set; }

        /// <summary>
        /// 지금 낼 수 있는 최고 속도 (칸/초). 달리는 중이면 달리기 속도(달리기를 막는 느리게 하기가 걸려 있으면 걷기 속도),
        /// 그 위에 걸린 느리게 하기 배율을 모두 곱한다.
        /// </summary>
        public float CurrentMaxSpeed
        {
            get
            {
                if (profile == null) return 0f;
                float multiplier = 1f;
                bool sprintBlocked = false;
                foreach (SpeedModifier modifier in speedModifiers.Values)
                {
                    multiplier *= modifier.Multiplier;
                    sprintBlocked |= modifier.BlocksSprint;
                }
                float speed = Sprinting && !sprintBlocked ? profile.SprintSpeed : profile.MaxSpeed;
                return speed * multiplier;
            }
        }

        /// <summary>
        /// source(건 쪽)가 거는 속도 배율. 같은 source가 다시 걸면 덮어쓴다. blocksSprint면 걸려 있는 동안 달리지 못한다.
        /// 건 쪽마다 따로 기억해서, 조준을 풀어도 디버프의 느리게 하기는 그대로 남는다.
        /// </summary>
        public void SetSpeedModifier(object source, float multiplier, bool blocksSprint = false)
        {
            speedModifiers[source] = new SpeedModifier(Mathf.Max(0f, multiplier), blocksSprint);
        }

        public void ClearSpeedModifier(object source) => speedModifiers.Remove(source);

        /// <summary>강제 이동 중인가. 이동 의도는 무시된다.</summary>
        public bool IsForcedMoving => forcedTimeLeft > 0f;

        /// <summary>
        /// duration초 동안 이동 의도를 무시하고 velocity로 민다(구르기, 돌진 …). 벽에 막히는 것은 물리가 그대로 처리한다.
        /// 이미 강제 이동 중이면 새것으로 덮어쓴다.
        /// </summary>
        public void StartForcedMove(Vector2 velocity, float duration)
        {
            forcedVelocity = velocity;
            forcedTimeLeft = Mathf.Max(0f, duration);
        }

        public void StopForcedMove() => forcedTimeLeft = 0f;

        public Vector2 Velocity => body.linearVelocity;
        public MovementProfile Profile => profile;

        /// <summary>장비가 바뀌어 이동 무게감이 달라질 때 부른다.</summary>
        public void SetProfile(MovementProfile newProfile)
        {
            profile = newProfile;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        void OnDisable() => forcedTimeLeft = 0f;

        void FixedUpdate()
        {
            if (forcedTimeLeft > 0f)
            {
                body.linearVelocity = forcedVelocity;
                forcedTimeLeft -= Time.fixedDeltaTime;
                return;
            }

            if (profile == null) return;

            Vector2 targetVelocity = moveIntent * CurrentMaxSpeed;
            bool hasIntent = moveIntent.sqrMagnitude > IdleThreshold * IdleThreshold;
            float rate = hasIntent ? profile.Acceleration : profile.Deceleration;

            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
        }

        readonly struct SpeedModifier
        {
            public readonly float Multiplier;
            public readonly bool BlocksSprint;

            public SpeedModifier(float multiplier, bool blocksSprint)
            {
                Multiplier = multiplier;
                BlocksSprint = blocksSprint;
            }
        }
    }
}
