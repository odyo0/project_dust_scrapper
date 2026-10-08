using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Navigation
{
    /// <summary>
    /// 목적지를 받으면 NavGrid로 길을 찾고, 지금 어느 쪽으로 가야 하는지(DesiredDirection)를 알려 준다.
    /// 몸을 직접 움직이지는 않는다. 얼마나 빨리 갈지(걷기·달리기)는 두뇌가 정해서 ActorMotor에 넣는다.
    /// 목적지가 움직이면(쫓는 대상) 일정 간격으로 길을 다시 찾고, 목적지가 곧장 보이면 길을 무시하고 바로 간다.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    [DisallowMultipleComponent]
    public sealed class PathFollower : MonoBehaviour
    {
        [Tooltip("길을 다시 찾는 간격 (초)")]
        [SerializeField, Min(0.05f)] float repathInterval = 0.4f;

        [Tooltip("목적지가 이 거리(칸) 넘게 바뀌면 간격을 기다리지 않고 바로 다시 찾는다")]
        [SerializeField, Min(0f)] float repathDistance = 2f;

        [Tooltip("경로의 꺾이는 점에 이 거리(칸) 안으로 오면 다음 점으로 넘어간다")]
        [SerializeField, Min(0.01f)] float waypointReach = 0.3f;

        [Tooltip("목적지에 이 거리(칸) 안으로 오면 도착으로 본다")]
        [SerializeField, Min(0.01f)] float arriveDistance = 0.4f;

        readonly List<Vector2> path = new();
        int waypoint;
        Vector2 pathDestination;
        float repathTimer;

        public bool HasDestination { get; private set; }
        public Vector2 Destination { get; private set; }

        /// <summary>목적지에 도착했는가.</summary>
        public bool HasArrived { get; private set; }

        /// <summary>목적지까지 가는 길이 없는가(막힌 곳).</summary>
        public bool IsUnreachable { get; private set; }

        /// <summary>지금 가야 할 방향. 길이 1, 멈춰야 하면 0.</summary>
        public Vector2 DesiredDirection { get; private set; }

        public void SetDestination(Vector2 point)
        {
            bool isNew = !HasDestination;
            HasDestination = true;
            Destination = point;
            if (isNew || (point - pathDestination).sqrMagnitude > repathDistance * repathDistance) Repath();
            UpdateDirection();
        }

        public void Stop()
        {
            HasDestination = false;
            HasArrived = false;
            IsUnreachable = false;
            path.Clear();
            DesiredDirection = Vector2.zero;
        }

        void OnDisable() => Stop();

        void Update()
        {
            if (!HasDestination) return;

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f) Repath();
            UpdateDirection();
        }

        void Repath()
        {
            repathTimer = repathInterval;
            pathDestination = Destination;
            waypoint = 0;

            NavGrid grid = NavGrid.Active;
            if (grid == null)
            {
                // 칸 지도가 없는 맵에서는 벽을 무시하고 곧장 간다.
                path.Clear();
                path.Add(Destination);
                IsUnreachable = false;
                return;
            }
            IsUnreachable = !GridPathfinder.TryFindPath(grid, transform.position, Destination, path);
        }

        void UpdateDirection()
        {
            Vector2 position = transform.position;
            HasArrived = (Destination - position).sqrMagnitude <= arriveDistance * arriveDistance;
            if (HasArrived || IsUnreachable)
            {
                DesiredDirection = Vector2.zero;
                return;
            }

            while (waypoint < path.Count - 1 && (path[waypoint] - position).sqrMagnitude <= waypointReach * waypointReach)
            {
                waypoint++;
            }

            // 목적지가 곧장 보이면 경로를 따르지 않고 바로 간다. 쫓을 때 움직임이 매끄러워진다.
            NavGrid grid = NavGrid.Active;
            Vector2 target = waypoint < path.Count ? path[waypoint] : Destination;
            if (grid == null || grid.IsWalkLineClear(position, Destination)) target = Destination;

            Vector2 toTarget = target - position;
            DesiredDirection = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;
        }

        void OnDrawGizmosSelected()
        {
            if (!HasDestination) return;
            // 디버그용: 지금 따라가는 경로를 Scene 창에 그린다.
            Gizmos.color = IsUnreachable ? Color.red : new Color(0.4f, 1f, 0.5f, 0.9f);
            Vector3 previous = transform.position;
            for (int i = waypoint; i < path.Count; i++)
            {
                Gizmos.DrawLine(previous, path[i]);
                previous = path[i];
            }
            Gizmos.DrawWireSphere(Destination, 0.2f);
        }
    }
}
