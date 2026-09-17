using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class LVGameOpenWorldTerrain : MonoBehaviour
{
    const string TerrainDataPath = "Assets/Project/Resources/Levels/Terrain/TD_LV_Game.asset";
    const float TerrainX = -180f;
    const float TerrainZ = -180f;
    const float TerrainW = 480f;
    const float TerrainL = 420f;
    const float TerrainH = 42f;
    const float TerrainPosY = -1.5f;

    static readonly string[] HideGroups =
    {
        "Plaza", "C_StartMelee", "C_MeleeMix", "C_MixTank", "C_TankEast", "C_ToPlaza",
        "SC_East", "SC_North", "SC_Mid", "SC_ToPlaza", "C_PlazaAlley", "C_PlazaNorth",
        "C_BldgPlaza", "Connectors", "MeleePit", "ExploreRoom", "MixHall", "TankCourt",
        "Alley", "NorthRoom", "StartYard"
    };

    [SerializeField] Terrain terrain;

    void OnEnable()
    {
        HideBoxedGeometry();
        EnsureTerrain();
        HidePlaceholdersIfTerrainExists();
        EnsureOpenWorldProps();
        ApplyAtmosphere();
    }

    void HideBoxedGeometry()
    {
        foreach (string name in HideGroups)
        {
            var found = GameObject.Find(name);
            if (found) found.SetActive(false);
        }
    }

    void EnsureTerrain()
    {
        if (terrain == null)
            terrain = GetComponentInChildren<Terrain>();
        if (terrain == null)
            terrain = FindAnyObjectByType<Terrain>();
        if (terrain != null)
            return;

        BuildTerrain();
    }

    void HidePlaceholdersIfTerrainExists()
    {
        if (terrain == null) return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("Ground_") || child.name.StartsWith("Hill_") || child.name.StartsWith("Ridge_"))
                child.gameObject.SetActive(false);
        }
    }

    void BuildTerrain()
    {
        int heightRes = Application.isPlaying ? 129 : 257;
        int alphaRes = Application.isPlaying ? 128 : 256;
        float groundN = (0f - TerrainPosY) / TerrainH;
        TerrainData data = null;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Project/Resources/Levels/Terrain"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Project/Resources/Levels"))
                    AssetDatabase.CreateFolder("Assets/Project/Resources", "Levels");
                AssetDatabase.CreateFolder("Assets/Project/Resources/Levels", "Terrain");
            }

            data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }
        }
#endif
        if (data == null)
            data = new TerrainData();

        data.heightmapResolution = heightRes;
        data.alphamapResolution = alphaRes;
        data.size = new Vector3(TerrainW, TerrainH, TerrainL);
        data.terrainLayers = CreateLayers();
        data.SetHeights(0, 0, Sculpt(groundN, heightRes));
        data.SetAlphamaps(0, 0, Paint(alphaRes));

        var terrainGo = Terrain.CreateTerrainGameObject(data);
        terrainGo.name = "WorldTerrain";
        terrainGo.transform.SetParent(transform, true);
        terrainGo.transform.position = new Vector3(TerrainX, TerrainPosY, TerrainZ);
        terrain = terrainGo.GetComponent<Terrain>();
        terrain.drawInstanced = true;
        terrain.groupingID = 0;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(data);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    TerrainLayer[] CreateLayers()
    {
        return new[]
        {
            Layer(new Color(0.55f, 0.48f, 0.38f), 14f),
            Layer(new Color(0.30f, 0.42f, 0.28f), 16f),
            Layer(new Color(0.28f, 0.26f, 0.24f), 18f),
            Layer(new Color(0.62f, 0.52f, 0.36f), 10f),
            Layer(new Color(0.18f, 0.11f, 0.11f), 12f)
        };
    }

    static TerrainLayer Layer(Color color, float tile)
    {
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            float n = Mathf.PerlinNoise((i % 32) * 0.2f, (i / 32) * 0.2f) * 0.12f;
            pixels[i] = color * (0.9f + n);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;

        return new TerrainLayer
        {
            diffuseTexture = tex,
            tileSize = new Vector2(tile, tile),
            smoothness = 0.08f
        };
    }

    float[,] Sculpt(float groundN, int res)
    {
        var h = new float[res, res];
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                Vector2 w = IndexToWorld(x, z, res);
                float nx = x / (float)(res - 1);
                float nz = z / (float)(res - 1);
                float n1 = Mathf.PerlinNoise(w.x * 0.012f, w.y * 0.012f);
                float n2 = Mathf.PerlinNoise(w.x * 0.035f + 40f, w.y * 0.035f + 12f);
                float n3 = Mathf.PerlinNoise(w.x * 0.08f + 9f, w.y * 0.08f + 3f);
                float edge = Edge(nx, nz, 0.14f);
                h[z, x] = groundN + n1 * 0.10f + n2 * 0.045f + n3 * 0.018f + edge * edge * (0.42f + n2 * 0.14f);
            }
        }

        Flatten(h, res, -110f, -100f, -20f, 18f, groundN, 18f);
        Flatten(h, res, -40f, 16f, 95f, 112f, groundN, 16f);
        Flatten(h, res, 96f, 12f, 220f, 100f, groundN + 0.012f, 12f);
        Path(h, res, new Vector2(-80f, -74f), new Vector2(-80f, -14f), 16f, groundN);
        Path(h, res, new Vector2(-80f, -14f), new Vector2(-36f, -12f), 14f, groundN);
        Path(h, res, new Vector2(-36f, -12f), new Vector2(10f, 18f), 16f, groundN);
        Path(h, res, new Vector2(10f, 18f), new Vector2(28f, 50f), 22f, groundN);
        Path(h, res, new Vector2(60f, 50f), new Vector2(108f, 50f), 14f, groundN + 0.006f);
        Path(h, res, new Vector2(-62f, -74f), new Vector2(-40f, -20f), 9f, groundN - 0.008f);
        Path(h, res, new Vector2(-40f, -20f), new Vector2(-4f, 22f), 10f, groundN - 0.006f);
        Bowl(h, res, 28f, 50f, 42f, groundN - 0.01f);
        Path(h, res, new Vector2(-4f, 70f), new Vector2(52f, 70f), 9f, groundN + 0.11f);
        Hill(h, res, -130f, -40f, 28f, 0.16f);
        Hill(h, res, -20f, -90f, 22f, 0.12f);
        Hill(h, res, 70f, -40f, 26f, 0.14f);
        Hill(h, res, 10f, 130f, 30f, 0.18f);
        Hill(h, res, 230f, 20f, 24f, 0.20f);
        Hill(h, res, -50f, 90f, 20f, 0.13f);
        Hill(h, res, -150f, 80f, 32f, 0.22f);
        Hill(h, res, 140f, -70f, 28f, 0.17f);
        return h;
    }

    float[,,] Paint(int res)
    {
        var a = new float[res, res, 5];
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = x / (float)(res - 1);
                float nz = z / (float)(res - 1);
                float wx = TerrainX + nx * TerrainW;
                float wz = TerrainZ + nz * TerrainL;
                float dirt = In(wx, wz, -120f, -110f, -8f, 24f) ? 1f : 0.15f;
                float contam = In(wx, wz, -48f, 10f, 100f, 118f) ? 1f : 0f;
                float dark = In(wx, wz, 90f, 8f, 230f, 110f) ? 0.85f : 0f;
                float path = Near(wx, wz, -80f, -74f, 28f, 50f, 11f) ||
                             Near(wx, wz, -62f, -74f, -4f, 22f, 7f) ||
                             Near(wx, wz, 60f, 50f, 110f, 50f, 8f)
                    ? 1f
                    : 0f;
                float rock = 0.25f + Edge(nx, nz, 0.16f) * 1.4f + Mathf.PerlinNoise(wx * 0.04f, wz * 0.04f) * 0.25f;
                float grass = Mathf.PerlinNoise(wx * 0.02f + 5f, wz * 0.02f) * 0.35f;
                contam += grass * 0.45f;
                dirt += (1f - grass) * 0.2f;
                float sum = dirt + contam + rock + path + dark + 0.0001f;
                a[z, x, 0] = dirt / sum;
                a[z, x, 1] = contam / sum;
                a[z, x, 2] = rock / sum;
                a[z, x, 3] = path / sum;
                a[z, x, 4] = dark / sum;
            }
        }

        return a;
    }

    static float Edge(float nx, float nz, float margin)
    {
        float e = 0f;
        e = Mathf.Max(e, Mathf.InverseLerp(margin, 0f, nx));
        e = Mathf.Max(e, Mathf.InverseLerp(1f - margin, 1f, nx));
        e = Mathf.Max(e, Mathf.InverseLerp(margin, 0f, nz));
        e = Mathf.Max(e, Mathf.InverseLerp(1f - margin, 1f, nz));
        return Mathf.Clamp01(e);
    }

    void Flatten(float[,] h, int res, float x0, float z0, float x1, float z1, float height, float blend)
    {
        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
        {
            Vector2 w = IndexToWorld(x, z, res);
            float dx = w.x < x0 ? x0 - w.x : w.x > x1 ? w.x - x1 : 0f;
            float dz = w.y < z0 ? z0 - w.y : w.y > z1 ? w.y - z1 : 0f;
            float wgt = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dz * dz) / blend);
            if (wgt <= 0f) continue;
            wgt = wgt * wgt * (3f - 2f * wgt);
            h[z, x] = Mathf.Lerp(h[z, x], height, wgt);
        }
    }

    void Path(float[,] h, int res, Vector2 a, Vector2 b, float width, float height)
    {
        Vector2 ab = b - a;
        float len = ab.magnitude;
        if (len < 0.01f) return;
        Vector2 dir = ab / len;
        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
        {
            Vector2 p = IndexToWorld(x, z, res);
            float t = Mathf.Clamp(Vector2.Dot(p - a, dir), 0f, len);
            float wgt = 1f - Mathf.Clamp01(Vector2.Distance(p, a + dir * t) / width);
            if (wgt <= 0f) continue;
            h[z, x] = Mathf.Lerp(h[z, x], height, wgt * wgt);
        }
    }

    void Bowl(float[,] h, int res, float cx, float cz, float radius, float height)
    {
        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
        {
            float wgt = 1f - Mathf.Clamp01(Vector2.Distance(IndexToWorld(x, z, res), new Vector2(cx, cz)) / radius);
            if (wgt <= 0f) continue;
            h[z, x] = Mathf.Lerp(h[z, x], height, wgt * wgt);
        }
    }

    void Hill(float[,] h, int res, float cx, float cz, float radius, float add)
    {
        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
        {
            float wgt = 1f - Mathf.Clamp01(Vector2.Distance(IndexToWorld(x, z, res), new Vector2(cx, cz)) / radius);
            if (wgt <= 0f) continue;
            h[z, x] += add * wgt * wgt;
        }
    }

    static Vector2 IndexToWorld(int x, int z, int res)
    {
        return new Vector2(
            TerrainX + x / (float)(res - 1) * TerrainW,
            TerrainZ + z / (float)(res - 1) * TerrainL);
    }

    static bool In(float x, float z, float x0, float z0, float x1, float z1)
    {
        return x >= x0 && x <= x1 && z >= z0 && z <= z1;
    }

    static bool Near(float x, float z, float ax, float az, float bx, float bz, float width)
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

    void EnsureOpenWorldProps()
    {
        var z1 = GameObject.Find("Zone1_Outskirts");
        var z2 = GameObject.Find("Zone2_Center");
        var z3 = GameObject.Find("Zone3_Stronghold");
        var level = GameObject.Find("Level");
        Transform wildernessParent = level ? level.transform : transform;

        var z1Root = z1 ? z1.transform : transform;
        var z2Root = z2 ? z2.transform : transform;
        var z3Root = z3 ? z3.transform : transform;

        if (GameObject.Find("Ruin_Start_N") != null)
        {
            EnsureWilderness(wildernessParent);
            return;
        }

        Prop(z1Root, "Ruin_Start_N", new Vector3(-80f, 1.6f, -60f), new Vector3(18f, 3.2f, 0.8f), WallColor(0.42f, 0.36f, 0.28f), 0f);
        Prop(z1Root, "Ruin_Start_W", new Vector3(-96f, 1.4f, -74f), new Vector3(0.8f, 2.8f, 12f), WallColor(0.42f, 0.36f, 0.28f), 0f);
        Prop(z1Root, "Ruin_Start_Broken", new Vector3(-70f, 1.1f, -82f), new Vector3(8f, 2.2f, 0.7f), WallColor(0.42f, 0.36f, 0.28f), 18f);
        Prop(z1Root, "Melee_Wall_W", new Vector3(-92f, 1.8f, -42f), new Vector3(0.8f, 3.6f, 14f), WallColor(0.42f, 0.36f, 0.28f), 0f);
        Prop(z1Root, "Melee_Wall_N", new Vector3(-84f, 1.5f, -33f), new Vector3(10f, 3f, 0.8f), WallColor(0.42f, 0.36f, 0.28f), 0f);
        Prop(z1Root, "Explore_W", new Vector3(-66f, 1.7f, -42f), new Vector3(0.7f, 3.4f, 12f), WallColor(0.42f, 0.36f, 0.28f), 0f);
        Prop(z1Root, "Explore_N", new Vector3(-58f, 1.6f, -34f), new Vector3(12f, 3.2f, 0.7f), WallColor(0.42f, 0.36f, 0.28f), 0f);
        Prop(z1Root, "Mix_L", new Vector3(-93f, 2f, -14f), new Vector3(0.8f, 4f, 16f), WallColor(0.42f, 0.36f, 0.28f), 0f);
        Prop(z1Root, "Mix_Broken", new Vector3(-72f, 1.4f, -5f), new Vector3(12f, 2.8f, 0.7f), WallColor(0.42f, 0.36f, 0.28f), -12f);
        Prop(z1Root, "Tank_N", new Vector3(-36f, 1.6f, -2f), new Vector3(14f, 3.2f, 0.8f), WallColor(0.42f, 0.36f, 0.28f), 0f);
        Prop(z1Root, "Ravine_L", new Vector3(-44f, 2.2f, -48f), new Vector3(1.2f, 4.4f, 18f), WallColor(0.42f, 0.36f, 0.28f), 8f);
        Prop(z1Root, "Ravine_R", new Vector3(-36f, 2.0f, -48f), new Vector3(1.2f, 4f, 16f), WallColor(0.42f, 0.36f, 0.28f), 8f);

        Prop(z2Root, "PlazaRuin_SW", new Vector3(-2f, 1.8f, 28f), new Vector3(10f, 3.6f, 0.9f), WallColor(0.28f, 0.38f, 0.30f), 12f);
        Prop(z2Root, "PlazaRuin_NE", new Vector3(58f, 2.1f, 72f), new Vector3(8f, 4.2f, 1f), WallColor(0.28f, 0.38f, 0.30f), -20f);
        Prop(z2Root, "AlleyCliff_N", new Vector3(80f, 4f, 64f), new Vector3(10f, 8f, 2.2f), WallColor(0.28f, 0.38f, 0.30f), 0f);
        Prop(z2Root, "AlleyCliff_S", new Vector3(80f, 3.5f, 30f), new Vector3(10f, 7f, 2.2f), WallColor(0.28f, 0.38f, 0.30f), 0f);
        Prop(z2Root, "AlleyCliff_E", new Vector3(86f, 4.2f, 48f), new Vector3(2f, 8.4f, 22f), WallColor(0.28f, 0.38f, 0.30f), 0f);
        Prop(z2Root, "NorthRuin_W", new Vector3(18f, 2.2f, 92f), new Vector3(0.8f, 4.4f, 12f), WallColor(0.28f, 0.38f, 0.30f), 0f);
        Prop(z2Root, "NorthRuin_E", new Vector3(38f, 2.0f, 92f), new Vector3(0.8f, 4f, 10f), WallColor(0.28f, 0.38f, 0.30f), 0f);

        Prop(z3Root, "HQCliff_N", new Vector3(150f, 6f, 104f), new Vector3(70f, 12f, 6f), WallColor(0.18f, 0.11f, 0.11f), 0f);
        Prop(z3Root, "HQCliff_S", new Vector3(150f, 5.5f, 8f), new Vector3(64f, 11f, 6f), WallColor(0.18f, 0.11f, 0.11f), 0f);
        Prop(z3Root, "HQCliff_E", new Vector3(222f, 7f, 50f), new Vector3(6f, 14f, 70f), WallColor(0.18f, 0.11f, 0.11f), 0f);

        EnsureWilderness(wildernessParent);
    }

    void EnsureWilderness(Transform wildernessParent)
    {
        if (GameObject.Find("Wilderness") != null)
            return;

        var wild = new GameObject("Wilderness");
        wild.transform.SetParent(wildernessParent, false);
        Vector3[] trees =
        {
            new Vector3(-120f, 0f, -50f), new Vector3(-108f, 0f, -20f), new Vector3(-125f, 0f, 10f),
            new Vector3(-95f, 0f, 40f), new Vector3(-140f, 0f, -80f), new Vector3(-60f, 0f, -110f),
            new Vector3(-20f, 0f, -100f), new Vector3(20f, 0f, -80f), new Vector3(50f, 0f, -55f),
            new Vector3(90f, 0f, -30f), new Vector3(-10f, 0f, 120f), new Vector3(40f, 0f, 130f),
            new Vector3(80f, 0f, 118f), new Vector3(-70f, 0f, 80f), new Vector3(-100f, 0f, 70f),
            new Vector3(-150f, 0f, 30f), new Vector3(8f, 0f, -120f), new Vector3(70f, 0f, 150f),
            new Vector3(-40f, 0f, 140f), new Vector3(-160f, 0f, -40f)
        };
        for (int i = 0; i < trees.Length; i++)
            Tree(wild.transform, i, trees[i]);

        Vector3[] rocks =
        {
            new Vector3(-100f, 0.8f, -60f), new Vector3(-50f, 0.9f, -90f), new Vector3(-10f, 1.1f, -40f),
            new Vector3(12f, 0.8f, 8f), new Vector3(55f, 1.0f, 20f), new Vector3(70f, 1.2f, 80f),
            new Vector3(-30f, 0.9f, 30f), new Vector3(-90f, 1.0f, 20f), new Vector3(0f, 0.7f, 90f),
            new Vector3(45f, 1.3f, 110f), new Vector3(-15f, 0.8f, -15f)
        };
        for (int i = 0; i < rocks.Length; i++)
        {
            float s = 2.4f + (i % 4) * 0.7f;
            Prop(wild.transform, "Rock_" + i, rocks[i] + Vector3.up * (s * 0.25f), new Vector3(s, s * 0.7f, s * 0.85f), WallColor(0.32f, 0.28f, 0.24f), i * 17f);
        }
    }

    static Color WallColor(float r, float g, float b) => new Color(r, g, b);

    static void Tree(Transform parent, int index, Vector3 pos)
    {
        Prop(parent, "TreeTrunk_" + index, pos + new Vector3(0f, 2.1f, 0f), new Vector3(0.45f, 4.2f, 0.45f), new Color(0.28f, 0.18f, 0.10f), 0f, false);
        Prop(parent, "TreeCanopy_" + index, pos + new Vector3(0f, 4.6f, 0f), new Vector3(2.6f, 2.4f, 2.6f), new Color(0.22f, 0.38f, 0.18f), 0f, false);
    }

    static void Prop(Transform parent, string name, Vector3 pos, Vector3 size, Color color, float yaw, bool wall = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = size;
        go.isStatic = true;
        go.layer = 7;
        if (wall) go.tag = "Wall";

        var renderer = go.GetComponent<Renderer>();
        if (renderer)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = color;
                renderer.sharedMaterial = mat;
            }
        }
    }

    static void ApplyAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.52f, 0.55f, 0.45f);
        RenderSettings.fogDensity = 0.009f;

        var light = FindAnyObjectByType<Light>();
        if (light && light.type == LightType.Directional)
        {
            light.color = new Color(1f, 0.92f, 0.78f);
            light.intensity = 1.45f;
            light.shadows = LightShadows.Soft;
        }

        var cam = Camera.main;
        if (cam)
            cam.farClipPlane = 600f;
    }
}
