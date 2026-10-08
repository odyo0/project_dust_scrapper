using UnityEngine;

namespace DustScrapper.Vision
{
    /// <summary>
    /// 플레이어 시야의 모양을 정하는 수치 묶음: 내 주변(원) + 바라보는 방향(부채꼴).
    /// 기획상 시야 범위는 장비로 조정하므로, 장비가 이 묶음을 통째로 갈아 끼운다(MovementProfile과 같은 방식).
    /// 단위: 1 = 월드 1칸.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Vision Profile", fileName = "VisionProfile")]
    public sealed class VisionProfile : ScriptableObject
    {
        [Tooltip("사방으로 보이는 거리 (칸). 등 뒤도 이만큼은 보인다.")]
        [SerializeField, Min(0f)] float nearRadius = 2.5f;

        [Tooltip("바라보는 방향으로 보이는 거리 (칸)")]
        [SerializeField, Min(0f)] float forwardRadius = 8f;

        [Tooltip("멀리 보이는 부채꼴의 전체 각도 (도)")]
        [SerializeField, Range(0f, 360f)] float forwardAngle = 80f;

        [Tooltip("부채꼴 가장자리에서 먼 시야가 가까운 시야로 줄어드는 폭 (도). 0이면 칼같이 끊긴다.")]
        [SerializeField, Range(0f, 90f)] float angleFeather = 10f;

        public float NearRadius => nearRadius;
        public float ForwardRadius => forwardRadius;
        public float ForwardAngle => forwardAngle;
        public float AngleFeather => angleFeather;

        /// <summary>바라보는 방향에서 angle(도)만큼 벗어난 쪽으로 보이는 거리.</summary>
        public float RadiusAt(float angle)
        {
            float half = forwardAngle * 0.5f;
            float t = angleFeather > 0f
                ? Mathf.Clamp01((half + angleFeather * 0.5f - angle) / angleFeather)
                : (angle <= half ? 1f : 0f);
            return Mathf.Lerp(nearRadius, Mathf.Max(nearRadius, forwardRadius), t);
        }
    }
}
