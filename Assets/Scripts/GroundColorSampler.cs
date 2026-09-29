using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 取得射線打到的地面「看起來是什麼顏色」，給塵土之類的特效用。
///
///   Terrain  - 依接觸點的圖層權重（alphamap）混合各 Terrain Layer 的平均色
///   其他物件 - Renderer 材質的 Base Color × Base Map 平均色
///
/// 貼圖平均色用 GPU 縮圖取得（不需要開 Read/Write），每張貼圖只算一次並快取。
/// 第一次讀某張貼圖會有一次極短的 GPU 同步，所以 PlayerJumpDust 開場會先預熱 Terrain 的圖層。
/// </summary>
public static class GroundColorSampler
{
    private static readonly Dictionary<Texture, Color> s_TextureAverage = new Dictionary<Texture, Color>();
    private static readonly Dictionary<Material, Color> s_MaterialColor = new Dictionary<Material, Color>();

    private const int SampleSize = 8;   // 縮到 8×8 再平均；Blit 會自動取小 mip，等於整張平均

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_TextureAverage.Clear();
        s_MaterialColor.Clear();
    }

    /// <summary>取得地面顏色。取不到（沒有 Renderer、沒有材質…）時回傳 false。</summary>
    public static bool TrySample(RaycastHit hit, out Color color)
    {
        color = Color.white;
        Collider col = hit.collider;
        if (col == null) return false;

        if (col is TerrainCollider)
        {
            Terrain terrain = col.GetComponent<Terrain>();
            return terrain != null && TrySampleTerrain(terrain, hit.point, out color);
        }

        Renderer r = col.GetComponent<Renderer>();
        if (r == null) r = col.GetComponentInParent<Renderer>();
        if (r == null) return false;

        Material m = r.sharedMaterial;
        if (m == null) return false;

        color = MaterialColor(m);
        return true;
    }

    /// <summary>依接觸點的圖層權重混合 Terrain Layer 顏色。</summary>
    public static bool TrySampleTerrain(Terrain terrain, Vector3 worldPos, out Color color)
    {
        color = Color.white;
        TerrainData td = terrain.terrainData;
        if (td == null) return false;

        TerrainLayer[] layers = td.terrainLayers;
        if (layers == null || layers.Length == 0) return false;

        Vector3 local = worldPos - terrain.GetPosition();
        int w = td.alphamapWidth, h = td.alphamapHeight;
        if (w <= 0 || h <= 0) return false;

        int ax = Mathf.Clamp(Mathf.RoundToInt(local.x / td.size.x * (w - 1)), 0, w - 1);
        int az = Mathf.Clamp(Mathf.RoundToInt(local.z / td.size.z * (h - 1)), 0, h - 1);

        float[,,] weights = td.GetAlphamaps(ax, az, 1, 1);   // 只在跳躍 / 落地時呼叫，頻率很低
        int n = Mathf.Min(layers.Length, weights.GetLength(2));

        Color sum = Color.clear;
        float total = 0f;
        for (int i = 0; i < n; i++)
        {
            float wi = weights[0, 0, i];
            if (wi <= 0.001f || layers[i] == null) continue;
            sum += LayerColor(layers[i]) * wi;
            total += wi;
        }

        if (total <= 0f) return false;
        color = sum / total;
        color.a = 1f;
        return true;
    }

    /// <summary>先把場上 Terrain 用到的貼圖平均色算好，避免第一次跳躍時才讀 GPU。</summary>
    public static void PrewarmActiveTerrains()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        for (int t = 0; t < terrains.Length; t++)
        {
            TerrainData td = terrains[t] != null ? terrains[t].terrainData : null;
            if (td == null || td.terrainLayers == null) continue;
            foreach (TerrainLayer layer in td.terrainLayers)
                if (layer != null) LayerColor(layer);
        }
    }

    private static Color LayerColor(TerrainLayer layer)
    {
        Color c = layer.diffuseTexture != null ? TextureAverage(layer.diffuseTexture) : Color.white;
        Vector4 remap = layer.diffuseRemapMax;   // Terrain Layer 上的 Tint（預設 1,1,1,1）
        c.r *= remap.x;
        c.g *= remap.y;
        c.b *= remap.z;
        c.a = 1f;
        return c;
    }

    private static Color MaterialColor(Material m)
    {
        if (s_MaterialColor.TryGetValue(m, out Color cached)) return cached;

        Color tint = Color.white;
        if (m.HasProperty("_BaseColor")) tint = m.GetColor("_BaseColor");
        else if (m.HasProperty("_Color")) tint = m.GetColor("_Color");

        Texture tex = null;
        if (m.HasProperty("_BaseMap")) tex = m.GetTexture("_BaseMap");
        if (tex == null && m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");

        Color c = tint;
        if (tex != null) c *= TextureAverage(tex);
        c.a = 1f;

        s_MaterialColor[m] = c;
        return c;
    }

    /// <summary>貼圖的平均顏色（sRGB / Gamma 空間，跟 ParticleSystem 的 startColor 一致）。</summary>
    public static Color TextureAverage(Texture tex)
    {
        if (tex == null) return Color.white;
        if (s_TextureAverage.TryGetValue(tex, out Color cached)) return cached;

        Color result = Color.white;
        if (tex.dimension == TextureDimension.Tex2D)
        {
            var desc = new RenderTextureDescriptor(SampleSize, SampleSize, RenderTextureFormat.ARGB32, 0)
            {
                sRGB = true,        // 寫回 sRGB，讀出來的位元組就是一般看到的顏色
                useMipMap = false,
            };
            RenderTexture rt = RenderTexture.GetTemporary(desc);
            RenderTexture prev = RenderTexture.active;

            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;

            var read = new Texture2D(SampleSize, SampleSize, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, SampleSize, SampleSize), 0, 0, false);
            read.Apply(false);

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            Color32[] px = read.GetPixels32();
            float r = 0f, g = 0f, b = 0f;
            for (int i = 0; i < px.Length; i++)
            {
                r += px[i].r;
                g += px[i].g;
                b += px[i].b;
            }
            float inv = 1f / (px.Length * 255f);
            result = new Color(r * inv, g * inv, b * inv, 1f);

            Object.Destroy(read);
        }

        s_TextureAverage[tex] = result;
        return result;
    }
}
