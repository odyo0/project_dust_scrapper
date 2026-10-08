using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Navigation
{
    /// <summary>
    /// NavGrid 위에서 두 점 사이의 길을 찾는다(A* 탐색, 8방향).
    /// 찾은 칸 경로를 그대로 쓰면 계단처럼 꺾이므로, 곧장 걸어갈 수 있는 구간은 건너뛰어 꺾이는 점만 남긴다.
    /// </summary>
    public static class GridPathfinder
    {
        const float StraightCost = 1f;
        const float DiagonalCost = 1.41421356f;
        /// <summary>목적지가 벽 안이면 이 칸 수 안에서 가장 가까운 갈 수 있는 칸으로 바꾼다.</summary>
        const int NearestSearchRadius = 4;

        static readonly Vector2Int[] Directions =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        // 탐색할 때마다 배열을 새로 만들지 않도록 재사용한다. visitStamp가 이번 탐색 번호와 같아야 유효한 값이다.
        static float[] costSoFar;
        static int[] cameFrom;
        static int[] visitStamp;
        static bool[] closed;
        static int currentStamp;
        static readonly MinHeap Open = new();
        static readonly List<Vector2> RawPath = new();

        /// <summary>
        /// from에서 to까지의 길을 찾아 지나갈 점들을 result에 담는다(출발점은 빼고, 마지막 점은 목적지).
        /// 길이 없으면 false.
        /// </summary>
        public static bool TryFindPath(NavGrid grid, Vector2 from, Vector2 to, List<Vector2> result)
        {
            result.Clear();
            if (!grid.TryFindNearestWalkable(grid.WorldToCell(from), NearestSearchRadius, out Vector2Int start)) return false;
            if (!grid.TryFindNearestWalkable(grid.WorldToCell(to), NearestSearchRadius, out Vector2Int goal)) return false;

            if (!Search(grid, start, goal)) return false;

            RawPath.Clear();
            for (int index = grid.Index(goal.x, goal.y); index != -1; index = cameFrom[index])
            {
                RawPath.Add(grid.CellToWorld(new Vector2Int(index % grid.Width, index / grid.Width)));
            }
            RawPath.Reverse();

            // 목적지 칸이 원래 목적지 그대로라면 칸 가운데가 아니라 정확한 지점으로 간다.
            if (grid.WorldToCell(to) == goal) RawPath[^1] = to;

            Simplify(grid, from, result);
            return true;
        }

        static bool Search(NavGrid grid, Vector2Int start, Vector2Int goal)
        {
            EnsureCapacity(grid.Width * grid.Height);
            currentStamp++;
            Open.Clear();

            int startIndex = grid.Index(start.x, start.y);
            int goalIndex = grid.Index(goal.x, goal.y);
            Visit(startIndex, 0f, -1);
            Open.Push(startIndex, Heuristic(start, goal));

            while (Open.Count > 0)
            {
                int current = Open.Pop();
                if (closed[current]) continue;
                closed[current] = true;
                if (current == goalIndex) return true;

                var cell = new Vector2Int(current % grid.Width, current / grid.Width);
                for (int i = 0; i < Directions.Length; i++)
                {
                    Vector2Int step = Directions[i];
                    var next = cell + step;
                    if (!grid.IsWalkable(next)) continue;

                    bool diagonal = step.x != 0 && step.y != 0;
                    // 대각선은 양옆 칸이 모두 열려 있을 때만 간다. 벽 모서리를 비스듬히 뚫고 지나가지 않게.
                    if (diagonal && (!grid.IsWalkable(new Vector2Int(cell.x + step.x, cell.y)) ||
                                     !grid.IsWalkable(new Vector2Int(cell.x, cell.y + step.y)))) continue;

                    int nextIndex = grid.Index(next.x, next.y);
                    if (IsVisited(nextIndex) && closed[nextIndex]) continue;

                    float cost = costSoFar[current] + (diagonal ? DiagonalCost : StraightCost);
                    if (IsVisited(nextIndex) && cost >= costSoFar[nextIndex]) continue;

                    Visit(nextIndex, cost, current);
                    Open.Push(nextIndex, cost + Heuristic(next, goal));
                }
            }
            return false;
        }

        /// <summary>곧장 걸어갈 수 있는 점들은 건너뛰고, 방향이 꺾이는 점만 남긴다.</summary>
        static void Simplify(NavGrid grid, Vector2 from, List<Vector2> result)
        {
            Vector2 anchor = from;
            int i = 0;
            while (i < RawPath.Count)
            {
                // 앞에서부터 막힐 때까지 늘려 간다. 뒤에서부터 찾는 것보다 덜 매끈하지만 검사 횟수가 경로 길이만큼으로 끝난다.
                int farthest = i;
                for (int j = i + 1; j < RawPath.Count; j++)
                {
                    if (!grid.IsWalkLineClear(anchor, RawPath[j])) break;
                    farthest = j;
                }
                result.Add(RawPath[farthest]);
                anchor = RawPath[farthest];
                i = farthest + 1;
            }
        }

        static float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return StraightCost * (dx + dy) + (DiagonalCost - 2f * StraightCost) * Mathf.Min(dx, dy);
        }

        static void EnsureCapacity(int count)
        {
            if (costSoFar != null && costSoFar.Length >= count) return;
            costSoFar = new float[count];
            cameFrom = new int[count];
            visitStamp = new int[count];
            closed = new bool[count];
            currentStamp = 0;
        }

        static bool IsVisited(int index) => visitStamp[index] == currentStamp;

        static void Visit(int index, float cost, int parent)
        {
            if (!IsVisited(index)) closed[index] = false;
            visitStamp[index] = currentStamp;
            costSoFar[index] = cost;
            cameFrom[index] = parent;
        }

        /// <summary>가장 작은 값부터 꺼내는 대기열. A*가 "가장 가망 있는 칸"을 먼저 보게 한다.</summary>
        sealed class MinHeap
        {
            readonly List<(int index, float priority)> items = new();

            public int Count => items.Count;

            public void Clear() => items.Clear();

            public void Push(int index, float priority)
            {
                items.Add((index, priority));
                int child = items.Count - 1;
                while (child > 0)
                {
                    int parent = (child - 1) / 2;
                    if (items[parent].priority <= items[child].priority) break;
                    (items[parent], items[child]) = (items[child], items[parent]);
                    child = parent;
                }
            }

            public int Pop()
            {
                int top = items[0].index;
                int last = items.Count - 1;
                items[0] = items[last];
                items.RemoveAt(last);

                int parent = 0;
                while (true)
                {
                    int left = parent * 2 + 1;
                    if (left >= items.Count) break;
                    int right = left + 1;
                    int smallest = right < items.Count && items[right].priority < items[left].priority ? right : left;
                    if (items[parent].priority <= items[smallest].priority) break;
                    (items[parent], items[smallest]) = (items[smallest], items[parent]);
                    parent = smallest;
                }
                return top;
            }
        }
    }
}
