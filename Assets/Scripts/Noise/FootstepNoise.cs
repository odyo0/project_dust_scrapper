using DustScrapper.Actors;
using UnityEngine;

namespace DustScrapper.Noise
{
    /// <summary>
    /// 걸을 때 일정한 간격으로 발소리 소음을 낸다. 빠를수록 멀리 들리고, 일정 속도 이하(살금살금)면 소리가 나지 않는다.
    /// 입력이 아니라 실제 움직이는 속도를 보므로, 무거운 장비로 느려지거나 밀려날 때도 그대로 맞는다.
    /// </summary>
    [RequireComponent(typeof(ActorMotor))]
    [DisallowMultipleComponent]
    public sealed class FootstepNoise : MonoBehaviour
    {
        [Tooltip("이 속도(칸/초) 이하로 움직이면 발소리가 나지 않는다. 살금살금 걷는 속도보다 조금 높게 둔다.")]
        [SerializeField, Min(0f)] float silentSpeed = 2.2f;

        [Tooltip("최고 속도로 걸을 때 발소리가 들리는 거리 (칸)")]
        [SerializeField, Min(0f)] float radiusAtFullSpeed = 4f;

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

            float maxSpeed = motor.Profile != null ? motor.Profile.MaxSpeed : speed;
            float t = Mathf.InverseLerp(silentSpeed, maxSpeed, speed);
            float radius = Mathf.Lerp(radiusAtSilentSpeed, radiusAtFullSpeed, t);
            NoiseBus.Emit(new NoiseEvent(transform.position, radius, gameObject));
        }
    }
}
