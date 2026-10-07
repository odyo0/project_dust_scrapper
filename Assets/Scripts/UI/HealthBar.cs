using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 체력 막대. 맞으면 앞 막대는 바로 줄고, 뒤의 밝은 막대(잔상)는 잠깐 머물렀다가 따라 줄어서
    /// "방금 얼마나 깎였는지"가 눈에 보인다.
    /// 막대 길이는 RectTransform의 오른쪽 기준점(anchorMax.x)으로 조절한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthBar : MonoBehaviour
    {
        [SerializeField] Health health;
        [Tooltip("현재 체력 막대")]
        [SerializeField] RectTransform fill;
        [Tooltip("깎인 만큼 늦게 따라오는 잔상 막대")]
        [SerializeField] RectTransform trail;

        [Tooltip("잔상이 줄기 시작할 때까지 기다리는 시간 (초)")]
        [SerializeField, Min(0f)] float trailDelay = 0.4f;
        [Tooltip("잔상이 1초에 줄어드는 비율 (전체 = 1)")]
        [SerializeField, Min(0.01f)] float trailSpeed = 1.2f;

        float trailRatio = 1f;
        float trailWait;

        void OnEnable()
        {
            if (health != null) health.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
        }

        void Update()
        {
            if (health == null) return;

            float ratio = health.Current / health.Max;
            if (ratio >= trailRatio)
            {
                trailRatio = ratio;
            }
            else if (trailWait > 0f)
            {
                trailWait -= Time.deltaTime;
            }
            else
            {
                trailRatio = Mathf.MoveTowards(trailRatio, ratio, trailSpeed * Time.deltaTime);
            }

            SetRatio(fill, ratio);
            SetRatio(trail, trailRatio);
        }

        void OnDamaged(DamageInfo info) => trailWait = trailDelay;

        static void SetRatio(RectTransform bar, float ratio)
        {
            if (bar == null) return;
            Vector2 max = bar.anchorMax;
            max.x = ratio;
            bar.anchorMax = max;
        }
    }
}
