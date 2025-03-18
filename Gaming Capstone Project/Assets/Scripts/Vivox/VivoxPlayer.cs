using UnityEngine;
using Unity.Services.Vivox;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Netcode;
using System.Xml.Serialization;

public class VivoxPlayer : NetworkBehaviour
{
    [SerializeField] private GameObject localPlayerHead;
    private Vector3 lastPlayerHeadPos;
    private string gameChannelName = "3DChannel";
    private bool isIn3DChannel = false;
    Channel3DProperties player3DProperties;
    private int clientID;
    [SerializeField] private int newVolumeMinusPlus50 = 0;
    private float nextPosUpdate;
    public override void OnNetworkSpawn()
    {
        if(IsLocalPlayer)
        {
            InitalizeAsync();
            VivoxService.Instance.LoggedIn += onLoggedIn;
            VivoxService.Instance.LoggedOut += onLoggedOut;
        }
        
    }

    void Update()
    {
        if(isIn3DChannel && IsLocalPlayer)
        {
            if(Time.time > nextPosUpdate)
            {
                updatePlayer3DPos();
                nextPosUpdate += 0.3f;
            }
        }
    }

    async void InitalizeAsync()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await VivoxService.Instance.InitializeAsync();
        Debug.Log("Vivox initlaize successfull");
    }

    public async void LoginToVivoxAsync()
    {
        if(IsLocalPlayer)
        {
            clientID = (int) GameObject.Find("NetworkManager").GetComponent<NetworkManager>().LocalClientId;
            LoginOptions options = new LoginOptions();
            options.DisplayName = "Client" + clientID;
            options.EnableTTS = true;
            await VivoxService.Instance.LoginAsync();

            Join3DChannelAsync();
        }
    }

    public async void Join3DChannelAsync()
    {
        await VivoxService.Instance.JoinPositionalChannelAsync(gameChannelName, ChatCapability.AudioOnly, player3DProperties);
        isIn3DChannel = true;
        Debug.Log("vivox joined 3d channel");
    }

    public void updatePlayer3DPos()
    {
        VivoxService.Instance.Set3DPosition(localPlayerHead, gameChannelName);
        if(localPlayerHead.transform.position != lastPlayerHeadPos) 
        {
            lastPlayerHeadPos = localPlayerHead.transform.position;
        }
    }

    private void onLoggedIn()
    {
        if(VivoxService.Instance.IsLoggedIn)
        {
            Debug.Log("Client " + clientID + " login successful");
        } else
        {
            Debug.Log("cannot sign in");
        }
    }

    private void onLoggedOut()
    {
        isIn3DChannel = false;
        VivoxService.Instance.LeaveAllChannelsAsync();
        Debug.Log("channel left");
        VivoxService.Instance.LogoutAsync();
        Debug.Log("logged out");
    }

    public void setPlayerHeadPos(GameObject playerHead)
    {
        if(localPlayerHead == null)
        {
            localPlayerHead = playerHead;
        }
    }

    public void updateVolume()
    {
        VivoxService.Instance.SetChannelVolumeAsync(gameChannelName, newVolumeMinusPlus50);
    }
    
}
