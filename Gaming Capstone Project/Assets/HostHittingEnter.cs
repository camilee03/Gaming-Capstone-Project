using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class HostHittingEnter : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    GameController controller;
    public Button StartButton;
    public GameObject LobbyMenu;
    public LobbyFadeScript LobbyFadeScript;

    public LobbyTextAnimation text;
    bool started;

    private void Update()
    {
        if (StartButton != null)
        {
            StartButton.onClick.RemoveAllListeners();
            StartButton.onClick.AddListener(ButtonPressed);



        }

        controller = GameController.Instance;
        Cursor.lockState = CursorLockMode.None;

        controller.setLobby(LobbyMenu);
        if (!started)
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
    }


    void ButtonPressed()
    {
        if(IsHost)
        {
            StartCoroutine(CheckPlayerCountBeforeStart());
        }
        else
        {
            text.ShowError(0);
        }
        Debug.Log("Button has been pressed!");
        Cursor.lockState = CursorLockMode.Locked;
    }

    IEnumerator CheckPlayerCountBeforeStart()
    {
        started = true;
        float timer = 0f;
        float maxWaitTime = 10f; // optional: limit how long to check

        while (timer < 5f && maxWaitTime > 0f)
        {
            /*
            if (NetworkManager.Singleton.ConnectedClients.Count < 2)
            {
                text.ShowError(1);
                yield break;
            }
            */
            LobbyFadeScript.LevelFade();

            Cursor.lockState = CursorLockMode.Locked;

            StartButton.interactable = false;
            StartButton.GetComponentInChildren<TMP_Text>().text = "Loading...";
            timer += Time.deltaTime;
            maxWaitTime -= Time.deltaTime;
            yield return null;
        }
        Cursor.lockState = CursorLockMode.Locked;
        controller.HostSelectsStart();
        controller.DisableLobbyCanvasClientRpc();
        this.enabled = false;
    }
}
