using UnityEngine;

// Schedules ambient scares: sounds, light flickers and brief silhouette appearances.
public class HorrorEventManager : MonoBehaviour
{
    Transform player;
    Camera cam;
    HorrorEntity entity;
    FlickeringLight ceiling;
    PlayerController pc;
    Vector3 spotFar, spotWardrobe, pendingSpot;
    float timer = 15f, pendingTimeout;
    bool firstCreak, firstFlicker, pending;

    public void Init(Transform p, Camera c, HorrorEntity e, FlickeringLight l, PlayerController controller, Vector3 far, Vector3 wardrobe)
    {
        player = p; cam = c; entity = e; ceiling = l; pc = controller; spotFar = far; spotWardrobe = wardrobe;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || entity == null) return;
        float dt = Time.deltaTime, t = gm.PlayTime;

        if (!firstCreak && t > 12f) { firstCreak = true; AudioManager.SfxAt("creak", RandomAround(5f), 0.7f); }
        if (!firstFlicker && t > 26f) { firstFlicker = true; ceiling.Burst(0.7f); }

        timer -= dt;
        if (timer <= 0f) { Fire(); timer = NextInterval(); }

        if (pending)
        {
            pendingTimeout -= dt;
            if (pendingTimeout <= 0f) pending = false;
            else if (InView(pendingSpot + Vector3.up * 1.2f, 0.82f))
            {
                pending = false;
                ceiling.Burst(0.35f);
                entity.Glimpse(pendingSpot, Random.Range(1.3f, 2.2f));
            }
        }
    }

    float NextInterval()
    {
        switch (entity.State)
        {
            case EntityState.Dormant: return Random.Range(14f, 22f);
            case EntityState.Watching: return Random.Range(9f, 15f);
            case EntityState.Stalking: return Random.Range(7f, 12f);
            default: return Random.Range(10f, 16f);
        }
    }

    void Fire()
    {
        int stage = (int)entity.State;
        float r = Random.value;
        if (stage >= 1 && stage <= 2 && !pending && r < 0.35f)
        {
            pendingSpot = Random.value < 0.5f ? spotFar : spotWardrobe;
            pendingTimeout = 25f;
            pending = true;
        }
        else if (r < 0.55f) AudioManager.SfxAt("whisper", RandomAround(4f), 0.55f);
        else if (r < 0.75f) AudioManager.SfxAt("creak", RandomAround(5f), 0.7f);
        else if (r < 0.88f) AudioManager.SfxAt("knock", RandomAround(5f), 0.8f);
        else ceiling.Burst(Random.Range(0.4f, 1f));
        if (stage >= 2 && Random.value < 0.4f) pc.Shake(0.03f, 0.5f);
    }

    public void OnKeyCollected(int keys)
    {
        timer = Mathf.Min(timer, 5f);
        ceiling.Burst(0.5f);
        pc.Shake(0.05f, 0.4f);
    }

    public void Spook()
    {
        ceiling.Burst(0.6f);
        AudioManager.SfxAt("whisper", RandomAround(3f), 0.7f);
        pc.Shake(0.03f, 0.3f);
    }

    Vector3 RandomAround(float radius)
    {
        Vector3 p = player.position + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * radius + Vector3.up * 1.2f;
        p.x = Mathf.Clamp(p.x, -3.8f, 3.8f);
        p.z = Mathf.Clamp(p.z, -4.8f, 4.8f);
        return p;
    }

    bool InView(Vector3 point, float threshold)
    {
        Vector3 d = point - cam.transform.position;
        float dist = d.magnitude;
        if (dist < 3f) return false;
        d /= dist;
        if (Vector3.Dot(cam.transform.forward, d) < threshold) return false;
        return !Physics.Raycast(cam.transform.position, d, dist - 0.3f, ~0, QueryTriggerInteraction.Ignore);
    }
}
