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

        // var joinOptions = new ChannelOptions
        // {
        //     IsPositional = true,  // Enables 3D positional audio
        //     Channel3DProperties = properties
        // };
        await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, properties);
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


