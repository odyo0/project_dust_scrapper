using DustScrapper.Player;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 마우스 커서 자리에 그리는 조준점. 운영체제 커서는 숨긴다.
    /// 총을 들면 네 개의 눈금이 지금 탄 퍼짐만큼 벌어져서, 커서 거리에서 탄이 떨어질 수 있는 폭을 보여 준다.
    /// 연사하면 벌어지고 멈추면 모이므로 "끊어 쏘면 정확하다"를 화면에서 바로 읽을 수 있다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Crosshair : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] WeaponLoadout loadout;
        [Tooltip("비워 두면 Main Camera를 쓴다.")]
        [SerializeField] Camera viewCamera;
        [SerializeField] Canvas canvas;

        [Tooltip("위·아래·왼쪽·오른쪽 눈금 (이 순서)")]
        [SerializeField] RectTransform[] ticks = new RectTransform[4];

        [Tooltip("퍼짐이 0일 때도 띄워 둘 최소 간격 (화면 기준 크기)")]
        [SerializeField, Min(0f)] float minGap = 4f;
        [Tooltip("근접 무기를 들었을 때 간격")]
        [SerializeField, Min(0f)] float meleeGap = 2f;

        RectTransform rect;

        static readonly Vector2[] TickDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        void Awake()
        {
            rect = (RectTransform)transform;
            if (viewCamera == null) viewCamera = Camera.main;
        }

        void OnEnable() => Cursor.visible = false;
        void OnDisable() => Cursor.visible = true;

        void LateUpdate()
        {
            if (input == null) return;

            Vector2 screen = input.AimScreenPosition;
            rect.position = screen;

            float gap = loadout != null && loadout.Current is Gun gun ? SpreadGap(gun, screen) : meleeGap;
            for (int i = 0; i < ticks.Length && i < TickDirections.Length; i++)
            {
                if (ticks[i] != null) ticks[i].anchoredPosition = TickDirections[i] * gap;
            }
        }

        /// <summary>커서까지의 거리에서 퍼짐 각도만큼 벌어지는 폭을 화면 크기로 바꾼다.</summary>
        float SpreadGap(Gun gun, Vector2 screen)
        {
            if (viewCamera == null || !viewCamera.orthographic) return minGap;

            Vector2 cursorWorld = viewCamera.ScreenToWorldPoint(screen);
            float worldDistance = Vector2.Distance(loadout.transform.position, cursorWorld);
            float worldSpread = Mathf.Tan(gun.CurrentSpread * Mathf.Deg2Rad) * worldDistance;

            float screenPixelsPerUnit = Screen.height / (2f * viewCamera.orthographicSize);
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            return minGap + worldSpread * screenPixelsPerUnit / scale;
        }
    }
}
