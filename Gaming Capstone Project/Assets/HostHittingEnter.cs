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
    void Start()
    {
        controller = GameController.Instance;
        StartButton.onClick.AddListener(ButtonPressed);


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
            LobbyMenu.gameObject.SetActive(false);
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
    }
}
