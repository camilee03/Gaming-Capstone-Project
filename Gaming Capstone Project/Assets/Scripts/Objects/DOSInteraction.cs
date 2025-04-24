using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using Unity.Netcode;
using System.Collections.Generic;

public class DOSInteraction : MonoBehaviour
{
    public Transform cameraMovementPoint;
    public CameraMovement CameraMovementScript;
    public PlayerController playerController;
    private GameObject camera;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    public bool InInteraction = false;
    public Interact interact;

    private Coroutine interactionCoroutine;

    private TMP_Text[] PreviousLines;
    public TMP_InputField WritingLine;
    public int maxCharacters = 20;

    private DOSCommandController DOSController;

    private DOSManager DOSManager;

    void Start()
    {
        DOSManager = DOSManager.Instance;
        DOSController = DOSCommandController.Instance;
        WritingLine = DOSManager.InputField;
        PreviousLines = DOSManager.CommandLines;
    }

    public void SetCam(GameObject cam)
    {
        camera = cam;
        CameraMovementScript = camera.GetComponent<CameraMovement>();
        playerController = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<PlayerController>();

    }

    private void Update()
    {
        if (DOSManager == null) { DOSManager = DOSManager.Instance; WritingLine = DOSManager.InputField; }
        if (DOSController == null) { DOSController = DOSCommandController.Instance; }

        if (InInteraction)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                //send current line to seperate method
                string possibleError = DOSController.HandleCommand(WritingLine.text);
                UpdatingCommandLine();
                if (possibleError != "") { ShowErrorMessage(possibleError); }

            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EndInteraction();
            }

        }
    }

    private void UpdatingCommandLine()
    {
        for (int i = PreviousLines.Length-1; i > 0; i--)
        {
            PreviousLines[i].text = PreviousLines[i - 1].text;
        }
        PreviousLines[0].text = WritingLine.text;
        WritingLine.text = "";
        WritingLine.interactable = true;
        WritingLine.ActivateInputField();
        WritingLine.Select();
    }

    private void ShowErrorMessage(string error)
    {
        DOSManager.ErrorPopup.SetActive(true);
        DOSManager.ErrorText.text = error;

        StartCoroutine(RemoveErrorMessage(3));
    }

    IEnumerator RemoveErrorMessage(int time)
    {
        yield return new WaitForSeconds(time);
        DOSManager.ErrorPopup.SetActive(false);
    }

    #region Entering and Exiting Interaction
    public void ToggleInteraction()
    {

        if (!InInteraction)
            StartInteraction();
    }
    public void SetInteract(Interact input)
    {
        interact = input;
    }
    public void StartInteraction()
    {
        if (interactionCoroutine != null)
            StopCoroutine(interactionCoroutine);

        playerController.ToggleInput(false);

        InInteraction = true;
        originalPosition = camera.transform.position;
        originalRotation = camera.transform.rotation;


        interactionCoroutine = StartCoroutine(LerpCamera(cameraMovementPoint.position, cameraMovementPoint.rotation, 1f, false));
        WritingLine.interactable = true;
        WritingLine.ActivateInputField();
        WritingLine.Select();
    }

    public void EndInteraction()
    {

        if (interactionCoroutine != null)
            StopCoroutine(interactionCoroutine);

        playerController.ToggleInput(true);

        InInteraction = false;
        interactionCoroutine = StartCoroutine(LerpCamera(originalPosition, originalRotation, 1f, true));
        WritingLine.DeactivateInputField(false);
        //EventSystem.current.SetSelectedGameObject(null);
        WritingLine.interactable = false;


    }

    private IEnumerator LerpCamera(Vector3 targetPosition, Quaternion targetRotation, float duration, bool enableCameraMovement)
    {
        float timeElapsed = 0f;
        Vector3 startPosition = camera.transform.position;
        Quaternion startRotation = camera.transform.rotation;
        if (!enableCameraMovement)
        {
            playerController.canMove = enableCameraMovement;
            CameraMovementScript.canMove = enableCameraMovement;
        }
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
            playerController.canMove = enableCameraMovement;
            CameraMovementScript.canMove = enableCameraMovement;
        }



    }
    #endregion
}