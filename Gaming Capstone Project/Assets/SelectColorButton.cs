using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SelectColorButton : MonoBehaviour
{
    [Tooltip("Color index from 0 to 11")]

    private Button buttonComponent;
    private Image buttonImage;
    private Color color;
    public LobbyColorGuy lobbyColorGuy;

    public LobbyColorButton[] colorButtons;

    private void Awake()
    {
        buttonComponent = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

    }

    private void Start()
    {
        RefreshAll();
    }

    public void OnClick_SelectColor(int colorIndex)
    {
        if (!NetworkManager.Singleton.IsConnectedClient) return;

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject?.GetComponent<PlayerController>();
        if (localPlayer != null)
        {
            localPlayer.RequestColorSelection(colorIndex);
        }

        Debug.Log($"Selected colorIndex: {colorIndex}");

    }

    public void RefreshAll()
    {
        foreach (var button in colorButtons)
        {
            button.RefreshState();
        }
    
    lobbyColorGuy.UpdateColor();
    }
}
