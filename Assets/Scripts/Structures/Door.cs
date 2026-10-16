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
        /// <summary>열림. 지나갈 수 있다.</summary>
        Open,
    }

    /// <summary>
    /// 문. E를 누르면 열리고 다시 누르면 닫힌다. 문짝은 여는 쪽 반대로 젖혀진다.
    /// 문짝은 한 짝(보통 문) 또는 두 짝(큰 문, 양쪽 문)이다. 짝이 몇이든 막는 충돌체·상호작용·걷어차기는 문 하나가 맡는다.
    /// 적은 문 앞에 오면 스스로 연다(길찾기는 문을 지나갈 수 있는 곳으로 본다: NavPassable).
    /// 걷어차이면(IBashable) 확 열리면서 문 뒤에 있던 것들이 맞아 쓰러진다.
    /// 구역 바깥문이면 구역(MapZone)에 연결해 두어, 열려 있는 동안 구역 안팎으로 소리가 더 새게 한다(IZoneOpening).
    /// </summary>
    [RequireComponent(typeof(NavPassable))]
    public sealed class Door : Interactable, IBashable, IZoneOpening
    {
        const float EnemyCheckInterval = 0.1f;

        static readonly List<Collider2D> Overlaps = new();
        static readonly ContactFilter2D SolidOnly = new() { useTriggers = false };

        [SerializeField] string displayName = "문";

        [Tooltip("문짝마다 하나씩, 문짝 그림을 달고 있는 돌쩌귀. 닫힌 상태에서 각 돌쩌귀의 오른쪽(+x)이 그 문짝 방향이다. 양쪽 문이면 둘을 문간 양 끝에 두고 서로 마주 보게 한다.")]
        [SerializeField] Transform[] hinges;
        [Tooltip("문간을 막는 충돌체. 닫혀 있으면 켜고, 열면 끈다. 돌지 않는다.")]
        [SerializeField] Collider2D blocker;

        [Header("모습")]
        [SerializeField, Range(0f, 180f)] float openAngle = 95f;
        [Tooltip("열고 닫히는 빠르기 (1초에 0→1)")]
        [SerializeField, Min(0.1f)] float swingSpeed = 6f;

        [Header("소리")]
        [Tooltip("열거나 닫을 때 소음 (칸)")]
        [SerializeField, Min(0f)] float noiseRadius = 3f;

        [Header("적")]
        [Tooltip("움직이는 적이 문 가운데에서 이 거리(칸) 안에 오면 문을 연다")]
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

        Quaternion[] closedRotations;
        float[] swingSigns;
        float openAmount;
        float enemyCheckTimer;

        public DoorState State { get; private set; }

        /// <summary>구역 소리 새기에 쓴다.</summary>
        public bool IsOpen => State == DoorState.Open;

        public override string ActionName => State == DoorState.Closed ? "열기" : "닫기";
        public override string TargetName => displayName;

        void Awake()
        {
            closedRotations = new Quaternion[hinges.Length];
            swingSigns = new float[hinges.Length];
            for (int i = 0; i < hinges.Length; i++)
            {
                closedRotations[i] = hinges[i].localRotation;
                swingSigns[i] = 1f;
            }
            SetState(DoorState.Closed, null);
            ApplyLeaves();
        }

        void Update()
        {
            float target = State == DoorState.Open ? 1f : 0f;
            if (!Mathf.Approximately(openAmount, target))
            {
                openAmount = Mathf.MoveTowards(openAmount, target, swingSpeed * Time.deltaTime);
                ApplyLeaves();
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
            if (State == DoorState.Closed) OpenAwayFrom(interactor.transform.position, interactor.gameObject);
            else SetState(DoorState.Closed, interactor.gameObject);
        }

        public void Bash(Vector2 from, Vector2 direction, GameObject source)
        {
            if (State == DoorState.Open) return;

            SwingAwayFrom(from);
            State = DoorState.Open;
            blocker.enabled = false;
            openAmount = 1f; // 확 열린다
            ApplyLeaves();
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
                health.TakeDamage(new DamageInfo(bashDamage, health.transform.position, away, bashKnockback, source, bashEffect));
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
                OpenAwayFrom(enemy.transform.position, enemy.gameObject);
                return;
            }
        }

        void OpenAwayFrom(Vector2 from, GameObject source)
        {
            SwingAwayFrom(from);
            SetState(DoorState.Open, source);
        }

        void SetState(DoorState next, GameObject source)
        {
            bool changed = State != next;
            State = next;
            blocker.enabled = next == DoorState.Closed;
            if (changed && source != null) NoiseBus.Emit(new NoiseEvent(Position, noiseRadius, source));
        }

        /// <summary>문짝마다 from 반대쪽으로 젖혀지는 회전 방향(+1 또는 -1)을 정한다. 양쪽 문은 두 짝이 같은 쪽으로 열린다.</summary>
        void SwingAwayFrom(Vector2 from)
        {
            Vector2 away = Position - from;
            for (int i = 0; i < hinges.Length; i++)
            {
                Quaternion closedWorld = hinges[i].parent != null ? hinges[i].parent.rotation * closedRotations[i] : closedRotations[i];
                Vector2 along = closedWorld * Vector3.right;
                Vector2 swingSide = new(-along.y, along.x); // +로 돌렸을 때 문짝이 가는 쪽
                swingSigns[i] = Vector2.Dot(swingSide, away) >= 0f ? 1f : -1f;
            }
        }

        void ApplyLeaves()
        {
            for (int i = 0; i < hinges.Length; i++)
                hinges[i].localRotation = closedRotations[i] * Quaternion.Euler(0f, 0f, openAngle * openAmount * swingSigns[i]);
        }
    }
}
