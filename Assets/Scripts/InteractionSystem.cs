using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    public float distance = 2.6f;
    Camera cam;

    public string CurrentPrompt { get; private set; }

    public void Init(Camera c) { cam = c; }

    void Update()
    {
        CurrentPrompt = null;
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || cam == null) return;

        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, distance, ~0, QueryTriggerInteraction.Collide))
        {
            var it = hit.collider.GetComponentInParent<IInteractable>();
            if (it != null && it.CanInteract)
            {
                CurrentPrompt = it.Prompt;
                if (GameInput.InteractPressed) it.Interact();
            }
        }
    }
}
