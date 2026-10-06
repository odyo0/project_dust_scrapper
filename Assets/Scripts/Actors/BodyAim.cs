using UnityEngine;

namespace DustScrapper.Actors
{
    /// <summary>
    /// 상체 그림을 조준점 쪽으로 즉시 돌린다. 그림은 오른쪽(+X)을 보고 그린다는 약속을 따른다.
    /// 무기는 나중에 AimDirection을 읽어 발사 방향으로 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BodyAim : MonoBehaviour
    {
        const float MinAimDistance = 0.05f;

        [Tooltip("회전시킬 상체 그림")]
        [SerializeField] Transform upperBody;

        public Vector2 AimDirection { get; private set; } = Vector2.right;

        /// <summary>월드 좌표의 한 점을 바라보게 한다. 너무 가까우면 직전 방향을 유지한다.</summary>
        public void AimAt(Vector2 worldPoint)
        {
            Vector2 toPoint = worldPoint - (Vector2)transform.position;
            if (toPoint.sqrMagnitude < MinAimDistance * MinAimDistance) return;

            AimDirection = toPoint.normalized;
            float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
            upperBody.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
