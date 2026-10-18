using System.Collections.Generic;
using DustScrapper.Combat;
using DustScrapper.Enemies;
using DustScrapper.Lantern;
using UnityEngine;

namespace DustScrapper.Indicators
{
    /// <summary>
    /// 빛 랜턴(RevealEnemiesEffect)이 드러낸 적을 시야 어둠 위에 표시한다. 드러난 동안은 적이 움직이는 대로 따라가고(기획),
    /// 끝날 무렵 흐려진다. 쓴 자리에서 둘레까지 퍼지는 고리도 한 번 그려 "어디까지 훑었는지"를 보여 준다. 소리는 내지 않는다.
    /// 효과 쪽이 알려 주는 것(중심·둘레·시간)만 듣고, 어떤 적이 둘레 안에 있는지는 여기서 찾는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RevealedEnemyMarkers : MonoBehaviour
    {
        [Tooltip("적 위에 띄울 그림 (고리 모양 권장)")]
        [SerializeField] Sprite markerSprite;
        [Tooltip("퍼지는 고리 그림")]
        [SerializeField] Sprite pulseSprite;
        [SerializeField] Color markerColor = new(0.6f, 0.92f, 1f, 0.9f);
        [SerializeField] Color pulseColor = new(0.6f, 0.92f, 1f, 0.45f);
        [Tooltip("적 표시의 지름 (칸)")]
        [SerializeField, Min(0.1f)] float markerSize = 1.1f;
        [Tooltip("고리가 둘레까지 퍼지는 시간 (초)")]
        [SerializeField, Min(0.05f)] float pulseTime = 0.45f;
        [Tooltip("끝나기 전 흐려지는 시간 (초)")]
        [SerializeField, Min(0f)] float fadeTime = 0.6f;
        [Tooltip("시야 어둠(100)보다 위에 그린다")]
        [SerializeField] int sortingOrder = 112;

        readonly List<Mark> marks = new();
        SpriteRenderer pulse;
        float pulseAge = -1f;
        float pulseRadius;

        void OnEnable() => RevealEnemiesEffect.Revealed += OnRevealed;

        void OnDisable()
        {
            RevealEnemiesEffect.Revealed -= OnRevealed;
            foreach (var mark in marks) mark.Hide();
            if (pulse != null) pulse.enabled = false;
            pulseAge = -1f;
        }

        void OnRevealed(Vector2 center, float radius, float duration)
        {
            float radiusSq = radius * radius;
            foreach (var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                if (((Vector2)enemy.transform.position - center).sqrMagnitude > radiusSq) continue;
                var health = enemy.GetComponent<Health>();
                if (health != null && health.IsDead) continue;
                MarkFor(enemy.transform, health).Show(duration);
            }
            StartPulse(center, radius);
        }

        Mark MarkFor(Transform target, Health health)
        {
            Mark free = null;
            foreach (var mark in marks)
            {
                if (mark.Target == target) return mark;
                if (free == null && !mark.Active) free = mark;
            }
            if (free == null)
            {
                free = new Mark(CreateRenderer("Revealed", markerSprite, markerSize));
                marks.Add(free);
            }
            free.Bind(target, health);
            return free;
        }

        void StartPulse(Vector2 center, float radius)
        {
            if (pulseSprite == null) return;
            if (pulse == null) pulse = CreateRenderer("RevealPulse", pulseSprite, 1f);
            pulse.transform.position = center;
            pulseRadius = radius;
            pulseAge = 0f;
            pulse.enabled = true;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            foreach (var mark in marks) mark.Tick(dt, markerColor, fadeTime);

            if (pulse == null || pulseAge < 0f) return;
            pulseAge += dt;
            float t = Mathf.Clamp01(pulseAge / pulseTime);
            pulse.transform.localScale = Vector3.one * (pulseRadius * 2f * t / SpriteWidth(pulseSprite));
            Color c = pulseColor;
            c.a *= 1f - t;
            pulse.color = c;
            if (t >= 1f)
            {
                pulse.enabled = false;
                pulseAge = -1f;
            }
        }

        SpriteRenderer CreateRenderer(string objectName, Sprite sprite, float worldSize)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = false;
            go.transform.localScale = Vector3.one * (worldSize / SpriteWidth(sprite));
            return renderer;
        }

        static float SpriteWidth(Sprite sprite) => sprite != null && sprite.bounds.size.x > 0f ? sprite.bounds.size.x : 1f;

        /// <summary>드러난 적 하나의 표시. 적을 따라가다가 시간이 다 되거나 적이 쓰러지면 꺼진다.</summary>
        sealed class Mark
        {
            readonly SpriteRenderer renderer;
            Health health;
            float timeLeft;

            public Mark(SpriteRenderer renderer) => this.renderer = renderer;

            public Transform Target { get; private set; }
            public bool Active => timeLeft > 0f;

            public void Bind(Transform target, Health targetHealth)
            {
                Target = target;
                health = targetHealth;
            }

            /// <summary>보이는 시간을 다시 채운다(이미 보이는 중이면 더 긴 쪽으로).</summary>
            public void Show(float duration)
            {
                timeLeft = Mathf.Max(timeLeft, duration);
                renderer.enabled = true;
            }

            public void Hide()
            {
                timeLeft = 0f;
                Target = null;
                renderer.enabled = false;
            }

            public void Tick(float dt, Color color, float fadeTime)
            {
                if (!Active) return;
                bool gone = Target == null || !Target.gameObject.activeInHierarchy || (health != null && health.IsDead);
                timeLeft -= dt;
                if (gone || timeLeft <= 0f)
                {
                    Hide();
                    return;
                }
                renderer.transform.position = Target.position;
                Color c = color;
                if (fadeTime > 0f && timeLeft < fadeTime) c.a *= timeLeft / fadeTime;
                renderer.color = c;
            }
        }
    }
}
