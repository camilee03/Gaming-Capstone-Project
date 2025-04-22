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

    private void Update()
    {
        if (StartButton != null)
        {
            StartButton.onClick.RemoveAllListeners();
            StartButton.onClick.AddListener(ButtonPressed);



        }

        controller = GameController.Instance;

        controller.setLobby(LobbyMenu);
    }
    private void LateUpdate()
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

            StartButton.interactable = false;
            StartButton.GetComponentInChildren<TMP_Text>().text = "Loading...";
            timer += Time.deltaTime;
            maxWaitTime -= Time.deltaTime;
            yield return null;
        }

        controller.HostSelectsStart();
        controller.DisableLobbyCanvasClientRpc();
        this.enabled = false;
    }
}
