using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DustScrapper.Combat;
using DustScrapper.Map;
using DustScrapper.Navigation;
using DustScrapper.Structures;
using DustScrapper.Zones;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DustScrapper.EditorTools
{
    /// <summary>
    /// 건물 배치(Aseprite에서 꺼낸 Art/Buildings/Layouts/*.json)를 건물 프리팹으로 만든다. 편집기 전용.
    /// 프리팹 = 바닥 · 벽 · 가구(키 큰 것 / 낮은 것) · 소품 타일맵 + 문 · 유리창 물체 + 구역.
    /// 타일은 테마 타일 묶음(TileKitImporter가 만든 것)을 가리키고, 이어지는 것은 룰 타일로 칠한다 —
    /// 그래서 프리팹을 Unity에서 고칠 때도 칠하기만 하면 모양이 맞춰지고, 타일 그림을 바꾸면 모든 건물이 같이 바뀐다.
    /// 다시 돌리면 같은 이름의 프리팹을 덮어쓴다(Unity에서 고친 배치는 사라진다).
    /// </summary>
    public static class BuildingImporter
    {
        const string LayoutDir = "Art/Buildings/Layouts";      // 프로젝트 폴더 기준, Assets 밖
        const string PrefabDir = "Assets/Prefabs/Buildings";
        const string Theme = "Ruin";
        const string TileRoot = "Assets/Art/Tiles/" + Theme;

        // 그리기 순서: 바닥 < 가구 < 소품 < 벽 < 플레이어(1~)
        const int FloorOrder = -10, FurnitureOrder = -8, PropsOrder = -7, WallOrder = -5, DoorOrder = -4;

        // 시야까지 막는 키 큰 가구 (나머지 가구는 몸·총알만 막는 엄폐물)
        static readonly HashSet<string> TallKinds = new() { "shelf", "bookshelf", "cooler", "lockers", "partition", "rack", "fridge", "purifier" };

        static readonly Dictionary<string, string> DisplayNames = new()
        {
            ["Building_01"] = "학원", ["Building_02"] = "카페", ["Building_03"] = "편의점", ["Building_04"] = "옷가게",
            ["Building_05"] = "도서관", ["Building_06"] = "마트", ["Building_07"] = "PC방", ["Building_08"] = "식당",
        };

        static readonly Regex ConnectedName = new(@"^(.+)_[01]{4}$");
        static readonly Regex RotatedName = new(@"^(.+)_r[0-3]$");

        [Serializable] class Layout { public string name; public int cols; public int rows; public LayerCells[] layers; }
        [Serializable] class LayerCells { public string name; public string[] cells; }

        [MenuItem("Dust Scrapper/Buildings/Import Buildings")]
        static void ImportAllMenu()
        {
            if (!EditorUtility.DisplayDialog("건물 가져오기", $"{LayoutDir}의 배치로 {PrefabDir}의 건물 프리팹을 다시 만든다.\nUnity에서 고친 배치는 덮어쓴다.", "가져오기", "취소")) return;
            ImportAll();
        }

        public static void ImportAll()
        {
            var files = Directory.GetFiles(LayoutDir, "*.json").OrderBy(f => f).ToArray();
            foreach (string file in files) Import(file);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildingImporter] 건물 {files.Length}개 → {PrefabDir}");
        }

        public static void Import(string jsonPath)
        {
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(jsonPath));
            var cells = layout.layers.ToDictionary(l => l.name, l => l.cells);
            var root = new GameObject(layout.name, typeof(Grid));
            try
            {
                var floor = NewMap(root.transform, "Floor", FloorOrder);
                var walls = NewMap(root.transform, "Walls", WallOrder, "Walls");
                var tall = NewMap(root.transform, "FurnitureTall", FurnitureOrder, "Walls");
                var low = NewMap(root.transform, "FurnitureLow", FurnitureOrder, "Cover");
                var props = NewMap(root.transform, "Props", PropsOrder);
                var doorsRoot = NewChild(root.transform, "Doors");
                var windowsRoot = NewChild(root.transform, "Windows");
                var openings = new List<Component>();

                for (int r = 0; r < layout.rows; r++)
                {
                    for (int c = 0; c < layout.cols; c++)
                    {
                        int i = r * layout.cols + c;
                        var cell = new Vector3Int(c, layout.rows - 1 - r, 0);   // 배치는 위쪽 줄부터, Unity는 아래가 0
                        bool edge = r == 0 || c == 0 || r == layout.rows - 1 || c == layout.cols - 1;

                        Paint(floor, cell, FloorTile(cells["Floor"][i]));
                        Paint(props, cell, PlainTile("Props", cells["Props"][i]));

                        string f = cells["Furniture"][i];
                        if (f != "") Paint(IsTall(f) ? tall : low, cell, FurnitureTile(f));

                        string w = cells["Wall"][i];
                        if (w == "") continue;
                        if (w.StartsWith("wall_")) Paint(walls, cell, Rule("wall"));
                        else if (w.StartsWith("window_"))
                        {
                            Paint(walls, cell, PlainTile("Wall", w));
                            if (!w.EndsWith("_broken"))
                            {
                                var window = MakeWindow(windowsRoot, walls, cell, w);
                                if (edge) openings.Add(window);
                            }
                        }
                        else if (w.StartsWith("door_"))
                        {
                            var door = MakeDoor(doorsRoot, cell, w);
                            if (edge) openings.Add(door);
                        }
                    }
                }

                MakeZone(root.transform, layout, openings);
                Directory.CreateDirectory(PrefabDir);
                PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{layout.name}.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static Tilemap NewMap(Transform parent, string name, int order, string physicsLayer = null)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            if (physicsLayer != null)
            {
                go.layer = LayerMask.NameToLayer(physicsLayer);
                // 칸마다 충돌체를 하나로 합친다(Polygons: 안쪽까지 채워져 있어야 길찾기의 칸 검사가 벽 속에서도 잡힌다)
                var body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
                go.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
            }
            return go.GetComponent<Tilemap>();
        }

        static Transform NewChild(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static void Paint(Tilemap map, Vector3Int cell, TileBase tile)
        {
            if (tile != null) map.SetTile(cell, tile);
        }

        static bool IsTall(string name) => TallKinds.Contains(BaseName(name));

        static string BaseName(string name)
        {
            var m = ConnectedName.Match(name);
            if (m.Success) return m.Groups[1].Value;
            m = RotatedName.Match(name);
            return m.Success ? m.Groups[1].Value : name;
        }

        static TileBase FloorTile(string name) => PlainTile("Floor", name);

        // 이어지는 가구는 룰 타일로 — Unity에서 이어 칠해도 모양이 맞게
        static TileBase FurnitureTile(string name) =>
            ConnectedName.IsMatch(name) ? Rule(BaseName(name)) : PlainTile("Furniture", name);

        static TileBase PlainTile(string layer, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileRoot}/Tiles/{layer}/{name}.asset");
            if (tile == null) Debug.LogError($"[BuildingImporter] 타일 없음: {layer}/{name}");
            return tile;
        }

        static TileBase Rule(string kind)
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileRoot}/RuleTiles/{kind}.asset");
            if (tile == null) Debug.LogError($"[BuildingImporter] 룰 타일 없음: {kind}");
            return tile;
        }

        static Vector3 Center(Vector3Int cell) => new(cell.x + 0.5f, cell.y + 0.5f, 0f);

        /// <summary>
        /// 한 칸짜리 문. Outskirts의 문과 같은 짜임: Door(+NavPassable) → Hinge(DoorLeaf, Kinematic 몸) → Panel(Walls 레이어, 그림·충돌체).
        /// 세로 문은 가로 문을 90도 돌린다. 나무문은 차면 날아가는 Door_Wood, 유리문은 버티는 Door_Sturdy(AI 임시).
        /// </summary>
        static Door MakeDoor(Transform parent, Vector3Int cell, string tileName)
        {
            bool vertical = tileName.EndsWith("_v");
            bool glass = tileName.Contains("glass");
            var go = new GameObject($"Door_{cell.x}_{cell.y}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Center(cell);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, vertical ? 90f : 0f);
            go.AddComponent<NavPassable>();
            var door = go.AddComponent<Door>();

            var hinge = new GameObject("Hinge");
            hinge.transform.SetParent(go.transform, false);
            hinge.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
            hinge.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var leaf = hinge.AddComponent<DoorLeaf>();

            var panel = new GameObject("Panel") { layer = LayerMask.NameToLayer("Walls") };
            panel.transform.SetParent(hinge.transform, false);
            panel.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            var look = panel.AddComponent<SpriteRenderer>();
            look.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TileRoot}/Sprites/Wall/door_{(glass ? "glass" : "wood")}_h.png");
            look.sortingOrder = DoorOrder;
            panel.AddComponent<BoxCollider2D>().size = new Vector2(0.95f, 0.24f);

            var so = new SerializedObject(door);
            so.FindProperty("displayName").stringValue = glass ? "유리문" : "문";
            so.FindProperty("data").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DoorData>($"Assets/Data/Structures/{(glass ? "Door_Sturdy" : "Door_Wood")}.asset");
            var leaves = so.FindProperty("leaves");
            leaves.arraySize = 1;
            leaves.GetArrayElementAtIndex(0).objectReferenceValue = leaf;
            so.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        /// <summary>
        /// 유리창. 그림은 벽 타일맵의 창 타일, 막기·깨지기는 이 물체(Glass 레이어, 체력 1). 깨지면 WindowTileLook이 깨진 창 타일로 바꾼다.
        /// </summary>
        static Window MakeWindow(Transform parent, Tilemap walls, Vector3Int cell, string tileName)
        {
            var go = new GameObject($"Window_{cell.x}_{cell.y}") { layer = LayerMask.NameToLayer("Glass") };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Center(cell);
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            var health = go.AddComponent<Health>();
            var hs = new SerializedObject(health);
            hs.FindProperty("maxHealth").floatValue = 1f;
            hs.ApplyModifiedPropertiesWithoutUndo();
            var window = go.AddComponent<Window>();
            go.AddComponent<WindowTileLook>().Setup(walls, cell, PlainTile("Wall", tileName + "_broken"));
            return window;
        }

        // 건물 하나 = 구역 하나. 바깥벽의 문·창이 열리거나 깨지면 소리가 더 샌다(MapZone).
        static void MakeZone(Transform root, Layout layout, List<Component> openings)
        {
            var go = NewChild(root, "Zone").gameObject;
            go.transform.localPosition = new Vector3(layout.cols * 0.5f, layout.rows * 0.5f, 0f);
            var zone = go.AddComponent<MapZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("displayName").stringValue = DisplayNames.TryGetValue(layout.name, out string n) ? n : "상가";
            so.FindProperty("size").vector2Value = new Vector2(layout.cols, layout.rows);
            var list = so.FindProperty("openings");
            list.arraySize = openings.Count;
            for (int i = 0; i < openings.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = openings[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
