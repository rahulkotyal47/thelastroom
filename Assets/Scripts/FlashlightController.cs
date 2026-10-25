using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    Light spot;
    bool on = true;
    float baseIntensity, flickerTime;

    public bool IsOn { get { return on; } }

    public void Init(Light l) { spot = l; baseIntensity = l.intensity; }
    public void Flicker(float seconds) { flickerTime = Mathf.Max(flickerTime, seconds); }

    void Update()
    {
        if (spot == null) return;
        var gm = GameManager.Instance;
        if (gm != null && gm.State == GameState.Playing && GameInput.FlashlightPressed)
        {
            on = !on;
            AudioManager.Sfx("click", 0.6f);
        }
        float f = 1f;
        if (flickerTime > 0f)
        {
            flickerTime -= Time.deltaTime;
            f = Random.value > 0.45f ? 1f : 0.05f;
        }
        spot.enabled = on;
        spot.intensity = baseIntensity * f;
    }
}
