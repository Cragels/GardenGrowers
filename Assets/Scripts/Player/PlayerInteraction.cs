using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float playerReach = 3f;
    Interactable currentInteractable;

    [Header("References")]
    [SerializeField] private PlayerInputHandler playerInputHandler;


    void Update()
    {
        HandleInteract();   
    }

    public void HandleInteract()
    {
        CheckInteraction();

        if (playerInputHandler.ConsumeInteract() && currentInteractable != null)
        {
            Debug.Log("Touchable");
            currentInteractable.Interact();
        }
    }

    void CheckInteraction()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            DisableCurrentInteractable();
            return;
        }

        RaycastHit hit;
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        //colliders within reach
        if (Physics.Raycast(ray, out hit, playerReach))
        {
            if(hit.collider.tag == "Interactable")
            {
                Interactable newInteractable = hit.collider.GetComponent<Interactable>();
                //Debug.Log("Interactable in range");

                if(currentInteractable && newInteractable != currentInteractable)
                {
                    currentInteractable.DisableOutline();
                }
                if (newInteractable.enabled)
                {
                    
                    SetNewCurrentInteractable(newInteractable);
                }
                else //nothing in reach
                {
                    DisableCurrentInteractable();
                }
            }
        }
        else //nothing in reach
        {
            DisableCurrentInteractable();
        }
    }

    void SetNewCurrentInteractable(Interactable newInteractable)
    {
        currentInteractable = newInteractable;
        currentInteractable.EnableOutline();
    }

    void DisableCurrentInteractable()
    {
        if (currentInteractable)
        {
            currentInteractable.DisableOutline();
            currentInteractable = null;
        }
    }
}
