using Unity.Netcode;
using UnityEngine;

public class SelectColorButton : MonoBehaviour
{
    public void OnClick_SelectColor(int colorIndex)
    {
        PlayerController localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        localPlayer.RequestColorSelection(colorIndex);
    }
}
