using DustScrapper.Actors;
using UnityEngine;
using UnityEngine.Serialization;

namespace DustScrapper.Noise
{
    /// <summary>
    /// 걸을 때 일정한 간격으로 발소리 소음을 낸다. 빠를수록 멀리 들리고, 일정 속도 이하(살금살금)면 소리가 나지 않는다.
    /// 걷기 최고 속도를 넘어 달리면 달리기 속도까지 소리가 더 커진다.
    /// 입력이 아니라 실제 움직이는 속도를 보므로, 무거운 장비로 느려지거나 밀려날 때도 그대로 맞는다.
    /// </summary>
    [RequireComponent(typeof(ActorMotor))]
    [DisallowMultipleComponent]
    public sealed class FootstepNoise : MonoBehaviour
    {
        [Tooltip("이 속도(칸/초) 이하로 움직이면 발소리가 나지 않는다. 살금살금 걷는 속도보다 조금 높게 둔다.")]
        [SerializeField, Min(0f)] float silentSpeed = 2.2f;

        [Tooltip("최고 속도로 걸을 때 발소리가 들리는 거리 (칸)")]
        [FormerlySerializedAs("radiusAtFullSpeed")]
        [SerializeField, Min(0f)] float radiusAtWalkSpeed = 4f;

        [Tooltip("최고 속도로 달릴 때 발소리가 들리는 거리 (칸)")]
        [SerializeField, Min(0f)] float radiusAtSprintSpeed = 7f;

        [Tooltip("소리가 나기 시작하는 속도에서 들리는 거리 (칸)")]
        [SerializeField, Min(0f)] float radiusAtSilentSpeed = 2f;

        [Tooltip("발소리 간격 (초)")]
        [SerializeField, Min(0.05f)] float stepInterval = 0.35f;

        ActorMotor motor;
        float stepTimer;

        void Awake()
        {
            motor = GetComponent<ActorMotor>();
        }

        void Update()
        {
            float speed = motor.Velocity.magnitude;
            if (speed <= silentSpeed)
            {
                stepTimer = 0f;
                return;
            }

            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f) return;
            stepTimer = stepInterval;

            NoiseBus.Emit(new NoiseEvent(transform.position, RadiusAt(speed), gameObject));
        }

        /// <summary>
        /// 속도에 따른 발소리 거리. 소리 나기 시작하는 속도 → 걷기 최고 속도 → 달리기 최고 속도 두 구간으로 나눠 키운다.
        /// 한 구간으로 이으면 걷기 최고 속도의 소리가 달리기 속도에 따라 흔들리므로, 걷기 기준점을 따로 둔다.
        /// </summary>
        float RadiusAt(float speed)
        {
            MovementProfile profile = motor.Profile;
            float walkSpeed = profile != null ? profile.MaxSpeed : speed;
            if (speed <= walkSpeed)
            {
                float t = Mathf.InverseLerp(silentSpeed, walkSpeed, speed);
                return Mathf.Lerp(radiusAtSilentSpeed, radiusAtWalkSpeed, t);
            }

            float sprintSpeed = profile != null ? profile.SprintSpeed : speed;
            float u = Mathf.InverseLerp(walkSpeed, sprintSpeed, speed);
            return Mathf.Lerp(radiusAtWalkSpeed, radiusAtSprintSpeed, u);
        }
    }
}
