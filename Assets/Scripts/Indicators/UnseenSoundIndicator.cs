using System.Collections.Generic;
using DustScrapper.Audio;
using DustScrapper.Vision;
using UnityEngine;

namespace DustScrapper.Indicators
{
    /// <summary>
    /// 플레이어 시야 밖에서 난 소리 자리에 빨간 원이 퍼졌다가 사라지게 한다. "벽 너머에서 뭔가 걷는다" 정도만 알려 주고
    /// 무엇인지는 보여 주지 않는다. 소리 데이터(SoundCue)에서 표시를 켠 소리만 띄운다(지금은 적 발소리).
    /// 소리가 날 때 SoundPlayer가 알려 주므로, 소리를 내는 쪽은 이 표시를 모른다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnseenSoundIndicator : MonoBehaviour
    {
        [Tooltip("퍼지는 원 그림")]
        [SerializeField] SpriteRenderer ringPrefab;
        [SerializeField] Color color = new(1f, 0.22f, 0.18f, 0.9f);
        [Tooltip("원이 퍼졌다 사라지는 시간 (초)")]
        [SerializeField, Min(0.05f)] float duration = 0.6f;
        [Tooltip("처음 크기 (소리 데이터의 크기에 곱하는 값)")]
        [SerializeField, Range(0f, 1f)] float startScale = 0.35f;
        [Tooltip("한 번에 떠 있을 수 있는 원의 수")]
        [SerializeField, Min(1)] int poolSize = 24;

        readonly List<Ring> rings = new();
        int next;

        void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                SpriteRenderer renderer = Instantiate(ringPrefab, transform);
                renderer.gameObject.SetActive(false);
                rings.Add(new Ring { Renderer = renderer });
            }
        }

        void OnEnable() => SoundPlayer.Played += OnSoundPlayed;
        void OnDisable() => SoundPlayer.Played -= OnSoundPlayed;

        void OnSoundPlayed(SoundCue cue, Vector2 position)
        {
            if (!cue.ShowWhenUnseen) return;
            PlayerVision vision = PlayerVision.Active;
            if (vision == null || vision.CanSee(position)) return;

            // 가까울수록 진하게. 소리가 들리지 않을 만큼 멀면 띄우지 않는다.
            float distance = Vector2.Distance(vision.Origin, position);
            float closeness = 1f - distance / cue.MaxDistance;
            if (closeness <= 0f) return;

            Ring ring = rings[next];
            next = (next + 1) % rings.Count;
            ring.Age = 0f;
            ring.Size = cue.IndicatorSize;
            ring.Strength = Mathf.Lerp(0.35f, 1f, closeness);
            ring.Renderer.transform.position = position;
            ring.Renderer.gameObject.SetActive(true);
            Draw(ring);
        }

        void Update()
        {
            foreach (Ring ring in rings)
            {
                if (!ring.Renderer.gameObject.activeSelf) continue;
                ring.Age += Time.deltaTime;
                if (ring.Age >= duration)
                {
                    ring.Renderer.gameObject.SetActive(false);
                    continue;
                }
                Draw(ring);
            }
        }

        void Draw(Ring ring)
        {
            float t = ring.Age / duration;
            float eased = 1f - (1f - t) * (1f - t);
            ring.Renderer.transform.localScale = Vector3.one * (ring.Size * Mathf.Lerp(startScale, 1f, eased));
            Color c = color;
            c.a *= ring.Strength * (1f - t);
            ring.Renderer.color = c;
        }

        sealed class Ring
        {
            public SpriteRenderer Renderer;
            public float Age;
            public float Size;
            public float Strength;
        }
    }
}
