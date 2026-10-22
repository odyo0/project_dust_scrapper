using System;
using DustScrapper.Combat;
using DustScrapper.Navigation;
using DustScrapper.Noise;
using DustScrapper.Zones;
using UnityEngine;

namespace DustScrapper.Structures
{
    /// <summary>
    /// 유리창. 유리(Glass) 레이어에 있어서 못 지나가지만 플레이어는 너머를 본다(플레이어 시야는 벽 레이어만 막힌다).
    /// 적은 경계 중일 때만 유리 너머를 본다(EnemySenses.SeesThroughGlass) — 플레이어가 창으로 안을 미리 살피는 재미를 위해서.
    /// 체력(Health)이 있어서 쏘거나 베면 깨진다: 큰 소리가 나고, 그 자리로 드나들 수 있게 되고, 구역 안팎으로 소리가 더 샌다.
    /// 그림은 자기 SpriteRenderer(look, 깨지면 색을 바꿈)이거나, 타일맵 건물이면 그림 없이 Shattered를 듣는 쪽(WindowTileLook)이 타일을 바꾼다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public sealed class Window : MonoBehaviour, IZoneOpening
    {
        [SerializeField] SpriteRenderer look;
        [Tooltip("창을 막는 충돌체. 깨지면 끈다.")]
        [SerializeField] Collider2D blocker;
        [SerializeField] Color brokenColor = new(0.65f, 0.8f, 0.85f, 0.2f);
        [Tooltip("깨질 때 소음 (칸)")]
        [SerializeField, Min(0f)] float breakNoiseRadius = 10f;

        Health health;

        public bool IsBroken { get; private set; }
        public bool IsOpen => IsBroken;

        /// <summary>깨진 순간 한 번. 그림을 바꾸는 쪽이 듣는다.</summary>
        public event Action Shattered;

        void Awake()
        {
            health = GetComponent<Health>();
            if (look == null) look = GetComponent<SpriteRenderer>();
            if (blocker == null) blocker = GetComponent<Collider2D>();
        }

        void OnEnable() => health.Died += OnShattered;
        void OnDisable() => health.Died -= OnShattered;

        void OnShattered(DamageInfo info)
        {
            if (IsBroken) return;
            IsBroken = true;

            Bounds area = blocker.bounds; // 끄기 전에 재 둔다(꺼진 충돌체는 범위가 비어 있다)
            blocker.enabled = false;
            if (look != null) look.color = brokenColor;
            NoiseBus.Emit(new NoiseEvent(transform.position, breakNoiseRadius, info.Source));

            NavGrid grid = NavGrid.Active;
            if (grid != null) grid.RebuildArea(area);
            Shattered?.Invoke();
        }
    }
}
