using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Transform cameraRoot;
    public float walkSpeed = 3f;
    public float runSpeed = 5.2f;
    public float acceleration = 14f;
    public float gravity = -20f;
    public float eyeHeight = 1.6f;

    public float Stamina01 { get; private set; }
    public bool IsSprinting { get; private set; }

    CharacterController cc;
    float yaw, pitch, vy, bobTimer, stepSin, shakeMag, shakeTime, shakeDur, bobX, bobY;
    Vector3 vel;
    bool frozen, exhausted;

    void Awake() { cc = GetComponent<CharacterController>(); Stamina01 = 1f; }

    public void Teleport(Vector3 pos, float yawDeg)
    {
        cc.enabled = false;
        transform.position = pos;
        yaw = yawDeg; pitch = 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cc.enabled = true;
        vel = Vector3.zero; vy = 0f;
    }

    public void Freeze() { frozen = true; vel = Vector3.zero; }

    public void Shake(float magnitude, float duration)
    {
        shakeMag = magnitude; shakeDur = Mathf.Max(0.05f, duration); shakeTime = shakeDur;
    }

    public IEnumerator LookAtRoutine(Vector3 target, float duration)
    {
        Vector3 from = transform.position + Vector3.up * eyeHeight;
        Vector3 d = target - from;
        float ty = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        float tp = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
        float sy = yaw, sp = pitch, t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            yaw = Mathf.LerpAngle(sy, ty, k);
            pitch = Mathf.Clamp(Mathf.Lerp(sp, tp, k), -80f, 80f);
            yield return null;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || cc == null || !cc.enabled) return;

        var gm = GameManager.Instance;
        bool active = gm != null && gm.State == GameState.Playing && !frozen;
        Vector2 look = Vector2.zero, move = Vector2.zero;
        bool sprint = false;
        if (active)
        {
            look = GameInput.LookDelta * GameSettings.Sensitivity;
            move = GameInput.Move;
            sprint = GameInput.Sprint;
        }

        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        bool moving = move.sqrMagnitude > 0.01f;
        IsSprinting = sprint && moving && move.y > -0.1f && !exhausted && Stamina01 > 0f;
        if (IsSprinting)
        {
            Stamina01 = Mathf.Max(0f, Stamina01 - dt / 4.5f);
            if (Stamina01 <= 0f) exhausted = true;
        }
        else
        {
            Stamina01 = Mathf.Min(1f, Stamina01 + dt / 7f);
            if (exhausted && Stamina01 > 0.3f) exhausted = false;
        }

        Vector3 wish = transform.right * move.x + transform.forward * move.y;
        if (wish.sqrMagnitude > 1f) wish.Normalize();
        float speed = IsSprinting ? runSpeed : walkSpeed;
        vel = Vector3.MoveTowards(vel, wish * speed, acceleration * dt);
        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy += gravity * dt;
        cc.Move((vel + Vector3.up * vy) * dt);

        float hs = new Vector2(vel.x, vel.z).magnitude;
        if (cc.isGrounded && hs > 0.3f)
        {
            bobTimer += dt * (IsSprinting ? 11f : 7.5f);
            float s = Mathf.Sin(bobTimer);
            bobY = s * 0.02f;
            bobX = Mathf.Cos(bobTimer * 0.5f) * 0.01f;
            if (stepSin > 0f && s <= 0f) AudioManager.Sfx("footstep", IsSprinting ? 0.45f : 0.28f);
            stepSin = s;
        }
        else
        {
            bobY = Mathf.Lerp(bobY, 0f, dt * 8f);
            bobX = Mathf.Lerp(bobX, 0f, dt * 8f);
        }

        Vector3 so = Vector3.zero;
        float roll = 0f;
        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            float k = Mathf.Clamp01(shakeTime / shakeDur);
            so = Random.insideUnitSphere * shakeMag * k;
            roll = Random.Range(-1f, 1f) * shakeMag * 40f * k;
        }
        if (cameraRoot != null)
        {
            cameraRoot.localPosition = new Vector3(bobX, eyeHeight + bobY, 0f) + so;
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }
    }
}
