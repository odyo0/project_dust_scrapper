using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Zones
{
    /// <summary>
    /// 맵 안의 구역(상점, 경찰서 등). 사각 영역 하나이고, 한 맵의 구역끼리는 겹치지 않게 놓는다.
    /// 지금 하는 일은 둘이다: 어떤 지점이 어느 구역 안인지 알려 주고(Find), 구역 경계를 넘는 소리를 줄인다(NoiseFactor).
    /// 구역에 딸린 적 팀·상자는 이 물체 아래에 넣어 묶어 둔다. 구역마다 다른 규칙(위험도, 이름 표시 등)이 생기면 여기에 붙인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapZone : MonoBehaviour
    {
        static readonly List<MapZone> all = new();

        [SerializeField] string displayName = "구역";

        [Tooltip("구역 영역의 크기 (칸). 이 물체의 위치가 영역의 가운데다.")]
        [SerializeField] Vector2 size = new(16f, 12f);

        [Tooltip("구역 경계를 넘나드는 소리가 들리는 거리에 곱하는 값. 0이면 안팎으로 전혀 새지 않고, 1이면 경계가 없는 것과 같다.")]
        [SerializeField, Range(0f, 1f)] float noiseLeak = 0.3f;

        public static IReadOnlyList<MapZone> All => all;

        public string DisplayName => displayName;

        public Rect Bounds => new((Vector2)transform.position - size * 0.5f, size);

        public bool Contains(Vector2 point) => Bounds.Contains(point);

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        /// <summary>그 지점이 들어 있는 구역. 어느 구역에도 없으면(구역 밖) null.</summary>
        public static MapZone Find(Vector2 point)
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Contains(point)) return all[i];
            }
            return null;
        }

        /// <summary>
        /// from에서 난 소리가 to에 닿을 때 들리는 거리에 곱할 값.
        /// 같은 구역 안이거나 둘 다 밖이면 1. 구역 경계를 넘을 때마다 그 구역의 noiseLeak을 곱한다
        /// (구역 안 → 밖, 밖 → 구역 안, 구역 → 다른 구역은 두 번).
        /// </summary>
        public static float NoiseFactor(Vector2 from, Vector2 to)
        {
            MapZone source = Find(from);
            MapZone listener = Find(to);
            if (source == listener) return 1f;

            float factor = 1f;
            if (source != null) factor *= source.noiseLeak;
            if (listener != null) factor *= listener.noiseLeak;
            return factor;
        }

        // 도메인 다시 불러오기를 끈 설정에서도 지난 실행의 구역 목록이 남지 않게 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll() => all.Clear();

        void OnDrawGizmos()
        {
            // 디버그용: Scene 창에 구역 영역을 그린다(플레이 화면에는 안 보인다).
            Rect bounds = Bounds;
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
