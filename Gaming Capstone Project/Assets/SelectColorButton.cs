using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SelectColorButton : MonoBehaviour
{
    [Tooltip("Color index from 1 to 12")]
    public int colorIndex;

    private Button buttonComponent;
    private Image buttonImage;
    private Color color;
    private void Awake()
    {
        buttonComponent = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        color = buttonImage.color;

    }

    private void Start()
    {
        RefreshState();
    }

    public void OnClick_SelectColor()
    {
        if (!NetworkManager.Singleton.IsConnectedClient) return;

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject?.GetComponent<PlayerController>();
        if (localPlayer != null)
        {
            localPlayer.RequestColorSelection(colorIndex);
        }

        Debug.Log($"Selected colorIndex: {colorIndex}");

    }

    public void RefreshState()
    {
        if (GameController.Instance == null || buttonComponent == null) return;

        bool available = GameController.Instance.IsColorAvailable(colorIndex);
        buttonComponent.interactable = available;

        // Gray out the color if taken
        if (buttonImage != null)
        {
            if(available) buttonImage.color = color;
            else buttonImage.color = Color.gray;
            Debug.Log("RefreshedColor");
        }
    }
}
