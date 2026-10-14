using UnityEngine;

namespace DustScrapper.Glow
{
    /// <summary>글로우병이 오를수록 몸 주위가 빛난다(빛 그림의 진하기·크기). 지금은 보이기만 하고 게임에 주는 영향은 기획 빈칸이다.</summary>
    [DisallowMultipleComponent]
    public sealed class GlowHalo : MonoBehaviour
    {
        [SerializeField] GlowSickness sickness;
        [SerializeField] SpriteRenderer halo;
        [SerializeField, Range(0f, 1f)] float maxAlpha = 0.55f;
        [SerializeField] float minScale = 0.8f;
        [SerializeField] float maxScale = 2.2f;
        [Tooltip("높을 때 깜빡이는 빠르기")]
        [SerializeField] float pulseSpeed = 4f;

        void Update()
        {
            float ratio = sickness.Ratio;
            halo.enabled = ratio > 0f;
            if (!halo.enabled) return;

            // 위험할수록(높을수록) 깜빡임이 커진다.
            float pulse = 1f - 0.3f * ratio * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
            Color color = halo.color;
            color.a = maxAlpha * ratio * pulse;
            halo.color = color;
            halo.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, ratio);
        }
    }
}
