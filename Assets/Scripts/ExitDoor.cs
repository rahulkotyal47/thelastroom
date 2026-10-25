using System.Collections;
using UnityEngine;

public class ExitDoor : MonoBehaviour, IInteractable
{
    Renderer lampRenderer;
    Light lampLight, outsideLight;
    bool unlocked, opened;

    public string Prompt
    {
        get
        {
            var gm = GameManager.Instance;
            return (gm != null && gm.Keys >= GameManager.TotalKeys) ? "Press E to unlock door" : "Press E to try the door";
        }
    }
    public bool CanInteract { get { return !opened; } }

    public void Setup(Renderer lamp, Light lampL, Light outside)
    {
        lampRenderer = lamp; lampLight = lampL; outsideLight = outside;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (!unlocked && gm != null && gm.Keys >= GameManager.TotalKeys) Unlock();
    }

    void Unlock()
    {
        unlocked = true;
        var green = new Color(0.1f, 1f, 0.2f);
        if (lampRenderer != null)
        {
            lampRenderer.material.SetColor("_EmissionColor", green * 2f);
            lampRenderer.material.color = green * 0.5f;
        }
        if (lampLight != null) lampLight.color = green;
        AudioManager.Sfx("click", 0.8f);
    }

    public void Interact()
    {
        var gm = GameManager.Instance;
        if (gm == null || opened) return;
        if (gm.Keys < GameManager.TotalKeys)
        {
            int remaining = GameManager.TotalKeys - gm.Keys;
            gm.ShowMessage(remaining == 1 ? "The door is locked. 1 key remains." : "The door is locked. " + remaining + " keys remain.", 3.5f);
            AudioManager.Sfx("locked", 0.8f);
            return;
        }
        opened = true;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        gm.Escape();
    }

    public void Open() { StartCoroutine(OpenRoutine()); }

    IEnumerator OpenRoutine()
    {
        AudioManager.Sfx("door", 0.9f);
        if (outsideLight != null) outsideLight.enabled = true;
        float t = 0f; const float dur = 1.8f;
        Quaternion a = transform.localRotation, b = a * Quaternion.Euler(0f, -100f, 0f);
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(a, b, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
    }
}
