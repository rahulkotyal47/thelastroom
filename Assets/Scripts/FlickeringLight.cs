using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    Light lamp;
    Material bulbMat;
    Color emission;
    bool idleFlicker;
    float baseIntensity, burst, blackout, dipLeft, dipTimer, seed;

    public void Init(Light l, Renderer bulbRenderer, Color bulbEmission, bool idle)
    {
        lamp = l;
        bulbMat = bulbRenderer != null ? bulbRenderer.material : null;
        emission = bulbEmission;
        idleFlicker = idle;
        baseIntensity = l != null ? l.intensity : 1f;
        seed = Random.value * 100f;
        dipTimer = Random.Range(3f, 7f);
    }

    public void Burst(float seconds) { burst = Mathf.Max(burst, seconds); }
    public void Blackout(float seconds) { blackout = Mathf.Max(blackout, seconds); }

    void Update()
    {
        if (lamp == null) return;
        float dt = Time.deltaTime;
        float f = 1f;
        if (blackout > 0f)
        {
            blackout -= dt;
            f = (blackout < 0.5f && Random.value < 0.25f) ? 0.5f : 0f;
        }
        else if (burst > 0f)
        {
            burst -= dt;
            f = Random.value > 0.5f ? 1f : 0.05f;
        }
        else if (idleFlicker)
        {
            dipTimer -= dt;
            if (dipTimer <= 0f) { dipLeft = Random.Range(0.05f, 0.22f); dipTimer = Random.Range(4f, 10f); }
            if (dipLeft > 0f) { dipLeft -= dt; f = Random.Range(0.1f, 0.55f); }
            else f = 0.86f + 0.14f * Mathf.PerlinNoise(Time.time * 2.5f, seed);
        }
        lamp.intensity = baseIntensity * f;
        if (bulbMat != null) bulbMat.SetColor("_EmissionColor", emission * f);
    }
}
