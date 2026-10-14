using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Glow
{
    /// <summary>
    /// 연막 구름. 정해진 시간 동안 원 안을 지나는 시선을 막는다(적의 감각이 Blocks로 묻는다). 끝날 때 흐려지며 사라진다.
    /// 충돌체나 레이어 없이 켜져 있는 구름 목록으로 판정해서, 시선 검사 쪽은 한 줄만 더하면 된다.
    /// </summary>
    public sealed class SmokeCloud : MonoBehaviour
    {
        static readonly List<SmokeCloud> active = new();

        [SerializeField, Min(0.1f)] float radius = 2.2f;
        [SerializeField, Min(0.1f)] float duration = 8f;
        [Tooltip("퍼지는 데 걸리는 시간 (초)")]
        [SerializeField, Min(0.01f)] float spreadTime = 0.4f;
        [Tooltip("사라지기 시작하는 남은 시간 (초)")]
        [SerializeField, Min(0.01f)] float fadeTime = 1.5f;
        [SerializeField] SpriteRenderer look;

        float age;
        float baseAlpha;

        /// <summary>a에서 b로 가는 시선이 켜진 연막 구름을 지나는가.</summary>
        public static bool Blocks(Vector2 a, Vector2 b)
        {
            foreach (var cloud in active)
            {
                if (cloud.CurrentRadius > 0f && SegmentHitsCircle(a, b, cloud.transform.position, cloud.CurrentRadius)) return true;
            }
            return false;
        }

        float CurrentRadius => radius * Mathf.Clamp01(age / spreadTime);

        void Awake()
        {
            if (look != null) baseAlpha = look.color.a;
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        void Update()
        {
            age += Time.deltaTime;
            if (age >= duration)
            {
                Destroy(gameObject);
                return;
            }
            if (look == null) return;
            float scale = CurrentRadius * 2f;
            look.transform.localScale = new Vector3(scale, scale, 1f);
            Color color = look.color;
            color.a = baseAlpha * Mathf.Clamp01((duration - age) / fadeTime);
            look.color = color;
        }

        static bool SegmentHitsCircle(Vector2 a, Vector2 b, Vector2 center, float r)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(center - a, ab) / ab.sqrMagnitude) : 0f;
            return (a + ab * t - center).sqrMagnitude <= r * r;
        }
    }
}
