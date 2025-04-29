using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class VoteManager : NetworkBehaviour
{
    public GameController Controller;
    public GameObject Colorbutton;
    public HorizontalLayoutGroup HorizontalLayoutGroup;
    private List<GameObject> buttons;
    private Dictionary<GameObject, int> buttonToColor = new Dictionary<GameObject, int>();
    public AudioSource PlayerAudioSource;

    private void OnEnable()
    {
        Controller = GameController.Instance;
        buttons = new List<GameObject>();   
    }
    public void ClearButtons()
    {
        foreach (GameObject button in buttons)
        {
            Destroy(button);
        }
        buttons.Clear();
        buttonToColor.Clear();

    }
    public void CreateColorButtons()
    {
        ClearButtons();

        foreach (NetworkClient c in NetworkManager.Singleton.ConnectedClientsList)
        {
            PlayerController pc = c.PlayerObject.GetComponent<PlayerController>();
            GameObject button = Instantiate(Colorbutton, HorizontalLayoutGroup.transform);
            button.GetComponent<Image>().color = Controller.getColorByIndex(pc.ColorID);
            button.transform.GetComponentInChildren<TextMeshProUGUI>().text = pc.playerName;

            int capturedColor = pc.ColorID;
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnColorButtonClicked(capturedColor);
            });

            buttons.Add(button);
            buttonToColor[button] = capturedColor;
        }
        
    }


    public void SetProximityChatAmount(float amount)
    {
        DOTween.To(
            () => PlayerAudioSource.spatialBlend,
            x => PlayerAudioSource.spatialBlend = x,
            amount,
            3f
        ).SetEase(Ease.InOutSine);
    }




    private void OnColorButtonClicked(int colorIndex)
    {
        Debug.Log($"[VoteManager] Color {colorIndex} button clicked.");
        updateColors(colorIndex); // pass color index, not button index
        NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<PlayerController>().CastVoteServerRpc(colorIndex);
    }



    private void updateColors(int selectedColorIndex)
    {
        foreach (var kvp in buttonToColor)
        {
            var button = kvp.Key;
            var colorIndex = kvp.Value;

            if (colorIndex == selectedColorIndex)
                button.GetComponent<Image>().color = Color.gray;
            else
                button.GetComponent<Image>().color = Controller.getColorByIndex(colorIndex);
        }
    }


}
