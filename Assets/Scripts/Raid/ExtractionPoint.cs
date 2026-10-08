using System;
using DustScrapper.Interaction;
using DustScrapper.Noise;
using UnityEngine;

namespace DustScrapper.Raid
{
    /// <summary>
    /// 버티기 탈출 지점. E를 누르고 있어 신호기를 켜면 버티기가 시작되고,
    /// 플레이어가 구역 안에 있는 동안만 남은 시간이 줄어든다(구역 밖이면 멈춤). 다 버티면 Extracted를 알린다.
    /// 켜져 있는 동안 신호기가 일정 간격으로 맵 거의 전역에 들리는 소음을 내서 적을 불러들인다.
    /// 판을 끝내는 일은 한 판 관리(RaidSession)가 Extracted를 듣고 한다.
    /// </summary>
    public sealed class ExtractionPoint : Interactable
    {
        [Tooltip("플레이어가 이 안에 있어야 남은 시간이 줄어든다 (칸)")]
        [SerializeField, Min(0.5f)] float zoneRadius = 4f;

        [Tooltip("버텨야 하는 시간 (초)")]
        [SerializeField, Min(1f)] float holdOutDuration = 30f;

        [Header("신호기 소음")]
        [Tooltip("소음이 들리는 거리 (칸). 맵 거의 전역에 닿게 크게 둔다.")]
        [SerializeField, Min(0f)] float beaconNoiseRadius = 100f;
        [Tooltip("소음을 내는 간격 (초)")]
        [SerializeField, Min(0.5f)] float beaconNoiseInterval = 5f;

        Transform player;
        float noiseTimer;

        public float ZoneRadius => zoneRadius;
        public float HoldOutDuration => holdOutDuration;

        /// <summary>신호기를 켰는가.</summary>
        public bool IsActive { get; private set; }

        /// <summary>다 버텨서 탈출했는가.</summary>
        public bool IsCompleted { get; private set; }

        /// <summary>남은 버티기 시간 (초).</summary>
        public float Remaining { get; private set; }

        /// <summary>플레이어가 지금 구역 안에 있는가.</summary>
        public bool PlayerInZone { get; private set; }

        public override bool IsAvailable => !IsActive;
        public override string ActionName => "신호기 켜기";
        public override string TargetName => "탈출 신호기";

        /// <summary>신호기를 켰을 때. 나중에 적을 새로 불러내는 장치가 이것을 듣는다.</summary>
        public event Action HoldOutStarted;

        /// <summary>다 버텼을 때.</summary>
        public event Action Extracted;

        void Awake() => Remaining = holdOutDuration;

        public override string BlockedReason(PlayerInteractor interactor) => null;

        public override void Interact(PlayerInteractor interactor)
        {
            if (IsActive) return;
            IsActive = true;
            player = interactor.transform;
            Remaining = holdOutDuration;
            noiseTimer = 0f;
            HoldOutStarted?.Invoke();
        }

        void Update()
        {
            if (!IsActive || IsCompleted) return;

            noiseTimer -= Time.deltaTime;
            if (noiseTimer <= 0f)
            {
                noiseTimer = beaconNoiseInterval;
                NoiseBus.Emit(new NoiseEvent(transform.position, beaconNoiseRadius, gameObject));
            }

            PlayerInZone = player != null && player.gameObject.activeInHierarchy
                && Vector2.Distance(player.position, transform.position) <= zoneRadius;
            if (!PlayerInZone) return;

            Remaining = Mathf.Max(0f, Remaining - Time.deltaTime);
            if (Remaining > 0f) return;

            IsCompleted = true;
            Extracted?.Invoke();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, zoneRadius);
        }
    }
}
