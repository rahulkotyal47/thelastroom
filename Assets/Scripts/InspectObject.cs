using UnityEngine;

public class InspectObject : MonoBehaviour, IInteractable
{
    public string text = "Nothing unusual.";
    public bool spook;
    bool used;

    public string Prompt { get { return "Press E to inspect"; } }
    public bool CanInteract { get { return true; } }

    public void Interact()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.ShowMessage(text, 4.5f);
        AudioManager.Sfx("click", 0.3f);
        if (spook && !used)
        {
            used = true;
            if (gm.Events != null) gm.Events.Spook();
        }
    }
}
