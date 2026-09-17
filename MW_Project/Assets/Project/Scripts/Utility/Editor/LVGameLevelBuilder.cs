#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Builds LV_Game as a terrain-backed open map with ruin fragments and a fortress HQ.
/// Menu: NOVA / Level / Build LV_Game
/// </summary>
public static class LVGameLevelBuilder
{
    const string ScenePath = "Assets/Project/Scenes/LV_Game.unity";
    const string MatRoot = "Levels/Materials/";
    const string TerrainFolder = "Assets/Project/Resources/Levels/Terrain";
    const string TerrainDataPath = TerrainFolder + "/TD_LV_Game.asset";
    const int WallLayer = 7;
    const float FloorY = -0.25f;
    const float FloorH = 0.5f;
    const float WallT = 0.7f;

    const float TerrainX = -180f;
    const float TerrainZ = -180f;
    const float TerrainW = 480f;
    const float TerrainL = 420f;
    const float TerrainH = 42f;
    const float TerrainPosY = -1.5f;
    const int HeightRes = 257;
    const int AlphaRes = 256;

    enum Side { N, S, E, W }

    struct Door
    {
        public Side side;
        public float offset;
        public float width;

        public Door(Side side, float offset, float width)
        {
            this.side = side;
            this.offset = offset;
            this.width = width;
        }
    }

    static Door D(Side side, float width, float offset = 0f) => new Door(side, offset, width);

    static Material z1Floor, z1Wall, z2Floor, z2Wall, z2Accent, z3Floor, z3Wall, z3Accent, trim, gateMat;
    static Transform currentParent;
    static float GroundN;

    [MenuItem("NOVA/Level/Build LV_Game")]
    public static void BuildFromMenu()
    {
        BuildAndSave();
    }

    public static void BuildAndSave()
    {
        EnsureScene();
        Build();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("LV_Game open-world terrain level built.");
    }

    static void EnsureScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (System.IO.File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath);
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
        }
    }

    static void Build()
    {
        LoadMaterials();
        DestroySceneLeftovers();
        var level = FindOrCreateRoot("Level");
        ClearChildren(level);

        var openWorld = GameObject.Find("OpenWorldTerrain");
        if (openWorld)
        {
            for (int i = openWorld.transform.childCount - 1; i >= 0; i--)
            {
                var child = openWorld.transform.GetChild(i);
                if (child.name.StartsWith("Ground_") || child.name.StartsWith("Hill_") || child.name.StartsWith("Ridge_") || child.name == "WorldTerrain")
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        BuildTerrain(openWorld ? openWorld.transform : level);

        var z1 = Group(level, "Zone1_Outskirts");
        var z2 = Group(level, "Zone2_Center");
        var z3 = Group(level, "Zone3_Stronghold");
        var wilderness = Group(level, "Wilderness");
        var markers = Group(level, "Markers");

        BuildZone1(z1);
        BuildZone2(z2);
        BuildZone3(z3);
        BuildWilderness(wilderness);
        BuildMarkers(markers);
        EnsureLighting();
    }

    static void LoadMaterials()
    {
        z1Floor = Resources.Load<Material>(MatRoot + "M_LV_Zone1_Floor");
        z1Wall = Resources.Load<Material>(MatRoot + "M_LV_Zone1_Wall");
        z2Floor = Resources.Load<Material>(MatRoot + "M_LV_Zone2_Floor");
        z2Wall = Resources.Load<Material>(MatRoot + "M_LV_Zone2_Wall");
        z2Accent = Resources.Load<Material>(MatRoot + "M_LV_Zone2_Accent");
        z3Floor = Resources.Load<Material>(MatRoot + "M_LV_Zone3_Floor");
        z3Wall = Resources.Load<Material>(MatRoot + "M_LV_Zone3_Wall");
        z3Accent = Resources.Load<Material>(MatRoot + "M_LV_Zone3_Accent");
        trim = Resources.Load<Material>(MatRoot + "M_LV_Trim");
        gateMat = Resources.Load<Material>(MatRoot + "M_LV_Gate");
    }

    static void BuildTerrain(Transform level)
    {
        if (!AssetDatabase.IsValidFolder(TerrainFolder))
            AssetDatabase.CreateFolder("Assets/Project/Resources/Levels", "Terrain");

        GroundN = (0f - TerrainPosY) / TerrainH;

        var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        if (data == null)
        {
            data = new TerrainData();
            AssetDatabase.CreateAsset(data, TerrainDataPath);
        }

        data.heightmapResolution = HeightRes;
        data.alphamapResolution = AlphaRes;
        data.size = new Vector3(TerrainW, TerrainH, TerrainL);
        data.terrainLayers = CreateTerrainLayers();

        var heights = new float[HeightRes, HeightRes];
        SculptHeights(heights);
        data.SetHeights(0, 0, heights);
        data.SetAlphamaps(0, 0, PaintSplat());

        var old = GameObject.Find("WorldTerrain");
        if (old) Object.DestroyImmediate(old);

        var terrainGo = Terrain.CreateTerrainGameObject(data);
        terrainGo.name = "WorldTerrain";
        terrainGo.transform.SetParent(level, true);
        terrainGo.transform.position = new Vector3(TerrainX, TerrainPosY, TerrainZ);
        terrainGo.isStatic = true;

        var terrain = terrainGo.GetComponent<Terrain>();
        terrain.drawInstanced = true;
        terrain.groupingID = 0;
        EditorUtility.SetDirty(data);
    }

    static TerrainLayer[] CreateTerrainLayers()
    {
        string texFolder = TerrainFolder + "/Textures";
        if (!AssetDatabase.IsValidFolder(texFolder))
            AssetDatabase.CreateFolder(TerrainFolder, "Textures");

        return new[]
        {
            MakeLayer("TL_Dirt", MakeColorTexture("T_Dirt", new Color(0.55f, 0.48f, 0.38f)), new Vector2(14f, 14f)),
            MakeLayer("TL_Contaminated", MakeColorTexture("T_Contaminated", new Color(0.30f, 0.42f, 0.28f)), new Vector2(16f, 16f)),
            MakeLayer("TL_Rock", MakeColorTexture("T_Rock", new Color(0.28f, 0.26f, 0.24f)), new Vector2(18f, 18f)),
            MakeLayer("TL_Path", MakeColorTexture("T_Path", new Color(0.62f, 0.52f, 0.36f)), new Vector2(10f, 10f)),
            MakeLayer("TL_DarkRock", MakeColorTexture("T_DarkRock", new Color(0.18f, 0.11f, 0.11f)), new Vector2(12f, 12f))
        };
    }

    static Texture2D MakeColorTexture(string name, Color color)
    {
        string path = TerrainFolder + "/Textures/" + name + ".png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;

        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, true);
        var pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.12f;
                pixels[y * 64 + x] = color * (0.9f + n);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static TerrainLayer MakeLayer(string name, Texture2D texture, Vector2 tile)
    {
        string path = TerrainFolder + "/" + name + ".terrainlayer";
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, path);
        }

        layer.diffuseTexture = texture;
        layer.tileSize = tile;
        layer.smoothness = 0.08f;
        layer.metallic = 0f;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    static void SculptHeights(float[,] h)
    {
        int res = HeightRes;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = x / (float)(res - 1);
                float nz = z / (float)(res - 1);
                float wx = TerrainX + nx * TerrainW;
                float wz = TerrainZ + nz * TerrainL;

                float n1 = Mathf.PerlinNoise(wx * 0.012f, wz * 0.012f);
                float n2 = Mathf.PerlinNoise(wx * 0.035f + 40f, wz * 0.035f + 12f);
                float rolling = GroundN + n1 * 0.10f + n2 * 0.045f;

                float edge = EdgeFalloff(nx, nz, 0.14f);
                float rim = edge * edge * (0.38f + n2 * 0.12f);

                h[z, x] = rolling + rim;
            }
        }

        FlattenRect(h, -110f, -100f, -20f, 18f, GroundN, 18f);
        FlattenRect(h, -40f, 16f, 95f, 112f, GroundN, 16f);
        FlattenRect(h, 96f, 12f, 220f, 100f, GroundN + 0.012f, 12f);

        CarvePath(h, new Vector2(-80f, -74f), new Vector2(-80f, -14f), 16f, GroundN);
        CarvePath(h, new Vector2(-80f, -14f), new Vector2(-36f, -12f), 14f, GroundN);
        CarvePath(h, new Vector2(-36f, -12f), new Vector2(10f, 18f), 16f, GroundN);
        CarvePath(h, new Vector2(10f, 18f), new Vector2(28f, 50f), 22f, GroundN);
        CarvePath(h, new Vector2(60f, 50f), new Vector2(108f, 50f), 14f, GroundN + 0.006f);
        CarvePath(h, new Vector2(-62f, -74f), new Vector2(-40f, -20f), 9f, GroundN - 0.008f);
        CarvePath(h, new Vector2(-40f, -20f), new Vector2(-4f, 22f), 10f, GroundN - 0.006f);

        Bowl(h, 28f, 50f, 42f, GroundN - 0.01f);
        Ridge(h, 24f, 70f, 58f, 9f, GroundN + 0.11f);
        Plateau(h, 184f, 50f, 38f, GroundN + 0.01f);

        RaiseHill(h, -130f, -40f, 28f, 0.16f);
        RaiseHill(h, -20f, -90f, 22f, 0.12f);
        RaiseHill(h, 70f, -40f, 26f, 0.14f);
        RaiseHill(h, 10f, 130f, 30f, 0.18f);
        RaiseHill(h, 230f, 20f, 24f, 0.20f);
        RaiseHill(h, -50f, 90f, 20f, 0.13f);
        RaiseHill(h, -150f, 80f, 32f, 0.22f);
        RaiseHill(h, 140f, -70f, 28f, 0.17f);
        RaiseHill(h, 250f, 90f, 26f, 0.19f);
    }

    static float EdgeFalloff(float nx, float nz, float margin)
    {
        float e = 0f;
        e = Mathf.Max(e, Mathf.InverseLerp(margin, 0f, nx));
        e = Mathf.Max(e, Mathf.InverseLerp(1f - margin, 1f, nx));
        e = Mathf.Max(e, Mathf.InverseLerp(margin, 0f, nz));
        e = Mathf.Max(e, Mathf.InverseLerp(1f - margin, 1f, nz));
        return Mathf.Clamp01(e);
    }

    static void FlattenRect(float[,] h, float x0, float z0, float x1, float z1, float height, float blend)
    {
        int res = HeightRes;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                Vector2 w = IndexToWorld(x, z);
                float dx = DistanceToRange(w.x, x0, x1);
                float dz = DistanceToRange(w.y, z0, z1);
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                float wgt = 1f - Mathf.Clamp01(dist / blend);
                if (wgt <= 0f) continue;
                wgt = wgt * wgt * (3f - 2f * wgt);
                h[z, x] = Mathf.Lerp(h[z, x], height, wgt);
            }
        }
    }

    static void CarvePath(float[,] h, Vector2 a, Vector2 b, float width, float height)
    {
        int res = HeightRes;
        Vector2 ab = b - a;
        float len = ab.magnitude;
        if (len < 0.01f) return;
        Vector2 dir = ab / len;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                Vector2 p = IndexToWorld(x, z);
                Vector2 ap = p - a;
                float t = Mathf.Clamp(Vector2.Dot(ap, dir), 0f, len);
                float dist = Vector2.Distance(p, a + dir * t);
                float wgt = 1f - Mathf.Clamp01(dist / width);
                if (wgt <= 0f) continue;
                wgt = wgt * wgt;
                h[z, x] = Mathf.Lerp(h[z, x], height, wgt);
            }
        }
    }

    static void Bowl(float[,] h, float cx, float cz, float radius, float height)
    {
        StampRadial(h, cx, cz, radius, height, true);
    }

    static void Plateau(float[,] h, float cx, float cz, float radius, float height)
    {
        StampRadial(h, cx, cz, radius, height, false);
    }

    static void RaiseHill(float[,] h, float cx, float cz, float radius, float add)
    {
        int res = HeightRes;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                Vector2 p = IndexToWorld(x, z);
                float dist = Vector2.Distance(p, new Vector2(cx, cz));
                float wgt = 1f - Mathf.Clamp01(dist / radius);
                if (wgt <= 0f) continue;
                wgt = wgt * wgt;
                h[z, x] += add * wgt;
            }
        }
    }

    static void Ridge(float[,] h, float cx, float cz, float length, float width, float height)
    {
        Vector2 a = new Vector2(cx - length * 0.5f, cz);
        Vector2 b = new Vector2(cx + length * 0.5f, cz);
        CarvePath(h, a, b, width, height);
    }

    static void StampRadial(float[,] h, float cx, float cz, float radius, float height, bool smoothBowl)
    {
        int res = HeightRes;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                Vector2 p = IndexToWorld(x, z);
                float dist = Vector2.Distance(p, new Vector2(cx, cz));
                float wgt = 1f - Mathf.Clamp01(dist / radius);
                if (wgt <= 0f) continue;
                if (smoothBowl) wgt = wgt * wgt;
                else wgt = wgt * wgt * (3f - 2f * wgt);
                h[z, x] = Mathf.Lerp(h[z, x], height, wgt);
            }
        }
    }

    static float[,,] PaintSplat()
    {
        var alpha = new float[AlphaRes, AlphaRes, 5];
        for (int z = 0; z < AlphaRes; z++)
        {
            for (int x = 0; x < AlphaRes; x++)
            {
                float nx = x / (float)(AlphaRes - 1);
                float nz = z / (float)(AlphaRes - 1);
                float wx = TerrainX + nx * TerrainW;
                float wz = TerrainZ + nz * TerrainL;

                float dirt = 0.15f;
                float contam = 0f;
                float rock = 0.25f;
                float path = 0f;
                float dark = 0f;

                if (InRect(wx, wz, -120f, -110f, -8f, 24f)) dirt = 1f;
                if (InRect(wx, wz, -48f, 10f, 100f, 118f)) contam = 1f;
                if (InRect(wx, wz, 90f, 8f, 230f, 110f)) dark = 0.85f;

                if (NearSegment(wx, wz, -80f, -74f, 28f, 50f, 11f) ||
                    NearSegment(wx, wz, -62f, -74f, -4f, 22f, 7f) ||
                    NearSegment(wx, wz, 60f, 50f, 110f, 50f, 8f))
                    path = 1f;

                float edge = EdgeFalloff(nx, nz, 0.16f);
                rock += edge * 1.4f;
                if (edge > 0.35f) dark += 0.4f;

                float n = Mathf.PerlinNoise(wx * 0.04f, wz * 0.04f);
                rock += n * 0.25f;
                dirt += (1f - n) * 0.2f;

                float sum = dirt + contam + rock + path + dark + 0.0001f;
                alpha[z, x, 0] = dirt / sum;
                alpha[z, x, 1] = contam / sum;
                alpha[z, x, 2] = rock / sum;
                alpha[z, x, 3] = path / sum;
                alpha[z, x, 4] = dark / sum;
            }
        }

        return alpha;
    }

    static bool InRect(float x, float z, float x0, float z0, float x1, float z1)
    {
        return x >= x0 && x <= x1 && z >= z0 && z <= z1;
    }

    static bool NearSegment(float x, float z, float ax, float az, float bx, float bz, float width)
    {
        Vector2 p = new Vector2(x, z);
        Vector2 a = new Vector2(ax, az);
        Vector2 b = new Vector2(bx, bz);
        Vector2 ab = b - a;
        float len = ab.magnitude;
        if (len < 0.01f) return false;
        float t = Mathf.Clamp(Vector2.Dot(p - a, ab / len), 0f, len);
        return Vector2.Distance(p, a + ab / len * t) < width;
    }

    static Vector2 IndexToWorld(int x, int z)
    {
        float nx = x / (float)(HeightRes - 1);
        float nz = z / (float)(HeightRes - 1);
        return new Vector2(TerrainX + nx * TerrainW, TerrainZ + nz * TerrainL);
    }

    static float DistanceToRange(float v, float a, float b)
    {
        if (v < a) return a - v;
        if (v > b) return v - b;
        return 0f;
    }

    static void BuildZone1(Transform root)
    {
        currentParent = root;

        Ruin("Ruin_Start_N", new Vector3(-80f, 1.6f, -60f), new Vector3(18f, 3.2f, 0.8f), z1Wall);
        Ruin("Ruin_Start_W", new Vector3(-96f, 1.4f, -74f), new Vector3(0.8f, 2.8f, 12f), z1Wall);
        Ruin("Ruin_Start_Broken", new Vector3(-70f, 1.1f, -82f), new Vector3(8f, 2.2f, 0.7f), z1Wall, 18f);
        Box("RuinBlock_A", new Vector3(-88f, 0.7f, -70f), new Vector3(3f, 1.4f, 6f), z1Wall, true);
        Box("RuinBlock_B", new Vector3(-72f, 0.7f, -80f), new Vector3(5f, 1.4f, 2.2f), z1Wall, true);

        Ruin("Melee_Wall_W", new Vector3(-92f, 1.8f, -42f), new Vector3(0.8f, 3.6f, 14f), z1Wall);
        Ruin("Melee_Wall_N", new Vector3(-84f, 1.5f, -33f), new Vector3(10f, 3f, 0.8f), z1Wall);
        Box("MeleeCover", new Vector3(-76f, 0.7f, -46f), new Vector3(2f, 1.4f, 5f), z1Wall, true);

        Ruin("Explore_W", new Vector3(-66f, 1.7f, -42f), new Vector3(0.7f, 3.4f, 12f), z1Wall);
        Ruin("Explore_N", new Vector3(-58f, 1.6f, -34f), new Vector3(12f, 3.2f, 0.7f), z1Wall);
        Ruin("Explore_E", new Vector3(-50f, 1.3f, -40f), new Vector3(0.7f, 2.6f, 8f), z1Wall);
        Box("ExploreCrate", new Vector3(-54f, 0.6f, -38f), new Vector3(2.2f, 1.2f, 2.2f), trim, true);

        Ruin("Mix_L", new Vector3(-93f, 2f, -14f), new Vector3(0.8f, 4f, 16f), z1Wall);
        Ruin("Mix_Broken", new Vector3(-72f, 1.4f, -5f), new Vector3(12f, 2.8f, 0.7f), z1Wall, -12f);
        Box("MixCover_A", new Vector3(-88f, 0.7f, -14f), new Vector3(2f, 1.4f, 6f), z1Wall, true);
        Box("MixCover_B", new Vector3(-72f, 0.7f, -8f), new Vector3(6f, 1.4f, 2f), z1Wall, true);

        Ruin("Tank_N", new Vector3(-36f, 1.6f, -2f), new Vector3(14f, 3.2f, 0.8f), z1Wall);
        Ruin("Tank_E", new Vector3(-25f, 1.4f, -12f), new Vector3(0.8f, 2.8f, 10f), z1Wall);
        Box("TankCover", new Vector3(-36f, 0.75f, -6f), new Vector3(4f, 1.5f, 2f), trim, true);

        Box("Gate_Shortcut", new Vector3(-61.65f, 2.5f, -74f), new Vector3(0.5f, 5f, 6.2f), gateMat, true);
        Ruin("Ravine_L", new Vector3(-44f, 2.2f, -48f), new Vector3(1.2f, 4.4f, 18f), z1Wall, 8f);
        Ruin("Ravine_R", new Vector3(-36f, 2.0f, -48f), new Vector3(1.2f, 4f, 16f), z1Wall, 8f);
    }

    static void BuildZone2(Transform root)
    {
        currentParent = root;

        Room("WestBuilding", -28f, 50f, 24f, 30f, 6f, z2Floor, z2Wall, D(Side.E, 8f));
        Box("WestInnerWall_L", new Vector3(-35.5f, 3f, 50f), new Vector3(9f, 6f, 0.7f), z2Wall, true);
        Box("WestInnerWall_R", new Vector3(-20.5f, 3f, 50f), new Vector3(9f, 6f, 0.7f), z2Wall, true);

        Ruin("AlleyCliff_N", new Vector3(80f, 4f, 64f), new Vector3(10f, 8f, 2.2f), z2Wall);
        Ruin("AlleyCliff_S", new Vector3(80f, 3.5f, 30f), new Vector3(10f, 7f, 2.2f), z2Wall);
        Ruin("AlleyCliff_E", new Vector3(86f, 4.2f, 48f), new Vector3(2f, 8.4f, 22f), z2Wall);

        Room("SideRoom", 80f, 18f, 16f, 14f, 6f, z2Floor, z2Wall, D(Side.N, 6f));
        Ruin("NorthRuin_W", new Vector3(18f, 2.2f, 92f), new Vector3(0.8f, 4.4f, 12f), z2Wall);
        Ruin("NorthRuin_E", new Vector3(38f, 2.0f, 92f), new Vector3(0.8f, 4f, 10f), z2Wall);

        Box("MidArena", new Vector3(28f, 0.35f, 50f), new Vector3(14f, 0.7f, 14f), z2Accent, true);
        Box("Cover_SW", new Vector3(8f, 0.7f, 36f), new Vector3(2f, 1.4f, 8f), z2Wall, true);
        Box("Cover_SE", new Vector3(48f, 0.7f, 38f), new Vector3(8f, 1.4f, 2f), z2Wall, true);
        Box("Cover_NW", new Vector3(12f, 0.7f, 62f), new Vector3(2f, 1.4f, 6f), z2Wall, true);
        Box("Cover_NE", new Vector3(46f, 0.7f, 64f), new Vector3(6f, 1.4f, 2f), z2Wall, true);
        Box("Pillar_A", new Vector3(16f, 2.5f, 44f), new Vector3(2.2f, 5f, 2.2f), trim, true);
        Box("Pillar_B", new Vector3(40f, 2.5f, 44f), new Vector3(2.2f, 5f, 2.2f), trim, true);
        Box("Pillar_C", new Vector3(16f, 2.5f, 58f), new Vector3(2.2f, 5f, 2.2f), trim, true);
        Box("Pillar_D", new Vector3(40f, 2.5f, 58f), new Vector3(2.2f, 5f, 2.2f), trim, true);
        Box("Switch_Shortcut", new Vector3(18f, 0.6f, 44f), new Vector3(1.4f, 1.2f, 1.4f), z2Accent, true);

        Ruin("PlazaRuin_SW", new Vector3(-2f, 1.8f, 28f), new Vector3(10f, 3.6f, 0.9f), z2Wall, 12f);
        Ruin("PlazaRuin_NE", new Vector3(58f, 2.1f, 72f), new Vector3(8f, 4.2f, 1f), z2Wall, -20f);
    }

    static void BuildZone3(Transform root)
    {
        currentParent = root;
        Room("EntryHall", 112f, 50f, 20f, 18f, 8f, z3Floor, z3Wall, D(Side.W, 8f), D(Side.E, 6f), roof: true);
        Room("Split", 128f, 50f, 10f, 12f, 8f, z3Floor, z3Wall, D(Side.W, 6f), D(Side.N, 6f), D(Side.S, 6f), roof: true);
        Room("C_SplitN", 128f, 60f, 8f, 8f, 8f, z3Floor, z3Wall, D(Side.S, 6f), D(Side.N, 6f), roof: true);
        Room("CombatN", 128f, 74f, 26f, 22f, 8f, z3Floor, z3Wall, D(Side.S, 6f), D(Side.E, 6f), roof: true);
        Room("C_SplitS", 128f, 40f, 8f, 8f, 8f, z3Floor, z3Wall, D(Side.N, 6f), D(Side.S, 6f), roof: true);
        Room("CombatS", 128f, 26f, 26f, 22f, 8f, z3Floor, z3Wall, D(Side.N, 6f), D(Side.E, 6f), roof: true);
        Room("C_NEast", 148f, 74f, 14f, 8f, 8f, z3Floor, z3Wall, D(Side.W, 6f), D(Side.E, 6f), roof: true);
        Room("C_SEast", 148f, 26f, 14f, 8f, 8f, z3Floor, z3Wall, D(Side.W, 6f), D(Side.E, 6f), roof: true);
        Room("C_NSouth", 152f, 62f, 8f, 16f, 8f, z3Floor, z3Wall, D(Side.N, 6f), D(Side.S, 6f), roof: true);
        Room("C_SNorth", 152f, 38f, 8f, 16f, 8f, z3Floor, z3Wall, D(Side.S, 6f), D(Side.N, 6f), roof: true);
        Room("Merge", 152f, 50f, 16f, 14f, 8f, z3Floor, z3Wall, D(Side.N, 6f), D(Side.S, 6f), D(Side.E, 8f), roof: true);
        Room("C_Boss", 166f, 50f, 12f, 10f, 8f, z3Floor, z3Wall, D(Side.W, 8f), D(Side.E, 10f), roof: true);
        Room("BossArena", 184f, 50f, 44f, 44f, 10f, z3Floor, z3Wall, D(Side.W, 10f));
        Room("C_AlleyEntry", 94f, 50f, 16f, 8f, 7f, z3Floor, z3Wall, D(Side.W, 8f), D(Side.E, 8f), roof: true);

        Box("CombatN_Cover", new Vector3(122f, 0.7f, 74f), new Vector3(6f, 1.4f, 2f), trim, true);
        Box("CombatS_Cover", new Vector3(134f, 0.7f, 26f), new Vector3(2f, 1.4f, 6f), trim, true);
        Box("BossRing_N", new Vector3(184f, 0.6f, 66f), new Vector3(18f, 1.2f, 1.6f), z3Accent, true);
        Box("BossRing_S", new Vector3(184f, 0.6f, 34f), new Vector3(18f, 1.2f, 1.6f), z3Accent, true);
        Box("BossRing_E", new Vector3(200f, 0.6f, 50f), new Vector3(1.6f, 1.2f, 18f), z3Accent, true);
        Box("BossRing_W", new Vector3(172f, 0.6f, 50f), new Vector3(1.6f, 1.2f, 12f), z3Accent, true);
        Box("BossPillar_NE", new Vector3(198f, 5f, 66f), new Vector3(2.4f, 10f, 2.4f), trim, true);
        Box("BossPillar_NW", new Vector3(170f, 5f, 66f), new Vector3(2.4f, 10f, 2.4f), trim, true);
        Box("BossPillar_SE", new Vector3(198f, 5f, 34f), new Vector3(2.4f, 10f, 2.4f), trim, true);
        Box("BossPillar_SW", new Vector3(170f, 5f, 34f), new Vector3(2.4f, 10f, 2.4f), trim, true);
        Box("BossCenter", new Vector3(184f, 0.2f, 50f), new Vector3(8f, 0.4f, 8f), z3Accent, true);

        Ruin("HQCliff_N", new Vector3(150f, 6f, 104f), new Vector3(70f, 12f, 6f), z3Wall);
        Ruin("HQCliff_S", new Vector3(150f, 5.5f, 8f), new Vector3(64f, 11f, 6f), z3Wall);
        Ruin("HQCliff_E", new Vector3(222f, 7f, 50f), new Vector3(6f, 14f, 70f), z3Wall);
    }

    static void BuildWilderness(Transform root)
    {
        currentParent = root;
        var rng = new System.Random(2026);

        Vector3[] trees =
        {
            new Vector3(-120f, 0f, -50f), new Vector3(-108f, 0f, -20f), new Vector3(-125f, 0f, 10f),
            new Vector3(-95f, 0f, 40f), new Vector3(-140f, 0f, -80f), new Vector3(-60f, 0f, -110f),
            new Vector3(-20f, 0f, -100f), new Vector3(20f, 0f, -80f), new Vector3(50f, 0f, -55f),
            new Vector3(90f, 0f, -30f), new Vector3(-10f, 0f, 120f), new Vector3(40f, 0f, 130f),
            new Vector3(80f, 0f, 118f), new Vector3(120f, 0f, 130f), new Vector3(-70f, 0f, 80f),
            new Vector3(-100f, 0f, 70f), new Vector3(210f, 0f, 0f), new Vector3(200f, 0f, 110f),
            new Vector3(-150f, 0f, 30f), new Vector3(8f, 0f, -120f), new Vector3(70f, 0f, 150f),
            new Vector3(-40f, 0f, 140f), new Vector3(160f, 0f, -20f), new Vector3(-160f, 0f, -40f)
        };

        for (int i = 0; i < trees.Length; i++)
        {
            float jitter = (float)(rng.NextDouble() * 6.0 - 3.0);
            Vector3 p = trees[i] + new Vector3(jitter, 0f, jitter * 0.4f);
            Tree(i, p, rng);
        }

        Vector3[] rocks =
        {
            new Vector3(-100f, 0.8f, -60f), new Vector3(-50f, 0.9f, -90f), new Vector3(-10f, 1.1f, -40f),
            new Vector3(12f, 0.8f, 8f), new Vector3(55f, 1.0f, 20f), new Vector3(70f, 1.2f, 80f),
            new Vector3(-30f, 0.9f, 30f), new Vector3(100f, 1.4f, 20f), new Vector3(-90f, 1.0f, 20f),
            new Vector3(0f, 0.7f, 90f), new Vector3(45f, 1.3f, 110f), new Vector3(-15f, 0.8f, -15f)
        };

        for (int i = 0; i < rocks.Length; i++)
        {
            float s = 2.2f + (float)rng.NextDouble() * 3.5f;
            float yaw = (float)rng.NextDouble() * 180f;
            Box("Rock_" + i, rocks[i] + Vector3.up * (s * 0.25f), new Vector3(s, s * 0.7f, s * 0.85f), trim, true, Quaternion.Euler(0f, yaw, 8f));
        }
    }

    static void Tree(int index, Vector3 pos, System.Random rng)
    {
        float h = 3.6f + (float)rng.NextDouble() * 2.4f;
        float canopy = 2.1f + (float)rng.NextDouble() * 1.2f;
        Material leaf = pos.x > 0f && pos.z > 20f ? z2Accent : z1Wall;
        Box("TreeTrunk_" + index, pos + new Vector3(0f, h * 0.45f, 0f), new Vector3(0.45f, h, 0.45f), trim, true);
        Box("TreeCanopy_" + index, pos + new Vector3(0f, h + canopy * 0.25f, 0f), new Vector3(canopy, canopy, canopy), leaf, true);
    }

    static void BuildMarkers(Transform root)
    {
        Empty(root, "PlayerSpawn", new Vector3(-80f, 0f, -82f));
        Empty(root, "Checkpoint_Zone1", new Vector3(-80f, 0f, -42f));
        Empty(root, "Checkpoint_Zone2", new Vector3(28f, 0f, 36f));
        Empty(root, "Checkpoint_Zone3", new Vector3(112f, 0f, 50f));
        Empty(root, "Arena_MeleeTeach", new Vector3(-80f, 0f, -42f));
        Empty(root, "Arena_Explore", new Vector3(-58f, 0f, -42f));
        Empty(root, "Arena_Mixed", new Vector3(-80f, 0f, -14f));
        Empty(root, "Arena_TankIntro", new Vector3(-36f, 0f, -12f));
        Empty(root, "Arena_Plaza", new Vector3(28f, 0f, 50f));
        Empty(root, "Arena_HighPath", new Vector3(24f, 4.4f, 70f));
        Empty(root, "Arena_CombatN", new Vector3(128f, 0f, 74f));
        Empty(root, "Arena_CombatS", new Vector3(128f, 0f, 26f));
        Empty(root, "Arena_Boss", new Vector3(184f, 0f, 50f));
        Empty(root, "Switch_Shortcut_Marker", new Vector3(18f, 1.4f, 44f));
        Empty(root, "Gate_Shortcut_Marker", new Vector3(-61.65f, 2.5f, -74f));
    }

    static void Ruin(string name, Vector3 pos, Vector3 size, Material mat, float yaw = 0f)
    {
        Box(name, pos, size, mat, true, Quaternion.Euler(0f, yaw, 0f));
    }

    static void Room(
        string name,
        float cx,
        float cz,
        float w,
        float d,
        float wallH,
        Material floorMat,
        Material wallMat,
        Door doorA = default,
        Door doorB = default,
        Door doorC = default,
        Door doorD = default,
        Door doorE = default,
        bool roof = false)
    {
        var room = Group(currentParent, name);
        var prev = currentParent;
        currentParent = room;

        Box("Floor", new Vector3(cx, FloorY, cz), new Vector3(w, FloorH, d), floorMat, false);

        var doors = new List<Door>();
        if (doorA.width > 0f) doors.Add(doorA);
        if (doorB.width > 0f) doors.Add(doorB);
        if (doorC.width > 0f) doors.Add(doorC);
        if (doorD.width > 0f) doors.Add(doorD);
        if (doorE.width > 0f) doors.Add(doorE);

        BuildWall(cx, cz, w, d, wallH, wallMat, Side.N, doors);
        BuildWall(cx, cz, w, d, wallH, wallMat, Side.S, doors);
        BuildWall(cx, cz, w, d, wallH, wallMat, Side.E, doors);
        BuildWall(cx, cz, w, d, wallH, wallMat, Side.W, doors);

        if (roof)
            Box("Roof", new Vector3(cx, wallH + 0.15f, cz), new Vector3(w, 0.3f, d), wallMat, true);

        currentParent = prev;
    }

    static void BuildWall(float cx, float cz, float w, float d, float wallH, Material wallMat, Side side, List<Door> doors)
    {
        bool alongX = side == Side.N || side == Side.S;
        float length = alongX ? w : d;
        float origin = alongX ? cx - w * 0.5f : cz - d * 0.5f;
        float axisCenter = alongX ? cx : cz;

        var cuts = new List<(float start, float end)>();
        foreach (var door in doors)
        {
            if (door.side != side) continue;
            float mid = axisCenter + door.offset;
            cuts.Add((mid - door.width * 0.5f, mid + door.width * 0.5f));
        }

        cuts.Sort((a, b) => a.start.CompareTo(b.start));

        float cursor = origin;
        float end = origin + length;
        int index = 0;
        foreach (var cut in cuts)
        {
            float cutStart = Mathf.Max(cursor, cut.start);
            float cutEnd = Mathf.Min(end, cut.end);
            if (cutStart - cursor > 0.4f)
                PlaceWallSegment(cx, cz, w, d, wallH, wallMat, side, cursor, cutStart, index++);
            cursor = Mathf.Max(cursor, cutEnd);
        }

        if (end - cursor > 0.4f)
            PlaceWallSegment(cx, cz, w, d, wallH, wallMat, side, cursor, end, index);
    }

    static void PlaceWallSegment(
        float cx,
        float cz,
        float w,
        float d,
        float wallH,
        Material wallMat,
        Side side,
        float from,
        float to,
        int index)
    {
        float seg = to - from;
        float mid = (from + to) * 0.5f;
        Vector3 pos;
        Vector3 size;
        switch (side)
        {
            case Side.N:
                pos = new Vector3(mid, wallH * 0.5f, cz + d * 0.5f - WallT * 0.5f);
                size = new Vector3(seg, wallH, WallT);
                break;
            case Side.S:
                pos = new Vector3(mid, wallH * 0.5f, cz - d * 0.5f + WallT * 0.5f);
                size = new Vector3(seg, wallH, WallT);
                break;
            case Side.E:
                pos = new Vector3(cx + w * 0.5f - WallT * 0.5f, wallH * 0.5f, mid);
                size = new Vector3(WallT, wallH, seg);
                break;
            default:
                pos = new Vector3(cx - w * 0.5f + WallT * 0.5f, wallH * 0.5f, mid);
                size = new Vector3(WallT, wallH, seg);
                break;
        }

        Box($"Wall_{side}_{index}", pos, size, wallMat, true);
    }

    static GameObject Box(
        string name,
        Vector3 pos,
        Vector3 size,
        Material mat,
        bool wall,
        Quaternion rot = default,
        bool active = true)
    {
        if (rot == default) rot = Quaternion.identity;

        ProBuilderMesh pb = ShapeFactory.Instantiate<Cube>();
        var go = pb.gameObject;
        go.name = name;
        go.SetActive(active);
        go.transform.SetParent(currentParent, true);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = size;
        go.isStatic = true;
        go.layer = WallLayer;
        if (wall) go.tag = "Wall";

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer && mat) renderer.sharedMaterial = mat;

        var meshCollider = go.GetComponent<MeshCollider>();
        if (meshCollider == null)
            go.AddComponent<BoxCollider>();
        else
            meshCollider.convex = false;

        pb.ToMesh();
        pb.Refresh();
        return go;
    }

    static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.isStatic = true;
        return go.transform;
    }

    static void Empty(Transform parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
    }

    static void DestroySceneLeftovers()
    {
        var names = new HashSet<string>
        {
            "Level", "Zone1_Outskirts", "Zone2_Center", "Zone3_Stronghold",
            "Connectors", "Markers", "Wilderness"
        };

        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (names.Contains(root.name))
                Object.DestroyImmediate(root);
        }
    }

    static Transform FindOrCreateRoot(string name)
    {
        var existing = GameObject.Find(name);
        if (existing) return existing.transform;
        return new GameObject(name).transform;
    }

    static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.GetChild(i).gameObject);
    }

    static void EnsureLighting()
    {
        var light = Object.FindAnyObjectByType<Light>();
        if (!light)
        {
            var lightGo = new GameObject("Directional Light");
            light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.AddComponent<UniversalAdditionalLightData>();
        }

        light.color = new Color(1f, 0.92f, 0.78f);
        light.intensity = 1.45f;
        light.shadows = LightShadows.Soft;
        light.transform.position = new Vector3(40f, 80f, 10f);
        light.transform.rotation = Quaternion.Euler(42f, -25f, 0f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.52f, 0.55f, 0.45f);
        RenderSettings.fogDensity = 0.009f;

        if (!Object.FindAnyObjectByType<Volume>())
        {
            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        }

        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Preview Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();
        }

        cam.fieldOfView = 60f;
        cam.farClipPlane = 600f;
        cam.transform.position = new Vector3(-92f, 22f, -118f);
        cam.transform.rotation = Quaternion.Euler(18f, 28f, 0f);
    }
}

#endif
