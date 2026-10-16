using System.Collections.Generic;
using DustScrapper.Combat;
using DustScrapper.Enemies;
using DustScrapper.Interaction;
using DustScrapper.Navigation;
using DustScrapper.Noise;
using DustScrapper.Zones;
using UnityEngine;

namespace DustScrapper.Structures
{
    public enum DoorState
    {
        /// <summary>닫힘. 벽과 같다: 못 지나가고, 안 보이고, 총알도 막는다.</summary>
        Closed,
        /// <summary>살짝 열림. 못 지나가지만 틈으로 안이 보인다(유리와 같다: 경계 중이 아닌 적은 틈 너머의 플레이어를 못 알아챈다).</summary>
        Ajar,
        /// <summary>활짝 열림. 지나갈 수 있다.</summary>
        Open,
    }

    /// <summary>
    /// 문. E를 누를 때마다 닫힘 → 살짝 열림(엿보기) → 활짝 열림 → 닫힘 순서로 바뀐다. 문짝은 여는 쪽 반대로 젖혀진다.
    /// 적은 문 앞에 오면 스스로 활짝 연다(길찾기는 문을 지나갈 수 있는 곳으로 본다: NavPassable).
    /// 걷어차이면(IBashable) 확 열리면서 문 뒤에 있던 것들이 맞아 쓰러진다.
    /// 구역 바깥문이면 구역(MapZone)에 연결해 두어, 열려 있는 동안 구역 안팎으로 소리가 더 새게 한다(IZoneOpening).
    /// 막는 충돌체(blocker)와 보이는 문짝(hinge 아래)을 나눠서, 살짝 열린 모습이어도 틈으로 빠져나갈 수 없게 했다.
    /// </summary>
    [RequireComponent(typeof(NavPassable))]
    public sealed class Door : Interactable, IBashable, IZoneOpening
    {
        const string ClosedLayerName = "Walls";
        const string AjarLayerName = "Glass";
        const float EnemyCheckInterval = 0.1f;

        static readonly List<Collider2D> Overlaps = new();
        static readonly ContactFilter2D SolidOnly = new() { useTriggers = false };

        [SerializeField] string displayName = "문";

        [Tooltip("문짝 그림을 달고 있는 돌쩌귀. 이것을 돌려 열린 모습을 낸다. 닫힌 상태에서 오른쪽(+x)이 문짝 방향이다.")]
        [SerializeField] Transform hinge;
        [Tooltip("문간을 막는 충돌체. 닫힘·살짝 열림에서 켜고, 활짝 열면 끈다. 돌지 않는다.")]
        [SerializeField] Collider2D blocker;

        [Header("모습")]
        [SerializeField, Range(0f, 90f)] float ajarAngle = 20f;
        [SerializeField, Range(0f, 180f)] float openAngle = 95f;
        [Tooltip("문짝이 돌아가는 빠르기 (도/초)")]
        [SerializeField, Min(1f)] float swingSpeed = 540f;

        [Header("소리")]
        [Tooltip("활짝 열거나 닫을 때 소음 (칸). 살짝 열 때는 소리가 없다.")]
        [SerializeField, Min(0f)] float noiseRadius = 3f;

        [Header("적")]
        [Tooltip("움직이는 적이 문 가운데에서 이 거리(칸) 안에 오면 문을 활짝 연다")]
        [SerializeField, Min(0f)] float enemyOpenRadius = 1.3f;

        [Header("걷어차기")]
        [Tooltip("문 뒤에서 이 거리(칸) 안에 있는 것이 문짝에 맞는다")]
        [SerializeField, Min(0f)] float bashRadius = 1.3f;
        [SerializeField, Min(0f)] float bashDamage = 10f;
        [SerializeField, Min(0f)] float bashKnockback = 10f;
        [Tooltip("문짝에 맞은 것에 거는 디버프 (쓰러짐)")]
        [SerializeField] StatusEffectData bashEffect;
        [Tooltip("걷어차 열 때 소음 (칸)")]
        [SerializeField, Min(0f)] float bashNoiseRadius = 10f;

        readonly HashSet<Health> bashedThisTime = new();

        Quaternion closedRotation;
        float currentAngle;
        float targetAngle;
        float swingSign = 1f;
        float enemyCheckTimer;
        int closedLayer;
        int ajarLayer;

        public DoorState State { get; private set; }

        /// <summary>구역 소리 새기: 활짝 열려 있을 때만 열린 것으로 본다. 살짝 연 틈으로는 소리가 거의 안 샌다.</summary>
        public bool IsOpen => State == DoorState.Open;

        public override string ActionName => State switch
        {
            DoorState.Closed => "살짝 열기",
            DoorState.Ajar => "열기",
            _ => "닫기",
        };

        public override string TargetName => displayName;

        void Awake()
        {
            closedLayer = LayerMask.NameToLayer(ClosedLayerName);
            ajarLayer = LayerMask.NameToLayer(AjarLayerName);
            closedRotation = hinge.localRotation;
            SetState(DoorState.Closed, 1f, null);
            currentAngle = targetAngle;
            ApplyAngle();
        }

        void Update()
        {
            if (!Mathf.Approximately(currentAngle, targetAngle))
            {
                currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, swingSpeed * Time.deltaTime);
                ApplyAngle();
            }

            if (State == DoorState.Open) return;
            enemyCheckTimer -= Time.deltaTime;
            if (enemyCheckTimer > 0f) return;
            enemyCheckTimer = EnemyCheckInterval;
            OpenForPassingEnemy();
        }

        public override string BlockedReason(PlayerInteractor interactor) => null;

        public override void Interact(PlayerInteractor interactor)
        {
            Vector2 from = interactor.transform.position;
            switch (State)
            {
                case DoorState.Closed:
                    SetState(DoorState.Ajar, SignAwayFrom(from), interactor.gameObject);
                    break;
                case DoorState.Ajar:
                    // 살짝 연 쪽 그대로 마저 연다.
                    SetState(DoorState.Open, swingSign, interactor.gameObject);
                    break;
                default:
                    SetState(DoorState.Closed, swingSign, interactor.gameObject);
                    break;
            }
        }

        public void Bash(Vector2 from, Vector2 direction, GameObject source)
        {
            if (State == DoorState.Open) return;

            SetState(DoorState.Open, SignAwayFrom(from), source, silent: true);
            currentAngle = targetAngle; // 확 열린다
            ApplyAngle();
            NoiseBus.Emit(new NoiseEvent(Position, bashNoiseRadius, source));

            // 문 건너편(걷어찬 쪽 반대)에 있던 것들이 문짝에 맞는다.
            Vector2 away = (Position - from).normalized;
            Vector2 center = Position + away * bashRadius;
            bashedThisTime.Clear();
            int count = Physics2D.OverlapCircle(center, bashRadius, SolidOnly, Overlaps);
            for (int i = 0; i < count; i++)
            {
                Health health = Overlaps[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead || !bashedThisTime.Add(health)) continue;
                if (source != null && health.transform.IsChildOf(source.transform)) continue;
                var info = new DamageInfo(bashDamage, health.transform.position, away, bashKnockback, source, bashEffect);
                health.TakeDamage(info);
            }
        }

        void OpenForPassingEnemy()
        {
            int count = Physics2D.OverlapCircle(Position, enemyOpenRadius, SolidOnly, Overlaps);
            for (int i = 0; i < count; i++)
            {
                EnemyBrain enemy = Overlaps[i].GetComponentInParent<EnemyBrain>();
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                // 제자리에 서 있거나 두리번거리는 적은 문을 건드리지 않는다.
                if (enemy.State == EnemyState.Idle || enemy.State == EnemyState.Search) continue;
                SetState(DoorState.Open, SignAwayFrom(enemy.transform.position), enemy.gameObject);
                return;
            }
        }

        void SetState(DoorState next, float sign, GameObject source, bool silent = false)
        {
            bool wasOpen = State == DoorState.Open;
            State = next;
            swingSign = sign;
            targetAngle = next switch
            {
                DoorState.Ajar => ajarAngle * sign,
                DoorState.Open => openAngle * sign,
                _ => 0f,
            };
            blocker.enabled = next != DoorState.Open;
            blocker.gameObject.layer = next == DoorState.Closed ? closedLayer : ajarLayer;

            // 활짝 열거나 활짝 열린 것을 닫을 때만 소리가 난다. 살짝 열기는 조용하다.
            if (!silent && source != null && (next == DoorState.Open || wasOpen))
                NoiseBus.Emit(new NoiseEvent(Position, noiseRadius, source));
        }

        /// <summary>문짝이 from 반대쪽으로 젖혀지게 하는 회전 방향(+1 또는 -1).</summary>
        float SignAwayFrom(Vector2 from)
        {
            Vector2 along = hinge.parent != null ? (Vector2)(hinge.parent.rotation * closedRotation * Vector3.right) : Vector2.right;
            Vector2 swingSide = new(-along.y, along.x); // +90도로 돌렸을 때 문짝이 가는 쪽
            return Vector2.Dot(swingSide, Position - from) >= 0f ? 1f : -1f;
        }

        void ApplyAngle() => hinge.localRotation = closedRotation * Quaternion.Euler(0f, 0f, currentAngle);
    }
}
