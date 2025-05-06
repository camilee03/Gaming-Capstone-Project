using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Netcode.Components;

public class CloseGameOnExit : NetworkBehaviour
{
    public void OnApplicationQuit()
    {
        if(IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (IsClient && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
        }
    }
}
