using UnityEngine;

namespace DustScrapper.Actors
{
    /// <summary>
    /// 다리 그림을 실제로 움직이는 방향으로 돌린다. 상체와 달리 회전 속도에 한계가 있어
    /// 급하게 방향을 틀면 다리가 따라 도는 게 보인다. 멈춰 있으면 마지막 방향을 유지한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LegsFacing : MonoBehaviour
    {
        [Tooltip("회전시킬 다리 그림")]
        [SerializeField] Transform legs;
        [SerializeField] ActorMotor motor;

        [Tooltip("다리가 도는 최대 속도 (도/초)")]
        [SerializeField, Min(0f)] float turnSpeed = 720f;

        [Tooltip("이 속도(칸/초)보다 느리면 다리를 돌리지 않는다")]
        [SerializeField, Min(0f)] float minSpeedToTurn = 0.3f;

        void Awake()
        {
            if (motor == null) motor = GetComponent<ActorMotor>();
        }

        void Update()
        {
            Vector2 velocity = motor.Velocity;
            if (velocity.sqrMagnitude < minSpeedToTurn * minSpeedToTurn) return;

            float targetAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            float currentAngle = legs.eulerAngles.z;
            float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
            legs.rotation = Quaternion.Euler(0f, 0f, nextAngle);
        }
    }
}
