using UnityEngine;

namespace DustScrapper.Actors
{
    /// <summary>
    /// 이동 의도를 받아 Rigidbody2D 속도를 가속·감속으로 맞춘다. 벽 충돌은 물리가 처리한다.
    /// 누가 의도를 넣는지(플레이어 입력, 적 AI)는 모른다. 달리기도 "달리고 싶다"는 의도 하나로 받아서, 적 AI도 같은 방법으로 달린다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class ActorMotor : MonoBehaviour
    {
        const float IdleThreshold = 0.01f;

        [SerializeField] MovementProfile profile;

        Rigidbody2D body;
        Vector2 moveIntent;

        /// <summary>가려는 방향과 세기. 길이는 0~1로 잘린다.</summary>
        public Vector2 MoveIntent
        {
            get => moveIntent;
            set => moveIntent = Vector2.ClampMagnitude(value, 1f);
        }

        /// <summary>달리고 싶은가. 켜져 있으면 최고 속도가 걷기 대신 달리기 속도가 된다.</summary>
        public bool Sprinting { get; set; }

        /// <summary>지금 낼 수 있는 최고 속도 (칸/초). 달리는 중이면 달리기 속도.</summary>
        public float CurrentMaxSpeed => profile == null ? 0f : Sprinting ? profile.SprintSpeed : profile.MaxSpeed;

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

        void FixedUpdate()
        {
            if (profile == null) return;

            Vector2 targetVelocity = moveIntent * CurrentMaxSpeed;
            bool hasIntent = moveIntent.sqrMagnitude > IdleThreshold * IdleThreshold;
            float rate = hasIntent ? profile.Acceleration : profile.Deceleration;

            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
        }
    }
}
