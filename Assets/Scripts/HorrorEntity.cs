using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityState { Dormant = 0, Watching = 1, Stalking = 2, Attacking = 3 }

// The entity never uses a navmesh. It only moves while unobserved (Stalking),
// steers around obstacles with capsule checks, and can only appear on free floor spots.
public class HorrorEntity : MonoBehaviour
{
    public EntityState State { get; private set; }
    public bool IsVisible { get { return visual != null && visual.activeSelf; } }
    public Vector3 HeadPosition { get { return visual.transform.position + Vector3.up * 1.9f; } }

    [SerializeField] float stalkSpeed = 1.4f;
    [SerializeField] float attackSpeed = 2.4f;
    [SerializeField] float catchDistance = 1.0f;

    GameObject visual;
    Transform player;
    Camera cam;
    FlickeringLight ceiling;
    FlashlightController flash;
    Vector3 doorApproach, spotFar;
    readonly List<Vector3> spots = new List<Vector3>();
    readonly Collider[] hits = new Collider[16];
    static readonly float[] anglesPos = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };
    static readonly float[] anglesNeg = { 0f, -35f, 35f, -70f, 70f, -110f, 110f };

    float glimpseTimer, spawnDelay, observedTime, stuckTime, grace, biasTimer, stepDist, flickerCooldown;
    bool spawnPending;
    int bias;

    public void Init(GameObject visualObj, Transform p, Camera c, FlickeringLight l, FlashlightController f, Vector3 far, Vector3 door)
    {
        visual = visualObj; player = p; cam = c; ceiling = l; flash = f; spotFar = far; doorApproach = door;
        State = EntityState.Dormant;
        visual.SetActive(false);
    }

    void Start()
    {
        Physics.SyncTransforms();
        for (float x = -3.4f; x <= 3.41f; x += 0.6f)
            for (float z = -4.4f; z <= 4.41f; z += 0.6f)
            {
                var p = new Vector3(x, 0f, z);
                if (!Blocked(p, 0.4f)) spots.Add(p);
            }
    }

    bool Blocked(Vector3 p, float radius)
    {
        int n = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * 0.45f, p + Vector3.up * 1.6f, radius, hits, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (!hits[i].transform.IsChildOf(player)) return true;
        return false;
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || visual == null || gm.State != GameState.Playing) return;
        float dt = Time.deltaTime;

        UpdateStage(gm);

        if (spawnPending)
        {
            spawnDelay -= dt;
            if (spawnDelay <= 0f) { spawnPending = false; SpawnForStalk(); }
        }

        if (State <= EntityState.Watching)
        {
            if (visual.activeSelf)
            {
                glimpseTimer -= dt;
                FacePlayer(); Sway();
                if (glimpseTimer <= 0f) { visual.SetActive(false); ceiling.Burst(0.2f); }
            }
            return;
        }

        if (!visual.activeSelf) return;
        FacePlayer(); Sway();
        if (grace > 0f) { grace -= dt; return; }

        Vector3 to = Flat(player.position - visual.transform.position);
        float d = to.magnitude;
        if (d <= catchDistance) { gm.PlayerCaught(); return; }

        bool observed = IsObserved();
        float speed = State == EntityState.Attacking ? attackSpeed : (observed ? 0f : stalkSpeed);

        if (State == EntityState.Stalking)
        {
            if (observed)
            {
                observedTime += dt;
                if (observedTime > 4f && d > 2.5f) { Vanish(); return; }
            }
            else observedTime = 0f;
        }

        if (speed > 0f) Step(to / d, speed * dt);

        if (d < 5f)
        {
            flickerCooldown -= dt;
            if (flickerCooldown <= 0f) { flash.Flicker(0.35f); flickerCooldown = Random.Range(3f, 6f); }
        }
    }

    void UpdateStage(GameManager gm)
    {
        EntityState target = EntityState.Dormant;
        if (gm.Keys >= 1 || gm.PlayTime > 45f) target = EntityState.Watching;
        if (gm.Keys >= 2 || gm.PlayTime > 210f) target = EntityState.Stalking;
        if (target > State && State != EntityState.Attacking)
        {
            State = target;
            if (State == EntityState.Stalking)
            {
                visual.SetActive(false);
                spawnPending = true; spawnDelay = 6f;
            }
        }
    }

    void Step(Vector3 dir, float dist)
    {
        Vector3 pos = visual.transform.position;
        bool moved = false;
        float[] order = bias >= 0 ? anglesPos : anglesNeg;
        for (int i = 0; i < order.Length; i++)
        {
            float a = order[i];
            Vector3 np = pos + Quaternion.Euler(0f, a, 0f) * dir * dist;
            if (!Blocked(np, 0.28f))
            {
                visual.transform.position = np;
                moved = true;
                if (a > 1f) { bias = 1; biasTimer = 1.2f; }
                else if (a < -1f) { bias = -1; biasTimer = 1.2f; }
                break;
            }
        }
        if (biasTimer > 0f) { biasTimer -= Time.deltaTime; if (biasTimer <= 0f) bias = 0; }

        if (moved)
        {
            stuckTime = 0f;
            stepDist += dist;
            if (stepDist > 0.9f) { stepDist = 0f; AudioManager.SfxAt("footstep", visual.transform.position, 0.6f); }
        }
        else
        {
            stuckTime += Time.deltaTime;
            if (stuckTime > 2.5f) { stuckTime = 0f; Place(PickSpawn(5f)); ceiling.Burst(0.25f); }
        }
    }

    bool IsObserved()
    {
        Vector3 target = visual.transform.position + Vector3.up * 1.2f;
        Vector3 toE = target - cam.transform.position;
        float dist = toE.magnitude;
        if (dist < 0.01f) return true;
        toE /= dist;
        if (Vector3.Dot(cam.transform.forward, toE) < 0.62f) return false;
        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, toE, out hit, dist - 0.4f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(player)) return false;
        }
        return true;
    }

    Vector3 PickSpawn(float minDist)
    {
        if (spots.Count == 0) return spotFar;
        Vector3 pp = player.position;
        Vector3 fwd = Flat(cam.transform.forward).normalized;
        Vector3 best = spots[0];
        float bestScore = -1f, farthest = -1f;
        Vector3 farSpot = spots[0];
        foreach (var s in spots)
        {
            Vector3 to = Flat(s - pp);
            float d = to.magnitude;
            if (d > farthest) { farthest = d; farSpot = s; }
            if (d < minDist) continue;
            if (Vector3.Distance(Flat(s), Flat(doorApproach)) < 3f) continue;
            float dot = d > 0.01f ? Vector3.Dot(fwd, to / d) : 0f;
            float score = d + (dot < 0.3f ? 6f : 0f) + Random.value * 3f;
            if (score > bestScore) { bestScore = score; best = s; }
        }
        return bestScore < 0f ? farSpot : best;
    }

    void SpawnForStalk()
    {
        if (State < EntityState.Stalking) return;
        Place(PickSpawn(5.5f));
        visual.SetActive(true);
        AudioManager.SfxAt("whisper", visual.transform.position, 0.7f);
        ceiling.Burst(0.4f);
    }

    void Vanish()
    {
        visual.SetActive(false);
        ceiling.Burst(0.35f);
        spawnPending = true; spawnDelay = Random.Range(4f, 7f);
        observedTime = 0f;
    }

    void Place(Vector3 pos)
    {
        visual.transform.position = new Vector3(pos.x, 0f, pos.z);
        visual.transform.localScale = Vector3.one;
        FacePlayer();
    }

    void FacePlayer()
    {
        Vector3 d = Flat(player.position - visual.transform.position);
        if (d.sqrMagnitude > 0.001f) visual.transform.rotation = Quaternion.LookRotation(d);
    }

    void Sway() { visual.transform.rotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 5f) * 2.5f); }

    public void Glimpse(Vector3 pos, float duration)
    {
        if (State >= EntityState.Stalking || IsVisible) return;
        Place(pos);
        visual.SetActive(true);
        glimpseTimer = duration;
        AudioManager.SfxAt("whisper", pos, 0.5f);
    }

    public void BeginFinalAppearance()
    {
        State = EntityState.Attacking;
        spawnPending = false;
        visual.SetActive(false);
        StartCoroutine(FinalRoutine());
    }

    IEnumerator FinalRoutine()
    {
        ceiling.Blackout(2.2f);
        flash.Flicker(2f);
        AudioManager.Sfx("knock", 0.9f);
        yield return new WaitForSeconds(1.0f);
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) yield break;
        Place(PickSpawn(6f));
        visual.SetActive(true);
        AudioManager.SfxAt("whisper", visual.transform.position, 0.9f);
        if (gm.Player != null) gm.Player.Shake(0.08f, 0.8f);
        grace = 2.5f;
    }

    public void PrepareJumpScare()
    {
        Vector3 pp = player.position;
        Vector3 to = visual.activeSelf ? Flat(visual.transform.position - pp) : Flat(player.forward);
        if (to.sqrMagnitude < 0.01f) to = Flat(player.forward);
        to.Normalize();
        visual.SetActive(true);
        visual.transform.position = new Vector3(pp.x + to.x * 0.9f, 0f, pp.z + to.z * 0.9f);
        visual.transform.rotation = Quaternion.LookRotation(-to);
        visual.transform.localScale = Vector3.one * 1.12f;
    }

    public void HideNow() { if (visual != null) visual.SetActive(false); }
}
