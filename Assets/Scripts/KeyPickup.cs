using UnityEngine;

public class KeyPickup : MonoBehaviour, IInteractable
{
    bool collected;

    public string Prompt { get { return "Press E to pick up key"; } }
    public bool CanInteract { get { return !collected; } }

    public void Interact()
    {
        if (collected) return;
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.CollectKey())
        {
            collected = true;
            gameObject.SetActive(false);
        }
    }
}
