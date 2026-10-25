using UnityEngine;
using UnityEngine.Rendering;

// Creates materials at runtime for Built-in or URP projects.
public static class Mats
{
    static Shader lit;

    static Shader Lit
    {
        get { if (lit == null) lit = FindLit(); return lit; }
    }

    static Shader FindLit()
    {
        Shader s = null;
        if (GraphicsSettings.currentRenderPipeline != null) s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Sprites/Default");
        return s;
    }

    public static Material Make(Color color, float smooth = 0.1f, Color? emission = null, Texture tex = null, Vector2? tiling = null)
    {
        var m = new Material(Lit);
        m.color = color;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (tex != null)
        {
            m.mainTexture = tex;
            if (tiling.HasValue) m.mainTextureScale = tiling.Value;
        }
        return m;
    }

    public static Texture2D Noise(int size, Color a, Color b, float freq, int planks, int seed)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, true);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(seed * 13.1f + x * freq / size, seed * 7.7f + y * freq / size);
                float n2 = Mathf.PerlinNoise(seed + x * freq * 4f / size, seed + y * freq * 0.5f / size);
                float v = Mathf.Clamp01(n * 0.7f + n2 * 0.3f);
                Color c = Color.Lerp(a, b, v);
                if (planks > 0)
                {
                    int w = size / planks;
                    int p = x / w;
                    float shade = 0.85f + 0.15f * Mathf.PerlinNoise(p * 3.3f + seed, 0.5f);
                    c = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
                    if (x % w < 2) c = new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, 1f);
                }
                c.a = 1f;
                px[y * size + x] = c;
            }
        }
        t.SetPixels(px);
        t.wrapMode = TextureWrapMode.Repeat;
        t.Apply(true);
        return t;
    }
}
