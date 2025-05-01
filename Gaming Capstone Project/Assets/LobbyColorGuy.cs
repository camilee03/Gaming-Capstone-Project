using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LobbyColorGuy : MonoBehaviour
{
    private PlayerController localPlayer;
    private ColorManager colorManager;

    void Start()
    {
        // Delay getting player until it's spawned
        colorManager = GetComponent<ColorManager>();
        StartCoroutine(WaitForLocalPlayer());
    }

    private System.Collections.IEnumerator WaitForLocalPlayer()
    {
        while (NetworkManager.Singleton.LocalClient == null ||
               NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            yield return null;
        }

        localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();

    }
    public IEnumerator GetColorID(System.Action<int> callback)
    {
        yield return new WaitForSeconds(0.25f);
        callback?.Invoke(localPlayer.ColorID);
    }

    public void UpdateColor()
    {
        StartCoroutine(GetColorID((colorID) =>
        {
            colorManager.ChangeSuitColor(colorID);
        }));
    }

}
