using System;
using DustScrapper.Actors;
using DustScrapper.Combat;
using DustScrapper.Noise;
using UnityEngine;

namespace DustScrapper.Enemies
{
    /// <summary>
    /// 적의 눈과 귀. 대상(플레이어)이 보이는지, 어떤 소리가 들렸는지만 판단해서 알려 주고,
    /// 그래서 무엇을 할지는 두뇌(EnemyBrain)가 정한다.
    /// 눈: 바라보는 방향의 부채꼴 + 아주 가까우면 등 뒤도 알아챔. 벽 뒤는 못 본다.
    /// 귀: NoiseBus에 퍼진 소리 중, 소리의 들리는 거리 안에 있으면 듣는다.
    /// </summary>
    [DefaultExecutionOrder(-55)]
    [DisallowMultipleComponent]
    public sealed class EnemySenses : MonoBehaviour
    {
        const string TargetTag = "Player";

        [Tooltip("바라보는 방향. 시야 부채꼴의 기준이다.")]
        [SerializeField] BodyAim bodyAim;

        [Header("보기")]
        [Tooltip("볼 수 있는 거리 (칸)")]
        [SerializeField, Min(0f)] float sightRange = 8f;
        [Tooltip("시야 부채꼴의 전체 각도 (도)")]
        [SerializeField, Range(0f, 360f)] float sightAngle = 120f;
        [Tooltip("이 거리(칸) 안이면 등 뒤라도 알아챈다")]
        [SerializeField, Min(0f)] float awarenessRadius = 1.5f;
        [Tooltip("쫓는 중에는 이 거리(칸) 안이면 시야 각도와 상관없이 본다. 그보다 멀면 쫓는 중에도 부채꼴 안이어야 본다.")]
        [SerializeField, Min(0f)] float trackingRadius = 4f;
        [Tooltip("시야를 가리는 레이어(벽). 다른 적이나 플레이어 몸은 시야를 가리지 않는다.")]
        [SerializeField] LayerMask sightBlockers;

        [Header("듣기")]
        [Tooltip("소리가 들리는 거리에 곱하는 값. 귀가 밝은 적은 1보다 크게.")]
        [SerializeField, Min(0f)] float hearingScale = 1f;

        Transform target;
        Health targetHealth;

        public Transform Target => target;

        /// <summary>이번 프레임에 대상이 보이는가.</summary>
        public bool CanSeeTarget { get; private set; }

        /// <summary>대상을 마지막으로 본 위치.</summary>
        public Vector2 LastSeenPosition { get; private set; }

        /// <summary>
        /// 켜 두면 가까운 거리(trackingRadius) 안에서는 시야 각도를 무시한다. 쫓는 중에는 대상을 의식하고 있어서
        /// 바로 옆으로 돌아 들어가도 놓치지 않게 두뇌가 켠다. 멀리서 모퉁이를 돌면 놓칠 수 있다.
        /// </summary>
        public bool IsTracking { get; set; }

        /// <summary>들리는 소리가 났을 때.</summary>
        public event Action<NoiseEvent> NoiseHeard;

        void Awake()
        {
            if (bodyAim == null) bodyAim = GetComponent<BodyAim>();
            GameObject found = GameObject.FindWithTag(TargetTag);
            if (found == null)
            {
                Debug.LogWarning($"{name}: '{TargetTag}' 태그가 붙은 대상이 없다.", this);
                return;
            }
            target = found.transform;
            targetHealth = found.GetComponent<Health>();
        }

        void OnEnable() => NoiseBus.Emitted += OnNoise;

        void OnDisable()
        {
            NoiseBus.Emitted -= OnNoise;
            CanSeeTarget = false;
            IsTracking = false;
        }

        void Update()
        {
            CanSeeTarget = CheckSight();
            if (CanSeeTarget) LastSeenPosition = target.position;
        }

        bool CheckSight()
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            if (targetHealth != null && targetHealth.IsDead) return false;

            Vector2 origin = transform.position;
            Vector2 toTarget = (Vector2)target.position - origin;
            float distance = toTarget.magnitude;
            if (distance > sightRange) return false;

            bool inCone = distance <= awarenessRadius || (IsTracking && distance <= trackingRadius) ||
                          Vector2.Angle(bodyAim.AimDirection, toTarget) <= sightAngle * 0.5f;
            if (!inCone) return false;

            return Physics2D.Linecast(origin, target.position, sightBlockers).collider == null;
        }

        void OnNoise(NoiseEvent noise)
        {
            if (noise.Source == gameObject) return;
            float radius = noise.Radius * hearingScale;
            if (((Vector2)transform.position - noise.Position).sqrMagnitude > radius * radius) return;
            NoiseHeard?.Invoke(noise);
        }

        void OnDrawGizmosSelected()
        {
            // 디버그용: 시야 부채꼴과 등 뒤 알아채는 거리를 Scene 창에 그린다.
            Vector2 origin = transform.position;
            Vector2 forward = bodyAim != null && Application.isPlaying ? bodyAim.AimDirection : Vector2.right;
            Gizmos.color = CanSeeTarget ? new Color(1f, 0.3f, 0.2f, 0.9f) : new Color(1f, 1f, 0.4f, 0.6f);
            float half = sightAngle * 0.5f;
            Vector2 left = Quaternion.Euler(0f, 0f, half) * forward;
            Vector2 right = Quaternion.Euler(0f, 0f, -half) * forward;
            Gizmos.DrawLine(origin, origin + left * sightRange);
            Gizmos.DrawLine(origin, origin + right * sightRange);
            const int Segments = 16;
            Vector2 previous = origin + right * sightRange;
            for (int i = 1; i <= Segments; i++)
            {
                Vector2 next = origin + (Vector2)(Quaternion.Euler(0f, 0f, -half + sightAngle * i / Segments) * forward) * sightRange;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
            Gizmos.DrawWireSphere(origin, awarenessRadius);
        }
    }
}
