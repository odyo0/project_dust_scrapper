using UnityEngine;

namespace DustScrapper.Effects
{
    /// <summary>
    /// 잠깐 보였다가 줄어들며 사라지는 효과(탄이 벽에 맞은 불똥 등). 정해진 시간이 지나면 스스로 지운다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShortLivedEffect : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] float lifetime = 0.12f;
        [Tooltip("사라질 때 크기 비율 (1이면 그대로)")]
        [SerializeField, Min(0f)] float endScale = 0.2f;

        Vector3 startScale;
        float age;

        void Awake()
        {
            startScale = transform.localScale;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / lifetime;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            transform.localScale = startScale * Mathf.Lerp(1f, endScale, t);
        }
    }
}
