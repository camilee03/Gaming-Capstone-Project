using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;

public class HostHittingEnter : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    GameController controller;
    public Button StartButton;
    public Canvas LobbyMenu;

    public TMP_Text text;

    private void Update()
    {
        if (StartButton != null)
        {
            StartButton.onClick.RemoveAllListeners();
            StartButton.onClick.AddListener(ButtonPressed);



        }

        controller = GameController.Instance;

        controller.setLobby(LobbyMenu.gameObject);
    }
    private void LateUpdate()
    {
        Cursor.lockState = CursorLockMode.None;

    }


    void ButtonPressed()
    {
        if(IsHost)
        {
            controller.HostSelectsStart();
            controller.DisableLobbyCanvasClientRpc();
        }
        else
        {
            text.transform.DOMoveY(300, 5).OnComplete(() =>
            {
                text.transform.DOMoveY(75, 0.25f);
            });
            text.DOFade(1, 3).OnComplete(() => {
                text.DOFade(0, 2);
            });
        }
            Debug.Log("Button has been pressed!");
        Cursor.lockState = CursorLockMode.Locked;
    }
}
