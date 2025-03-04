using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIInteraction : MonoBehaviour
{
    public Transform playerCamera;
    public float interactionDistance = 3f;
    public GameObject inputFieldUI;
    public TMPro.TMP_InputField inputField;

    private bool isInteracting = false;
    private CharacterController characterController;
    private PlayerController fpsController; // Replace with your movement script

    void Start()
    {
        fpsController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
        inputFieldUI.SetActive(false);
    }

    void Update()
    {
        if (isInteracting)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EndInteraction();
            }
            return;
        }

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            if (hit.collider.CompareTag("InteractableUI") && Input.GetKeyDown(KeyCode.E))
            {
                StartInteraction();
            }
        }
    }

    void StartInteraction()
    {
        isInteracting = true;
        fpsController.enabled = false; // Disable player movement
        inputFieldUI.SetActive(true);
        inputField.ActivateInputField();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void EndInteraction()
    {
        isInteracting = false;
        fpsController.enabled = true; // Enable player movement
        inputFieldUI.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
