using UnityEngine;
using Unity.Services.Vivox;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class ProximityChat : MonoBehaviour
{
    private const string channelName = "ProximityChat";
    public GameObject player;
    private bool chatJoined = false;

    public async Task JoinProximityChat()
    {
        var properties = new Channel3DProperties(
            audibleDistance: 20,  // Max hearing distance
            conversationalDistance: 5,  // Normal talking range
            audioFadeIntensityByDistanceaudio: 1.0f,  // Smooth fading
            audioFadeModel: AudioFadeModel.LinearByDistance
        );
        //await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, properties);
        await VivoxService.Instance.JoinEchoChannelAsync(channelName, ChatCapability.AudioOnly);
        Debug.Log("Joined Proximity Voice Chat");
        chatJoined = true;
    }


    private void Update()
    {
        if(chatJoined)
        {
            VivoxService.Instance.Set3DPosition(player, channelName, true);
        }
    }


}


