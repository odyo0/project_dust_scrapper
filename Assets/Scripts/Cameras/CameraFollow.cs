using DustScrapper.Player;
using UnityEngine;

namespace DustScrapper.Cameras
{
    /// <summary>
    /// 대상(플레이어)을 부드럽게 따라간다. 마우스가 화면 중앙에서 벗어난 만큼 그쪽으로 살짝 밀리고,
    /// 멀리 보기 키를 누르는 동안에는 훨씬 멀리 밀린다.
    /// 마우스의 "화면" 위치를 기준으로 삼아서, 카메라가 움직여도 밀림 정도가 흔들리지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerInputReader input;

        [Header("평소")]
        [Tooltip("커서가 화면 가장자리에 있을 때 밀리는 거리 (칸)")]
        [SerializeField, Min(0f)] float aimPush = 1.5f;
        [SerializeField, Min(0.01f)] float smoothTime = 0.15f;

        [Header("멀리 보기")]
        [Tooltip("멀리 보기 중 커서가 화면 가장자리에 있을 때 밀리는 거리 (칸)")]
        [SerializeField, Min(0f)] float lookAheadDistance = 5f;
        [SerializeField, Min(0.01f)] float lookAheadSmoothTime = 0.3f;

        Vector3 followVelocity;

        void Start()
        {
            if (target != null) SnapToTarget();
        }

        void LateUpdate()
        {
            if (target == null) return;

            bool lookingAhead = input != null && input.LookAheadHeld;
            float distance = lookingAhead ? lookAheadDistance : aimPush;
            float time = lookingAhead ? lookAheadSmoothTime : smoothTime;

            Vector2 offset = input != null ? CursorOffsetFromCenter(input.AimScreenPosition) * distance : Vector2.zero;
            Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, time);
        }

        /// <summary>대상 위치로 바로 옮긴다. 순간이동·씬 시작 때 쓴다.</summary>
        public void SnapToTarget()
        {
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
            followVelocity = Vector3.zero;
        }

        /// <summary>
        /// 화면 중앙→커서 벡터. 화면 짧은 변의 절반을 1로 보고 길이를 1로 자른다.
        /// 가로세로 비율이 달라도 모든 방향에서 같은 거리만큼 밀리게 하려고 짧은 변 하나로 나눈다.
        /// </summary>
        static Vector2 CursorOffsetFromCenter(Vector2 screenPosition)
        {
            Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
            float halfShortSide = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) * 0.5f);
            return Vector2.ClampMagnitude((screenPosition - center) / halfShortSide, 1f);
        }
    }
}
