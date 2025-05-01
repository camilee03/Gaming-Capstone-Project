using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;

public class DisconnectButton : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [SerializeField] private Button disconnectButton;

    private void Start()
    {
        if (disconnectButton == null)
            disconnectButton = GetComponent<Button>();

        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(DisconnectAndLoadMenu);
    }

    public void DisconnectAndLoadMenu()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("No NetworkManager found.");
            return;
        }

        // Shutdown network (for client or host)
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Optionally: reset any static managers or singletons here

        // Load main menu
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
