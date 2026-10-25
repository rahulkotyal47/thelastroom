using System.Collections.Generic;
using UnityEngine;

// All sounds are generated in code. To use your own, put clips in Assets/Resources/Audio
// named: ambient, creak, knock, whisper, click, pickup, locked, door, scare, victory, footstep.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    const int SR = 22050;
    const float TAU = 6.2831853f;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    AudioSource ambient, sfx;

    public static void Sfx(string name, float vol = 1f) { if (Instance != null) Instance.Play(name, vol); }
    public static void SfxAt(string name, Vector3 pos, float vol = 1f) { if (Instance != null) Instance.PlayAt(name, pos, vol); }

    void Awake()
    {
        Instance = this;
        ambient = gameObject.AddComponent<AudioSource>();
        ambient.loop = true; ambient.spatialBlend = 0f; ambient.volume = 0.45f; ambient.playOnAwake = false;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0f; sfx.playOnAwake = false;
        BuildClips();
        ambient.clip = Get("ambient");
        if (ambient.clip != null) ambient.Play();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    AudioClip Get(string n) { AudioClip c; return clips.TryGetValue(n, out c) ? c : null; }

    public void Play(string name, float vol = 1f)
    {
        var c = Get(name);
        if (c != null && sfx != null) sfx.PlayOneShot(c, vol);
    }

    public void PlayAt(string name, Vector3 pos, float vol = 1f)
    {
        var c = Get(name);
        if (c == null) return;
        var g = new GameObject("Sfx_" + name);
        g.transform.position = pos;
        var s = g.AddComponent<AudioSource>();
        s.clip = c; s.volume = vol; s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 1.5f; s.maxDistance = 16f;
        s.Play();
        Destroy(g, c.length + 0.2f);
    }

    static float Noise() { return Random.value * 2f - 1f; }
    static float Edge(float t, float len) { return Mathf.Clamp01(Mathf.Min(t, len - t) / 0.1f); }

    AudioClip Gen(string name, float seconds, System.Func<float, float> f)
    {
        int n = Mathf.CeilToInt(seconds * SR);
        var d = new float[n];
        for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)SR), -0.95f, 0.95f);
        var c = AudioClip.Create(name, n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    void Add(string name, System.Func<AudioClip> make)
    {
        var ext = Resources.Load<AudioClip>("Audio/" + name);
        clips[name] = ext != null ? ext : make();
    }

    void BuildClips()
    {
        Add("ambient", () =>
        {
            float lp = 0f, lp2 = 0f;
            return Gen("ambient", 8f, t =>
            {
                lp += (Noise() - lp) * 0.015f;
                lp2 += (lp - lp2) * 0.04f;
                float hum = Mathf.Sin(TAU * 55f * t) * 0.05f + Mathf.Sin(TAU * 110f * t) * 0.018f;
                float swell = 0.65f + 0.35f * Mathf.Sin(TAU * t / 8f);
                return (hum + lp * 1.4f * swell + lp2 * 0.9f) * Edge(t, 8f);
            });
        });

        Add("creak", () =>
        {
            float ph = 0f;
            return Gen("creak", 1.0f, t =>
            {
                ph += TAU * (150f - 70f * t) / SR;
                float saw = (ph / TAU) % 1f * 2f - 1f;
                float env = Mathf.Sin(Mathf.PI * t);
                float flutter = 0.5f + 0.5f * Mathf.Sin(TAU * 13f * t);
                return saw * 0.28f * env * flutter + Noise() * 0.04f * env;
            });
        });

        Add("knock", () => Gen("knock", 1.0f, t =>
        {
            float o = 0f;
            float[] starts = { 0f, 0.22f, 0.5f };
            for (int i = 0; i < starts.Length; i++)
            {
                if (t < starts[i]) continue;
                float e = Mathf.Exp(-(t - starts[i]) * 25f);
                o += (Mathf.Sin(TAU * 95f * t) * 0.6f + Noise() * 0.25f) * e;
            }
            return o;
        }));

        Add("whisper", () =>
        {
            float a = 0f, b = 0f;
            return Gen("whisper", 2.0f, t =>
            {
                a += (Noise() - a) * 0.5f;
                b += (a - b) * 0.08f;
                float bp = a - b;
                float e = Mathf.Pow(Mathf.Abs(Mathf.Sin(TAU * 3.1f * t)), 2f) * (0.55f + 0.45f * Mathf.Sin(TAU * 0.7f * t + 1f)) * Mathf.Sin(Mathf.PI * t / 2f);
                return bp * 2.2f * e;
            });
        });

        Add("click", () => Gen("click", 0.06f, t => (Noise() * 0.3f + Mathf.Sin(TAU * 1800f * t) * 0.3f) * Mathf.Exp(-t * 90f)));

        Add("pickup", () => Gen("pickup", 0.6f, t =>
        {
            float o = Mathf.Sin(TAU * 880f * t) * Mathf.Exp(-t * 7f);
            if (t > 0.1f) o += Mathf.Sin(TAU * 1320f * t) * Mathf.Exp(-(t - 0.1f) * 8f);
            return o * 0.35f;
        }));

        Add("locked", () => Gen("locked", 0.5f, t =>
        {
            float o = 0f;
            float[] starts = { 0f, 0.15f };
            for (int i = 0; i < starts.Length; i++)
            {
                if (t < starts[i]) continue;
                float e = Mathf.Exp(-(t - starts[i]) * 30f);
                o += (Mathf.Sin(TAU * 600f * t) * 0.3f + Noise() * 0.3f) * e;
            }
            return o;
        }));

        Add("door", () =>
        {
            float ph = 0f;
            return Gen("door", 1.8f, t =>
            {
                ph += TAU * (90f - 30f * t / 1.8f) / SR;
                float saw = (ph / TAU) % 1f * 2f - 1f;
                float env = Mathf.Sin(Mathf.PI * t / 1.8f);
                float flutter = 0.5f + 0.5f * Mathf.Sin(TAU * 9f * t);
                float thud = Mathf.Sin(TAU * 60f * t) * Mathf.Exp(-t * 20f) * 0.6f;
                return saw * 0.25f * env * flutter + thud;
            });
        });

        Add("scare", () =>
        {
            float ph = 0f;
            return Gen("scare", 1.4f, t =>
            {
                ph += TAU * (1600f - 1000f * Mathf.Min(t, 1f)) / SR;
                float env = t < 0.02f ? t / 0.02f : Mathf.Exp(-(t - 0.02f) * 2.2f);
                float tone = Mathf.Sin(ph) + 0.5f * Mathf.Sin(ph * 2.01f);
                float boom = Mathf.Sin(TAU * 48f * t) * Mathf.Exp(-t * 3f);
                return (tone * 0.3f + Noise() * 0.25f + boom * 0.6f) * env * 0.7f;
            });
        });

        Add("victory", () => Gen("victory", 3.0f, t =>
        {
            float env = Mathf.Sqrt(Mathf.Max(0f, Mathf.Sin(Mathf.PI * t / 3f)));
            float o = Mathf.Sin(TAU * 220f * t) + Mathf.Sin(TAU * 277f * t) + Mathf.Sin(TAU * 330f * t) + Mathf.Sin(TAU * 440f * t);
            return o * 0.1f * env;
        }));

        Add("footstep", () =>
        {
            float lp = 0f;
            return Gen("footstep", 0.15f, t =>
            {
                lp += (Noise() - lp) * 0.2f;
                return Mathf.Sin(TAU * 70f * t) * Mathf.Exp(-t * 35f) * 0.7f + lp * Mathf.Exp(-t * 50f) * 0.4f;
            });
        });
    }
}
