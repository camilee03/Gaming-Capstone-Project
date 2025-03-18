using Unity.Services.Vivox;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class VivoxManager : MonoBehaviour
{
    public ProximityChat proximityChat;
    private async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        await VivoxService.Instance.InitializeAsync();
        await VivoxService.Instance.LoginAsync();
        Debug.Log("Vivox Initialized and Player Authenticated");
        await proximityChat.JoinProximityChat();
    }
}
