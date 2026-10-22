using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DustScrapper.Map;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DustScrapper.EditorTools
{
    /// <summary>
    /// 테마 타일 묶음(Aseprite에서 꺼낸 PNG 한 장 = 타일 하나)을 Unity 타일로 만든다. 편집기 전용.
    /// 만드는 것: 그림 가져오기 설정(32 PPU, 흐림 없음), 타일 에셋, 이어지는 것(벽·가구 종류)마다 룰 타일, 타일 팔레트.
    /// 이미 있으면 같은 에셋을 고쳐 쓰므로, 그림을 고친 뒤 다시 돌려도 건물들이 가리키는 타일은 그대로다.
    /// </summary>
    public static class TileKitImporter
    {
        const string ThemesRoot = "Assets/Art/Tiles";
        const int PixelsPerUnit = 32;
        const int PaletteColumns = 16;

        [MenuItem("Dust Scrapper/Tiles/Import Ruin Tiles")]
        static void ImportRuin() => Import("Ruin");

        [Serializable] class Order { public LayerOrder[] layers; }
        [Serializable] class LayerOrder { public string name; public string[] tiles; }

        // 이름 끝 네 자리 = 북·동·남·서 이웃이 같은 것인가 (예: counter_0101 = 동·서로 이어진 가운데 칸)
        static readonly Regex ConnectedName = new(@"^(.+)_([01])([01])([01])([01])$");
        static readonly Vector3Int[] Sides = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

        public static void Import(string theme)
        {
            string root = $"{ThemesRoot}/{theme}";
            var order = JsonUtility.FromJson<Order>(File.ReadAllText($"{root}/tile_order.json"));

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var layer in order.layers)
                    foreach (string name in layer.tiles)
                        SetUpSprite($"{root}/Sprites/{layer.name}/{name}.png");
            }
            finally { AssetDatabase.StopAssetEditing(); }

            var tiles = new Dictionary<string, Dictionary<string, Tile>>();
            foreach (var layer in order.layers)
            {
                var byName = tiles[layer.name] = new Dictionary<string, Tile>();
                EnsureFolder($"{root}/Tiles/{layer.name}");
                foreach (string name in layer.tiles)
                {
                    var tile = LoadOrCreate<Tile>($"{root}/Tiles/{layer.name}/{name}.asset");
                    tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{root}/Sprites/{layer.name}/{name}.png");
                    tile.colliderType = ColliderFor(layer.name, name);
                    EditorUtility.SetDirty(tile);
                    byName[name] = tile;
                }
            }

            var ruleTiles = new List<TileBase>();
            foreach (var layer in order.layers)
            {
                var friends = layer.name == "Wall"
                    ? tiles[layer.name].Where(p => p.Key.StartsWith("window_")).Select(p => (TileBase)p.Value).ToList()
                    : new List<TileBase>();
                foreach (var kind in ConnectedKinds(layer.tiles))
                    ruleTiles.Add(BuildRuleTile(root, layer.name, kind.Key, kind.Value, tiles[layer.name], friends));
            }

            BuildPalette(root, theme, order, tiles, ruleTiles);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TileKitImporter] {theme}: 타일 {tiles.Sum(t => t.Value.Count)}개, 룰 타일 {ruleTiles.Count}개, 팔레트 {root}/Palette/{theme}.prefab");
        }

        // 도트 그림을 흐리지 않고 32픽셀 = 1칸으로 가져온다
        static void SetUpSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) throw new FileNotFoundException(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;         // 칸 사이 틈이 생기지 않게
            settings.spriteGenerateFallbackPhysicsShape = true;        // 가구 충돌 모양을 그림 윤곽으로
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        /// <summary>
        /// 무엇이 몸·총알을 막는가. 실제로 시야까지 막을지는 타일이 아니라 타일맵의 레이어(Walls/Cover)가 정한다(건물 가져오기에서).
        /// 벽은 칸 전체, 가구는 그림 윤곽. 바닥·소품·의자는 막지 않는다(의자마다 막히면 걷기가 답답하다).
        /// 창·문 칸은 타일이 아니라 따로 놓는 Window·Door 물체가 막는다.
        /// </summary>
        static Tile.ColliderType ColliderFor(string layer, string name) => layer switch
        {
            "Wall" => name.StartsWith("wall_") ? Tile.ColliderType.Grid : Tile.ColliderType.None,
            "Furniture" => name.StartsWith("chair_") || name == "stool" ? Tile.ColliderType.None : Tile.ColliderType.Sprite,
            _ => Tile.ColliderType.None,
        };

        static Dictionary<string, List<string>> ConnectedKinds(IEnumerable<string> names)
        {
            var kinds = new Dictionary<string, List<string>>();
            foreach (string name in names)
            {
                var m = ConnectedName.Match(name);
                if (!m.Success) continue;
                if (!kinds.TryGetValue(m.Groups[1].Value, out var list)) kinds[m.Groups[1].Value] = list = new List<string>();
                list.Add(name);
            }
            return kinds.Where(k => k.Value.Count == 16).ToDictionary(k => k.Key, k => k.Value);
        }

        static TileBase BuildRuleTile(string root, string layer, string kind, List<string> variants, Dictionary<string, Tile> tiles, List<TileBase> friends)
        {
            EnsureFolder($"{root}/RuleTiles");
            var rule = LoadOrCreate<ConnectedRuleTile>($"{root}/RuleTiles/{kind}.asset");
            rule.friends = friends;
            rule.m_TilingRules = new List<RuleTile.TilingRule>();
            foreach (string name in variants)
            {
                var m = ConnectedName.Match(name);
                var r = new RuleTile.TilingRule
                {
                    m_NeighborPositions = Sides.ToList(),
                    m_Neighbors = Enumerable.Range(0, 4).Select(i => m.Groups[2 + i].Value == "1"
                        ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis).ToList(),
                    m_Sprites = new[] { tiles[name].sprite },
                    m_ColliderType = tiles[name].colliderType,
                    m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
                    m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
                };
                rule.m_TilingRules.Add(r);
            }
            var lone = tiles[$"{kind}_0000"];
            rule.m_DefaultSprite = lone.sprite;
            rule.m_DefaultColliderType = lone.colliderType;
            EditorUtility.SetDirty(rule);
            return rule;
        }

        // 팔레트: 맨 위 줄 = 룰 타일(칠하면 알아서 이어짐), 그 아래로 바닥 · 창·문 · 단품 가구 · 소품
        static void BuildPalette(string root, string theme, Order order, Dictionary<string, Dictionary<string, Tile>> tiles, List<TileBase> ruleTiles)
        {
            string folder = $"{root}/Palette";
            string path = $"{folder}/{theme}.prefab";
            EnsureFolder(folder);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                GridPaletteUtility.CreateNewPalette(folder, theme, GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Manual, Vector3.one, GridLayout.CellSwizzle.XYZ);

            var sections = new List<List<TileBase>> { ruleTiles };
            foreach (var layer in order.layers)
            {
                var singles = layer.tiles.Where(n => !ConnectedName.IsMatch(n)).Select(n => (TileBase)tiles[layer.name][n]).ToList();
                if (singles.Count > 0) sections.Add(singles);
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var map = contents.GetComponentInChildren<Tilemap>();
                map.ClearAllTiles();
                int y = 0;
                foreach (var section in sections)
                {
                    for (int i = 0; i < section.Count; i++)
                        map.SetTile(new Vector3Int(i % PaletteColumns, y - i / PaletteColumns, 0), section[i]);
                    y -= (section.Count + PaletteColumns - 1) / PaletteColumns + 1;
                }
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
