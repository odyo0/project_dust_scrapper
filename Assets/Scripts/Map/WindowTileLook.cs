using DustScrapper.Structures;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DustScrapper.Map
{
    /// <summary>
    /// 타일맵 건물의 유리창 그림. 창 그림은 벽 타일맵의 한 칸에 있고(벽 룰 타일이 창과 이어지게),
    /// 막기·깨지기는 같은 칸에 놓인 Window 물체가 맡는다. Window가 깨졌다고 알리면 그 칸을 깨진 창 타일로 바꾼다.
    /// </summary>
    [RequireComponent(typeof(Window))]
    [DisallowMultipleComponent]
    public sealed class WindowTileLook : MonoBehaviour
    {
        [SerializeField] Tilemap tilemap;
        [SerializeField] Vector3Int cell;
        [SerializeField] TileBase brokenTile;

        Window window;

        public void Setup(Tilemap map, Vector3Int at, TileBase broken)
        {
            tilemap = map; cell = at; brokenTile = broken;
        }

        void Awake() => window = GetComponent<Window>();
        void OnEnable() => window.Shattered += ShowBroken;
        void OnDisable() => window.Shattered -= ShowBroken;

        void ShowBroken()
        {
            if (tilemap != null && brokenTile != null) tilemap.SetTile(cell, brokenTile);
        }
    }
}
