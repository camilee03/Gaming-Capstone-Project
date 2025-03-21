using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;

public class DOSInteraction : MonoBehaviour
{
    public Transform cameraMovementPoint;
    public CameraMovement CameraMovementScript;
    private GameObject camera;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    public bool InInteraction = false;
    public Interact interact;
    PlayerController playerController;

    private Coroutine interactionCoroutine;

    public TMP_Text[] PreviousLines;
    public TMP_InputField WritingLine;
    public int maxCharacters = 9;

    void Start()
    {
        camera = Camera.main.gameObject;
        CameraMovementScript = camera.GetComponent<CameraMovement>();
    }

    private void Update()
    {
        if(InInteraction)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                //send current line to seperate method
                UpdatePreviousLines();

            }
            WritingLine.ActivateInputField();
            WritingLine.Select();
        }
    }

    private void UpdatePreviousLines()
    {
        for(int i = PreviousLines.Length-1; i > 0 ; i--)
        {
            Debug.Log(i + " " + (i - 1));
            PreviousLines[i].text = PreviousLines[i-1].text;
        }
        PreviousLines[0].text = WritingLine.text;
        WritingLine.text = "";
    }



    #region Entering and Exiting Interaction
    public void ToggleInteraction()
    {

            if (!InInteraction)
                StartInteraction();
            else
                EndInteraction();
        
    }
    public void SetInteract(Interact input, PlayerController player)
    {
        interact = input;
        playerController = player;
    }
    public void StartInteraction()
    {
        if (interactionCoroutine != null)
            StopCoroutine(interactionCoroutine);

        InInteraction = true;
        originalPosition = camera.transform.position;
        originalRotation = camera.transform.rotation;


        interactionCoroutine = StartCoroutine(LerpCamera(cameraMovementPoint.position, cameraMovementPoint.rotation, 1f, false));

        playerController.enabled = false;
    }

    public void EndInteraction()
    {

        if (interactionCoroutine != null)
            StopCoroutine(interactionCoroutine);

        InInteraction = false;
        interactionCoroutine = StartCoroutine(LerpCamera(originalPosition, originalRotation, 1f, true));

        playerController.enabled = true;
        playerController = null;
    }

    private IEnumerator LerpCamera(Vector3 targetPosition, Quaternion targetRotation, float duration, bool enableCameraMovement)
    {
        if (!enableCameraMovement) //if movement is disabled call before lerp
        {
            CameraMovementScript.canMove = enableCameraMovement;
            CameraMovementScript.playerController.ToggleMovement(enableCameraMovement);

        }

        float timeElapsed = 0f;
        Vector3 startPosition = camera.transform.position;
        Quaternion startRotation = camera.transform.rotation;

        while (timeElapsed < duration)
        {
            camera.transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / duration);
            camera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        camera.transform.position = targetPosition;
        camera.transform.rotation = targetRotation;
        if (enableCameraMovement)
        {
            CameraMovementScript.canMove = enableCameraMovement;
            CameraMovementScript.playerController.ToggleMovement(enableCameraMovement);
        }
    }
    #endregion
}
