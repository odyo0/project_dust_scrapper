using DustScrapper.Raid;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 탈출 지점이 화면 밖에 있으면 화면 가장자리에 그쪽을 가리키는 표시를 띄운다. 화면 안에 들어오면 숨긴다.
    /// 지도가 생기거나 "표시 없이 외우기"로 바꾸면 이 스크립트만 끄면 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObjectiveMarker : MonoBehaviour
    {
        [SerializeField] RaidSession session;
        [Tooltip("가장자리 표시. 오른쪽(+X)을 가리키게 만든다. 이 스크립트가 붙은 물체 자신이면 안 된다.")]
        [SerializeField] RectTransform arrow;
        [Tooltip("비워 두면 Main Camera를 쓴다.")]
        [SerializeField] Camera viewCamera;

        [Tooltip("화면 가장자리에서 띄울 거리 (화면 비율, 0~0.5)")]
        [SerializeField, Range(0f, 0.4f)] float edgeInset = 0.06f;

        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
        }

        void LateUpdate()
        {
            ExtractionPoint target = FindTarget();
            Vector2 viewport = target != null ? (Vector2)viewCamera.WorldToViewportPoint(target.transform.position) : Vector2.zero;
            bool onScreen = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            bool show = target != null && !session.IsOver && !onScreen;
            if (arrow.gameObject.activeSelf != show) arrow.gameObject.SetActive(show);
            if (!show) return;

            // 화면 가운데에서 목표 쪽으로 뻗은 선이 안쪽 사각형 테두리와 만나는 곳에 둔다.
            Vector2 fromCenter = viewport - new Vector2(0.5f, 0.5f);
            float limit = 0.5f - edgeInset;
            float scale = limit / Mathf.Max(Mathf.Abs(fromCenter.x), Mathf.Abs(fromCenter.y));
            Vector2 edge = new Vector2(0.5f, 0.5f) + fromCenter * scale;

            arrow.position = new Vector2(edge.x * Screen.width, edge.y * Screen.height);
            float angle = Mathf.Atan2(fromCenter.y * Screen.height, fromCenter.x * Screen.width) * Mathf.Rad2Deg;
            arrow.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        ExtractionPoint FindTarget()
        {
            foreach (var point in session.ExtractionPoints)
            {
                if (!point.IsCompleted) return point;
            }
            return null;
        }
    }
}
