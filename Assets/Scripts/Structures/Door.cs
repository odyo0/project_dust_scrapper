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
    /// <summary>
    /// 문 하나(문짝 한 짝 또는 양쪽 문 두 짝). 문짝은 물리로 움직이는 DoorLeaf이고, 이 스크립트는 문짝들을 묶어 바깥과 잇는다.
    /// F를 바로 누르면(누르고 있기 아님) 닫힌 문은 누른 사람 반대쪽으로 밀어 열고, 열린 문은 닫히는 쪽으로 당긴다(기획 2026-10-17).
    /// 닫힌 문은 걸쇠가 걸려 F·발차기로만 열리고, 열린 문은 몸에 밀린다(DoorLeaf).
    /// 적은 다가오면 스스로 밀어 연다(길찾기는 문간을 지나갈 수 있는 곳으로 본다: NavPassable).
    /// 걷어차이면(IBashable) 차기 세기에 따라 문짝이 세게 돌거나 날아간다 — 버티는 힘은 문 종류(DoorData)가 정한다.
    /// 구역 바깥문이면 구역(MapZone)에 연결해 두어, 열려 있는 동안 구역 안팎으로 소리가 더 새게 한다(IZoneOpening).
    /// </summary>
    [RequireComponent(typeof(NavPassable))]
    public sealed class Door : Interactable, IBashable, IZoneOpening
    {
        const float EnemyCheckInterval = 0.1f;

        static readonly List<Collider2D> Overlaps = new();
        static readonly ContactFilter2D SolidOnly = new() { useTriggers = false };

        [SerializeField] string displayName = "문";
        [Tooltip("문 종류(무게·여닫는 힘·걸쇠·발차기를 버티는 힘·부딪힘 피해)")]
        [SerializeField] DoorData data;
        [Tooltip("문짝들. 양쪽 문이면 두 짝을 문간 양 끝에 두고 서로 마주 보게 한다.")]
        [SerializeField] DoorLeaf[] leaves;

        [Header("적")]
        [Tooltip("움직이는 적이 문 가운데에서 이 거리(칸) 안에 오면 문을 민다")]
        [SerializeField, Min(0f)] float enemyOpenRadius = 1.3f;

        float enemyCheckTimer;

        /// <summary>문짝이 하나라도 열린 문으로 칠 만큼 열렸거나 떨어져 나갔는가. 구역 소리 새기에 쓴다.</summary>
        public bool IsOpen
        {
            get
            {
                foreach (var leaf in leaves)
                {
                    if (leaf.IsOpen) return true;
                }
                return false;
            }
        }

        /// <summary>남은 문짝이 모두 닫힌 쪽에 있는가(F를 누르면 열 차례인가).</summary>
        bool IsShut
        {
            get
            {
                foreach (var leaf in leaves)
                {
                    if (!leaf.IsDetached && !leaf.IsLatched && Mathf.Abs(leaf.Angle) > data.OpenThresholdAngle) return false;
                }
                return true;
            }
        }

        bool AllDetached
        {
            get
            {
                foreach (var leaf in leaves)
                {
                    if (!leaf.IsDetached) return false;
                }
                return true;
            }
        }

        /// <summary>F를 바로 누른다(누르고 있기 아님).</summary>
        public override float HoldTime => 0f;

        public override bool IsAvailable => base.IsAvailable && !AllDetached;
        public override string ActionName => IsShut ? "열기" : "닫기";
        public override string TargetName => displayName;

        void Awake()
        {
            if (data == null) Debug.LogError($"{name}: 문 종류(DoorData)가 비어 있다.", this);
            foreach (var leaf in leaves) leaf.Setup(data, this, leaves);
        }

        void Update()
        {
            enemyCheckTimer -= Time.deltaTime;
            if (enemyCheckTimer > 0f) return;
            enemyCheckTimer = EnemyCheckInterval;
            if (IsShut) OpenForPassingEnemy();
        }

        public override string BlockedReason(PlayerInteractor interactor) => null;

        public override void Interact(PlayerInteractor interactor)
        {
            if (IsShut) PushAwayFrom(interactor.transform.position, interactor.gameObject);
            else PullClosed(interactor.gameObject);
        }

        public void Bash(Vector2 from, Vector2 direction, float force, GameObject source)
        {
            foreach (var leaf in leaves) leaf.Kick(leaf.SignAwayFrom(from), direction, force, source);
            NoiseBus.Emit(new NoiseEvent(Position, data.KickNoiseRadius, source));
        }

        /// <summary>문짝이 날아가 문에서 빠졌다. 문간이 뚫렸으니 길찾기 지도를 그 근처만 다시 만든다.</summary>
        public void NotifyLeafDetached(DoorLeaf leaf)
        {
            if (NavGrid.Active == null) return;
            var area = new Bounds(Position, Vector3.one * 4f);
            NavGrid.Active.RebuildArea(area);
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
                PushAwayFrom(enemy.transform.position, enemy.gameObject);
                return;
            }
        }

        void PushAwayFrom(Vector2 from, GameObject source)
        {
            foreach (var leaf in leaves) leaf.Push(leaf.SignAwayFrom(from));
            NoiseBus.Emit(new NoiseEvent(Position, data.NoiseRadius, source));
        }

        void PullClosed(GameObject source)
        {
            foreach (var leaf in leaves) leaf.PullClosed();
            NoiseBus.Emit(new NoiseEvent(Position, data.NoiseRadius, source));
        }
    }
}
