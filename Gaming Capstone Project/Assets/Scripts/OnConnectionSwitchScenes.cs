using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplayer;

public class OnConnectionSwitchScenes : MonoBehaviour
{
    private NetworkManager networkManager;
    private void Start()
    {
        networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            // Subscribe to event properly
            networkManager.OnClientConnectedCallback += OnConnectedToServer;
        }
        else
        {
            Debug.LogError("NetworkManager is not initialized.");
        }
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnConnectedToServer;
        }
    }

    private void OnConnectedToServer(ulong clientId) 
    {
        Debug.Log("Server Connected");
        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.LoadScene("Main", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("SceneManager is not available in NetworkManager.");
        }
    }
}
