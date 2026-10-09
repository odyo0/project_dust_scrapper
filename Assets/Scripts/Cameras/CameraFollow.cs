using DustScrapper.Player;
using UnityEngine;

namespace DustScrapper.Cameras
{
    /// <summary>
    /// 대상(플레이어)을 부드럽게 따라간다. 마우스가 화면 중앙에서 벗어난 만큼 그쪽으로 살짝 밀리고,
    /// 멀리 보기 키를 누르는 동안에는 화면 크기만큼 크게 밀린다.
    /// 마우스의 "화면" 위치를 기준으로 삼아서, 카메라가 움직여도 밀림 정도가 흔들리지 않는다.
    /// 멀리 보기 거리를 고정 칸 수가 아니라 화면 크기로 정하는 이유: 화면은 가로가 세로보다 훨씬 길어서,
    /// 한 숫자로 정하면 가로로는 덜 보이고 세로로는 플레이어가 화면 밖으로 나간다.
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
        [Tooltip("멀리 보기 중 커서를 화면 끝까지 밀었을 때, 플레이어가 화면 가장자리에서 떨어져 있을 거리 (칸)")]
        [SerializeField, Min(0f)] float lookAheadEdgeMargin = 1.5f;
        [SerializeField, Min(0.01f)] float lookAheadSmoothTime = 0.3f;
        [Tooltip("보이는 넓이를 잴 카메라. 비워 두면 자식에서 찾는다.")]
        [SerializeField] Camera viewCamera;

        Vector3 followVelocity;

        void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        void Start()
        {
            if (target != null) SnapToTarget();
        }

        void LateUpdate()
        {
            if (target == null) return;

            bool lookingAhead = input != null && input.LookAheadHeld;
            float time = lookingAhead ? lookAheadSmoothTime : smoothTime;

            Vector2 offset = Vector2.zero;
            if (input != null)
            {
                offset = lookingAhead
                    ? Vector2.Scale(CursorOffsetPerAxis(input.AimScreenPosition), LookAheadReach())
                    : CursorOffsetFromCenter(input.AimScreenPosition) * aimPush;
            }
            Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, time);
        }

        /// <summary>대상 위치로 바로 옮긴다. 순간이동·씬 시작 때 쓴다.</summary>
        public void SnapToTarget()
        {
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
            followVelocity = Vector3.zero;
        }

        /// <summary>멀리 보기 때 가로·세로로 밀 수 있는 거리 (칸). 보이는 넓이의 절반에서 가장자리 여백을 뺀다.</summary>
        Vector2 LookAheadReach()
        {
            if (viewCamera == null || !viewCamera.orthographic) return Vector2.zero;
            float halfHeight = viewCamera.orthographicSize;
            float halfWidth = halfHeight * viewCamera.aspect;
            return new Vector2(Mathf.Max(0f, halfWidth - lookAheadEdgeMargin), Mathf.Max(0f, halfHeight - lookAheadEdgeMargin));
        }

        /// <summary>
        /// 화면 중앙→커서 벡터를 가로·세로 따로 -1~1로 맞춘다. 커서가 화면 오른쪽 끝이면 x = 1, 위쪽 끝이면 y = 1.
        /// 멀리 보기에서 커서가 닿은 화면 끝 쪽을 그대로 끝까지 보여 주려고 축마다 따로 나눈다.
        /// </summary>
        static Vector2 CursorOffsetPerAxis(Vector2 screenPosition)
        {
            Vector2 half = new Vector2(Mathf.Max(1f, Screen.width * 0.5f), Mathf.Max(1f, Screen.height * 0.5f));
            Vector2 offset = (screenPosition - half) / half;
            return new Vector2(Mathf.Clamp(offset.x, -1f, 1f), Mathf.Clamp(offset.y, -1f, 1f));
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
