using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.Rooms;
using UnityEngine;

namespace HullBreach.Game
{
    public static class RoomTemplateFactory
    {
        private const float W = 26f;
        private const float H = 14f;
        private const float T = 1f;
        private const float Gap = 4f;
        private const float Corridor = 3f;

        private struct Theme
        {
            public Color Floor;
            public Color Line;
            public Color Wall;
            public Color Accent;
        }

        private struct Block
        {
            public float X, Y, Width, Height;
            public Block(float x, float y, float w, float h) { X = x; Y = y; Width = w; Height = h; }
        }

        private static readonly Block[][] NormalPatterns =
        {
            new[] { new Block(-3f, 3.5f, 1.6f, 1.6f), new Block(-3f, -3.5f, 1.6f, 1.6f), new Block(3f, 3.5f, 1.6f, 1.6f), new Block(3f, -3.5f, 1.6f, 1.6f) },
            new[] { new Block(0f, 0f, 1.4f, 5f), new Block(-6f, 4.5f, 2.5f, 1.2f), new Block(6f, -4.5f, 2.5f, 1.2f) },
            new[] { new Block(-5f, -4.5f, 2.5f, 1.2f), new Block(5f, 4.5f, 2.5f, 1.2f), new Block(0f, 0f, 2.2f, 2.2f) },
            new[] { new Block(-4f, 0f, 1.5f, 1.5f), new Block(0f, 2.5f, 1.5f, 1.5f), new Block(0f, -2.5f, 1.5f, 1.5f), new Block(4f, 0f, 1.5f, 1.5f), new Block(7.5f, 5f, 1.5f, 1.5f), new Block(7.5f, -5f, 1.5f, 1.5f) },
            new[] { new Block(-2f, 4.5f, 7f, 1.2f), new Block(2f, -4.5f, 7f, 1.2f) },
            new[] { new Block(-6f, -3f, 1.2f, 1.2f), new Block(-6f, 3f, 1.2f, 1.2f), new Block(-1f, 0f, 1.2f, 1.2f), new Block(4f, -3f, 1.2f, 1.2f), new Block(4f, 3f, 1.2f, 1.2f), new Block(8f, 0f, 1.2f, 1.2f) },
            new[] { new Block(-2f, 2.8f, 1.2f, 3.4f), new Block(2f, -2.8f, 1.2f, 3.4f), new Block(6.5f, 0f, 1.2f, 1.2f) },
            new[] { new Block(0f, 4.5f, 10f, 1f), new Block(0f, -4.5f, 10f, 1f) },
            new[] { new Block(0f, 3f, 1.4f, 1.4f), new Block(0f, -3f, 1.4f, 1.4f), new Block(3f, 0f, 1.4f, 1.4f), new Block(-3f, 0f, 1.4f, 1.4f), new Block(7f, 3.5f, 1.4f, 1.4f), new Block(7f, -3.5f, 1.4f, 1.4f) },
        };

        private static readonly Block[] ReactorPattern =
        {
            new Block(-3f, 3.5f, 1.6f, 1.6f), new Block(-3f, -3.5f, 1.6f, 1.6f), new Block(2f, 5f, 1.6f, 1.4f), new Block(2f, -5f, 1.6f, 1.4f)
        };

        public static RoomTemplateSet[] CreateTemplateSets(Transform templatesRoot)
        {
            Theme engine = new Theme
            {
                Floor = new Color(0.17f, 0.12f, 0.10f, 1f), Line = new Color(0.28f, 0.19f, 0.14f, 1f),
                Wall = new Color(0.42f, 0.26f, 0.20f, 1f), Accent = new Color(1f, 0.54f, 0.24f, 1f)
            };
            Theme cargo = new Theme
            {
                Floor = new Color(0.12f, 0.16f, 0.12f, 1f), Line = new Color(0.19f, 0.26f, 0.17f, 1f),
                Wall = new Color(0.32f, 0.40f, 0.28f, 1f), Accent = new Color(1f, 0.77f, 0.24f, 1f)
            };
            Theme bridge = new Theme
            {
                Floor = new Color(0.08f, 0.12f, 0.22f, 1f), Line = new Color(0.14f, 0.20f, 0.36f, 1f),
                Wall = new Color(0.24f, 0.32f, 0.55f, 1f), Accent = new Color(0.49f, 0.55f, 1f, 1f)
            };

            return new[]
            {
                CreateSet(templatesRoot, EntryPointType.EngineRoom, engine, "Engine", 0),
                CreateSet(templatesRoot, EntryPointType.CargoHold, cargo, "Cargo", 3),
                CreateSet(templatesRoot, EntryPointType.Bridge, bridge, "Bridge", 6),
            };
        }

        private static RoomTemplateSet CreateSet(Transform root, EntryPointType entry, Theme theme, string label, int patternOffset)
        {
            RoomTemplateSet set = new RoomTemplateSet();
            set.entryPoint = entry;
            set.normalRooms = new RoomController[3];
            for (int i = 0; i < 3; i++)
            {
                set.normalRooms[i] = CreateRoom(root, label + "_Room" + (i + 1), RoomRole.Normal, theme,
                    NormalPatterns[patternOffset + i], patternOffset * 31 + i * 7 + 1);
            }
            set.reactorRooms = new[]
            {
                CreateRoom(root, label + "_Reactor", RoomRole.Reactor, theme, ReactorPattern, patternOffset * 17 + 5)
            };
            return set;
        }

        private static RoomController CreateRoom(Transform root, string name, RoomRole role, Theme theme, Block[] blocks, int seed)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(root, false);
            RoomController room = go.AddComponent<RoomController>();

            SpriteRenderer floor = AddSprite(go.transform, "Floor", RuntimeSprites.GridTile(theme.Floor, theme.Line), Color.white,
                Vector2.zero, Vector2.one, -10);
            floor.drawMode = SpriteDrawMode.Tiled;
            floor.size = new Vector2(W, H);

            AddWall(go.transform, "WallTop", 0f, H * 0.5f + T * 0.5f, W + 2f * T, T, theme.Wall);
            AddWall(go.transform, "WallBottom", 0f, -H * 0.5f - T * 0.5f, W + 2f * T, T, theme.Wall);
            AddWall(go.transform, "WallLeft", -W * 0.5f - T * 0.5f, 0f, T, H, theme.Wall);

            GameObject[] doors;
            RoomExitTrigger exit = null;
            GameObject reactor = null;

            if (role == RoomRole.Normal)
            {
                float segH = H * 0.5f + T - Gap * 0.5f;
                float segY = Gap * 0.5f + segH * 0.5f;
                AddWall(go.transform, "WallRightUp", W * 0.5f + T * 0.5f, segY, T, segH, theme.Wall);
                AddWall(go.transform, "WallRightDown", W * 0.5f + T * 0.5f, -segY, T, segH, theme.Wall);

                GameObject door = AddSprite(go.transform, "Door", RuntimeSprites.Square, new Color(1f, 0.27f, 0.38f, 0.85f),
                    new Vector2(W * 0.5f + T * 0.5f, 0f), new Vector2(T, Gap), -3).gameObject;
                BoxCollider2D doorCol = door.AddComponent<BoxCollider2D>();
                doorCol.size = Vector2.one;
                doors = new[] { door };

                float corridorX = W * 0.5f + T + Corridor * 0.5f;
                AddSprite(go.transform, "CorridorFloor", RuntimeSprites.Square, theme.Floor * 0.8f,
                    new Vector2(corridorX, 0f), new Vector2(Corridor, Gap), -10);
                AddWall(go.transform, "CorridorUp", corridorX, Gap * 0.5f + T * 0.5f, Corridor, T, theme.Wall);
                AddWall(go.transform, "CorridorDown", corridorX, -Gap * 0.5f - T * 0.5f, Corridor, T, theme.Wall);
                AddWall(go.transform, "CorridorEnd", W * 0.5f + T + Corridor + T * 0.5f, 0f, T, Gap + 2f * T, theme.Wall);

                AddSprite(go.transform, "ExitGlow", RuntimeSprites.Square, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.22f),
                    new Vector2(corridorX + 0.4f, 0f), new Vector2(1.8f, Gap), -9);

                GameObject exitGo = new GameObject("ExitTrigger");
                exitGo.transform.SetParent(go.transform, false);
                exitGo.transform.localPosition = new Vector3(corridorX + 0.4f, 0f, 0f);
                BoxCollider2D exitCol = exitGo.AddComponent<BoxCollider2D>();
                exitCol.isTrigger = true;
                exitCol.size = new Vector2(1.6f, Gap - 0.4f);
                exit = exitGo.AddComponent<RoomExitTrigger>();
                exit.Configure(room);
            }
            else
            {
                AddWall(go.transform, "WallRight", W * 0.5f + T * 0.5f, 0f, T, H, theme.Wall);
                doors = new GameObject[0];

                GameObject reactorGo = new GameObject("Reactor");
                reactorGo.transform.SetParent(go.transform, false);
                reactorGo.transform.localPosition = new Vector3(9.5f, 0f, 0f);
                reactorGo.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
                SpriteRenderer core = reactorGo.AddComponent<SpriteRenderer>();
                core.sprite = RuntimeSprites.Circle;
                core.color = new Color(0.2f, 0.95f, 0.85f, 1f);
                core.sortingOrder = -2;
                GameObject haloGo = new GameObject("Halo");
                haloGo.transform.SetParent(reactorGo.transform, false);
                haloGo.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
                SpriteRenderer halo = haloGo.AddComponent<SpriteRenderer>();
                halo.sprite = RuntimeSprites.Ring;
                halo.color = new Color(0.2f, 0.95f, 0.85f, 0.7f);
                halo.sortingOrder = -2;
                ReactorVisual.Attach(reactorGo, core, halo);
                reactor = reactorGo;
            }

            Color blockColor = Color.Lerp(theme.Wall, Color.white, 0.12f);
            for (int i = 0; i < blocks.Length; i++)
                AddWall(go.transform, "Block" + i, blocks[i].X, blocks[i].Y, blocks[i].Width, blocks[i].Height, blockColor, -4);

            Vector2 playerSpawnPos = new Vector2(-W * 0.5f + 2.5f, 0f);
            Transform playerSpawn = AddMarker(go.transform, "PlayerSpawn", playerSpawnPos);

            Transform bossSpawn = null;
            if (role == RoomRole.Reactor) bossSpawn = AddMarker(go.transform, "BossSpawn", new Vector2(3.5f, 0f));

            List<Transform> enemySpawns = BuildEnemySpawns(go.transform, blocks, playerSpawnPos, role, seed);

            room.Configure(role, playerSpawn, enemySpawns.ToArray(), bossSpawn, doors, exit, reactor, new Vector2(W + 2f * T, H + 2f * T));
            return room;
        }

        private static List<Transform> BuildEnemySpawns(Transform parent, Block[] blocks, Vector2 playerSpawn, RoomRole role, int seed)
        {
            List<Vector2> candidates = new List<Vector2>();
            float[] xs = { -5f, -2f, 1f, 4f, 7f, 10f };
            float[] ys = { -5f, -2.5f, 0f, 2.5f, 5f };
            for (int xi = 0; xi < xs.Length; xi++)
            {
                for (int yi = 0; yi < ys.Length; yi++)
                {
                    Vector2 p = new Vector2(xs[xi], ys[yi]);
                    if (Vector2.Distance(p, playerSpawn) < 4f) continue;
                    if (p.x > 10.5f && Mathf.Abs(p.y) < 3f) continue;
                    if (role == RoomRole.Reactor && Vector2.Distance(p, new Vector2(9.5f, 0f)) < 3.2f) continue;
                    if (Overlaps(p, blocks, 1.3f)) continue;
                    candidates.Add(p);
                }
            }

            System.Random rng = new System.Random(seed);
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                Vector2 tmp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = tmp;
            }

            List<Transform> result = new List<Transform>();
            int count = Mathf.Min(8, candidates.Count);
            for (int i = 0; i < count; i++) result.Add(AddMarker(parent, "EnemySpawn" + i, candidates[i]));
            if (result.Count == 0) result.Add(AddMarker(parent, "EnemySpawn0", new Vector2(2f, 0f)));
            return result;
        }

        private static bool Overlaps(Vector2 p, Block[] blocks, float margin)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                if (Mathf.Abs(p.x - blocks[i].X) < blocks[i].Width * 0.5f + margin &&
                    Mathf.Abs(p.y - blocks[i].Y) < blocks[i].Height * 0.5f + margin) return true;
            }
            return false;
        }

        private static Transform AddMarker(Transform parent, string name, Vector2 position)
        {
            GameObject m = new GameObject(name);
            m.transform.SetParent(parent, false);
            m.transform.localPosition = new Vector3(position.x, position.y, 0f);
            return m.transform;
        }

        private static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 scale, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private static void AddWall(Transform parent, string name, float x, float y, float w, float h, Color color, int order = -5)
        {
            SpriteRenderer sr = AddSprite(parent, name, RuntimeSprites.Square, color, new Vector2(x, y), new Vector2(w, h), order);
            BoxCollider2D col = sr.gameObject.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }
    }
}
