using DustScrapper.Actors;
using UnityEngine;

namespace DustScrapper.Audio
{
    /// <summary>
    /// 걸을 때 발소리를 낸다. 빠를수록 발걸음 간격이 짧아지고, 일정 속도보다 느리면 소리가 없다.
    /// 적에게 붙이면 보이지 않는 적이 다가오는 것을 귀로 알 수 있다.
    /// 적이 듣는 소음(FootstepNoise)과는 따로다: 이쪽은 플레이어 귀에 들리는 소리, 저쪽은 게임 규칙상의 소음이다.
    /// </summary>
    [RequireComponent(typeof(ActorMotor))]
    [DisallowMultipleComponent]
    public sealed class FootstepAudio : MonoBehaviour
    {
        [SerializeField] SoundCue step;

        [Tooltip("이 속도(칸/초) 이하면 발소리가 나지 않는다")]
        [SerializeField, Min(0f)] float silentSpeed = 1f;

        [Tooltip("최고 속도로 걸을 때 발걸음 간격 (초). 느리면 최대 두 배까지 늘어난다.")]
        [SerializeField, Min(0.05f)] float stepInterval = 0.32f;

        ActorMotor motor;
        float stepTimer;

        void Awake() => motor = GetComponent<ActorMotor>();

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

            float maxSpeed = motor.Profile != null ? motor.Profile.MaxSpeed : speed;
            stepTimer = stepInterval / Mathf.Clamp(speed / Mathf.Max(maxSpeed, 0.01f), 0.5f, 1f);
            if (step != null) step.PlayAt(transform.position);
        }
    }
}
