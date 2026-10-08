using UnityEngine;

namespace DustScrapper.Navigation
{
    /// <summary>
    /// 길찾기용 칸 지도. 시작할 때 맵 영역을 작은 칸으로 나누고, 벽(장애물 레이어)에 닿는 칸을 "못 감"으로 표시한다.
    /// 벽에서 clearance만큼 떨어진 칸만 "갈 수 있음"이라 캐릭터가 벽 모서리에 걸리지 않는다.
    /// 맵마다 하나 둔다. 적은 Active로 찾아 쓴다(적은 프리팹이라 씬 물체를 직접 연결할 수 없어서).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class NavGrid : MonoBehaviour
    {
        [Tooltip("지도 영역의 크기 (칸). 이 물체의 위치가 영역의 가운데다.")]
        [SerializeField] Vector2 size = new(48f, 32f);

        [Tooltip("길찾기 칸 하나의 크기 (칸). 작을수록 좁은 틈도 찾지만 계산이 늘어난다.")]
        [SerializeField, Min(0.1f)] float cellSize = 0.5f;

        [Tooltip("벽으로 볼 레이어")]
        [SerializeField] LayerMask obstacleMask;

        [Tooltip("벽에서 이만큼(칸) 떨어져야 다닐 수 있다. 다니는 캐릭터의 반지름보다 조금 크게 둔다.")]
        [SerializeField, Min(0f)] float clearance = 0.4f;

        bool[] walkable;
        Vector2 origin;

        /// <summary>지금 맵의 칸 지도. 없으면 null(적은 벽을 무시하고 곧장 간다).</summary>
        public static NavGrid Active { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize => cellSize;
        public float Clearance => clearance;
        public LayerMask ObstacleMask => obstacleMask;

        void Awake()
        {
            Build();
        }

        void OnEnable() => Active = this;

        void OnDisable()
        {
            if (Active == this) Active = null;
        }

        /// <summary>벽 위치를 다시 읽어 칸 지도를 만든다. 문이 열리고 닫히는 등 벽이 바뀌면 다시 부른다.</summary>
        public void Build()
        {
            Width = Mathf.Max(1, Mathf.CeilToInt(size.x / cellSize));
            Height = Mathf.Max(1, Mathf.CeilToInt(size.y / cellSize));
            origin = (Vector2)transform.position - new Vector2(Width, Height) * (cellSize * 0.5f);
            walkable = new bool[Width * Height];

            Vector2 probe = Vector2.one * (cellSize + clearance * 2f);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2 center = CellToWorld(new Vector2Int(x, y));
                    walkable[Index(x, y)] = Physics2D.OverlapBox(center, probe, 0f, obstacleMask) == null;
                }
            }
        }

        public Vector2Int WorldToCell(Vector2 world)
        {
            Vector2 local = (world - origin) / cellSize;
            return new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
        }

        public Vector2 CellToWorld(Vector2Int cell) => origin + ((Vector2)cell + Vector2.one * 0.5f) * cellSize;

        public bool IsInside(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;

        public bool IsWalkable(Vector2Int cell) => IsInside(cell) && walkable[Index(cell.x, cell.y)];

        public int Index(int x, int y) => y * Width + x;

        /// <summary>주어진 칸이 막혀 있으면 가장 가까운 갈 수 있는 칸을 찾는다(벽에 붙은 목적지 등).</summary>
        public bool TryFindNearestWalkable(Vector2Int cell, int maxRadius, out Vector2Int result)
        {
            result = cell;
            if (IsWalkable(cell)) return true;

            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                        var candidate = new Vector2Int(cell.x + dx, cell.y + dy);
                        if (!IsWalkable(candidate)) continue;
                        result = candidate;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>center에서 radius(칸) 안의 갈 수 있는 지점을 무작위로 하나 고른다. 몇 번 시도해도 없으면 false.</summary>
        public bool TryGetRandomWalkablePoint(Vector2 center, float radius, out Vector2 point)
        {
            const int Attempts = 12;
            for (int i = 0; i < Attempts; i++)
            {
                Vector2 candidate = center + Random.insideUnitCircle * radius;
                if (!IsWalkable(WorldToCell(candidate))) continue;
                point = candidate;
                return true;
            }
            point = center;
            return false;
        }

        /// <summary>캐릭터가 두 점 사이를 벽에 걸리지 않고 곧장 걸어갈 수 있는가.</summary>
        public bool IsWalkLineClear(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float distance = delta.magnitude;
            if (distance < 0.001f) return true;
            return Physics2D.CircleCast(from, clearance, delta / distance, distance, obstacleMask).collider == null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position, size);
            if (walkable == null) return;

            // 디버그용: 못 가는 칸을 Scene 창에 붉게 표시한다.
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
            Vector3 cube = Vector3.one * (cellSize * 0.9f);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (walkable[Index(x, y)]) continue;
                    Gizmos.DrawCube(CellToWorld(new Vector2Int(x, y)), cube);
                }
            }
        }
    }
}
