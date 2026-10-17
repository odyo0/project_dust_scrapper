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
    /// 문 하나(문짝 한 짝 또는 양쪽 문 두 짝). 문짝(DoorLeaf)은 각도를 스크립트로 돌리고, 이 스크립트는 문짝들을 묶어 바깥과 잇는다.
    /// F를 바로 누르면(누르고 있기 아님) 닫힌 문은 누른 사람 반대쪽으로 90도 열고, 열린 문은 닫는다(기획 2026-10-17, 배그처럼).
    /// 문 옆에 서 있어도 닿게, 손이 닿는지는 문간 전체와 열린 문짝까지의 가장 가까운 거리로 잰다(ClosestPoint).
    /// 적은 다가오면 스스로 연다(길찾기는 문간을 지나갈 수 있는 곳으로 본다: NavPassable).
    /// 걷어차이면(IBashable) 차기 세기에 따라 문짝이 아주 빠르게 열리거나 날아간다 — 버티는 힘은 문 종류(DoorData)가 정한다.
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

        [Tooltip("문간 깊이(벽 두께, 칸). F가 닿는지 잴 때 문간 가운데 선을 플레이어 쪽 벽면까지 당기는 데 쓴다.")]
        [SerializeField, Min(0f)] float doorwayDepth = 1f;

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

        /// <summary>남은 문짝이 모두 닫혀 있거나 닫히는 중인가(F를 누르면 열 차례인가).</summary>
        bool IsShut
        {
            get
            {
                foreach (var leaf in leaves)
                {
                    if (!leaf.IsDetached && !leaf.IsClosing) return false;
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
            foreach (var leaf in leaves) leaf.Setup(data, this);
        }

        void Update()
        {
            enemyCheckTimer -= Time.deltaTime;
            if (enemyCheckTimer > 0f) return;
            enemyCheckTimer = EnemyCheckInterval;
            if (IsShut) OpenForPassingEnemy();
        }

        /// <summary>문간 전체와 열린 문짝 중 from에서 가장 가까운 지점.</summary>
        public override Vector2 ClosestPoint(Vector2 from)
        {
            Vector2 best = Position;
            float bestDistance = float.MaxValue;
            foreach (var leaf in leaves)
            {
                if (leaf.IsDetached) continue;
                Vector2 point = leaf.ClosestPoint(from, Mathf.Max(0f, doorwayDepth * 0.5f - 0.05f));
                float distance = (point - from).sqrMagnitude;
                if (distance >= bestDistance) continue;
                best = point;
                bestDistance = distance;
            }
            return best;
        }

        public override string BlockedReason(PlayerInteractor interactor) => null;

        public override void Interact(PlayerInteractor interactor)
        {
            if (IsShut) OpenAwayFrom(interactor.transform.position, interactor.gameObject);
            else Close(interactor.gameObject);
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
                OpenAwayFrom(enemy.transform.position, enemy.gameObject);
                return;
            }
        }

        void OpenAwayFrom(Vector2 from, GameObject source)
        {
            foreach (var leaf in leaves) leaf.Open(leaf.SignAwayFrom(from));
            NoiseBus.Emit(new NoiseEvent(Position, data.NoiseRadius, source));
        }

        void Close(GameObject source)
        {
            foreach (var leaf in leaves) leaf.Close();
            NoiseBus.Emit(new NoiseEvent(Position, data.NoiseRadius, source));
        }
    }
}
