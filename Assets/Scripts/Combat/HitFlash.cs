using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 피해를 받으면 그림을 잠깐 한 색으로 번쩍인다. "맞았다"를 가장 빨리 알려 주는 반응이다.
    /// 지금은 흰 네모 그림에 색을 입힌 임시 그림이라 색만 바꿔도 하얗게 보인다.
    /// 실제 도트 그림이 들어오면 색을 곱하는 방식으로는 하얘지지 않으므로 번쩍임용 셰이더로 바꿔야 한다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public sealed class HitFlash : MonoBehaviour
    {
        [SerializeField] Color flashColor = Color.white;
        [SerializeField, Min(0f)] float duration = 0.06f;

        Health health;
        SpriteRenderer[] renderers;
        Color[] originalColors;
        float remaining;

        void Awake()
        {
            health = GetComponent<Health>();
            // 몸에 처음부터 붙어 있는 그림만 번쩍인다. 나중에 손에 쥐는 무기는 제외된다.
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i].color;
        }

        void OnEnable() => health.Damaged += OnDamaged;

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            Restore();
        }

        void Update()
        {
            if (remaining <= 0f) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f) Restore();
        }

        void OnDamaged(DamageInfo info)
        {
            for (int i = 0; i < renderers.Length; i++) renderers[i].color = flashColor;
            remaining = duration;
        }

        void Restore()
        {
            remaining = 0f;
            for (int i = 0; i < renderers.Length; i++) renderers[i].color = originalColors[i];
        }
    }
}
