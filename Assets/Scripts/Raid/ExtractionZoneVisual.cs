using UnityEngine;

namespace DustScrapper.Raid
{
    /// <summary>
    /// 탈출 구역의 범위를 바닥에 원으로 그린다. 꺼져 있을 땐 흐리게, 켜지면 밝게 숨 쉬듯 깜빡이고,
    /// 플레이어가 구역 밖에 있으면 경고색으로. 판정은 ExtractionPoint가 하고 여기서는 읽기만 한다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public sealed class ExtractionZoneVisual : MonoBehaviour
    {
        [SerializeField] ExtractionPoint point;
        [Tooltip("원 그림 한 장의 지름 (칸). 크기를 구역 반지름에 맞출 때 쓴다.")]
        [SerializeField, Min(0.01f)] float spriteDiameter = 1f;

        [SerializeField] Color idleColor = new(1f, 0.85f, 0.3f, 0.25f);
        [SerializeField] Color activeColor = new(1f, 0.85f, 0.3f, 0.8f);
        [SerializeField] Color outOfZoneColor = new(1f, 0.35f, 0.3f, 0.8f);
        [Tooltip("켜져 있을 때 깜빡이는 빠르기 (번/초)")]
        [SerializeField, Min(0f)] float pulseRate = 1.2f;

        SpriteRenderer look;

        void Awake()
        {
            look = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one * (point.ZoneRadius * 2f / spriteDiameter);
        }

        void LateUpdate()
        {
            if (!point.IsActive)
            {
                look.color = idleColor;
                return;
            }

            Color color = point.PlayerInZone ? activeColor : outOfZoneColor;
            color.a *= Mathf.Lerp(0.55f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * pulseRate * Mathf.PI * 2f));
            look.color = color;
        }
    }
}
