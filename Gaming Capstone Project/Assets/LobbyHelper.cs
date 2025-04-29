using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class LobbyHelper : NetworkBehaviour
{
    public Button[] colorButtons;
    public LobbyColorGuy lcg;
    private void Start()
    {
        updateColorButtons();
    }
    public void ChangeName(string name)
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().ExternalSetName(name);
    }
    public void ChangeColor(int colorIndex)
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().ExternalSetColor(colorIndex);
        lcg.UpdateColor();
        if (IsServer) { updateColorButtonsClientRpc(); }
        else { updateColorButtonsServerRpc(); }
    }
    [ServerRpc(RequireOwnership = false)]
    void updateColorButtonsServerRpc()
    {
        updateColorButtonsClientRpc();
    }
    [ClientRpc]
    void updateColorButtonsClientRpc()
    {
        updateColorButtons();
    }
    void updateColorButtons()
    {
        for (int i = 0; i < colorButtons.Length; i++)
        {
            if (GameController.Instance.usedColors.Contains(i))
            {
                colorButtons[i].interactable = false;
                
            }
            else colorButtons[i].interactable = true;
        }
    }
}
