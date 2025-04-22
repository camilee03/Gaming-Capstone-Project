using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class NetMic2 : NetworkBehaviour
{
    public AudioClip micClip;
    public PlayerController pc;
    public int sampleRate = 16000;
    public int sampleLength = 1024;
    private float[] sampleBuffer;
    private byte[] byteBuffer;
    public AudioSource source;
    private float sendTimer = 0f;
    public float sendInterval = 0.1f;
    public float noiseThreshold = 0.002f;
    public bool playback;
    bool playable;

    public static List<NetMic2> allMics = new List<NetMic2>();

    private void OnEnable()
    {
        allMics.Add(this);
    }
    private void OnDisable()
    {
        allMics.Remove(this);
    }

    void Start()
    {
        if (!IsOwner || playback)
        {
            playable = true;
        }
        else playable = false;
        source.enabled = playable;

        if (IsOwner)
        {
            micClip = Microphone.Start(null, true, 16, sampleRate);
            sampleBuffer = new float[sampleLength];
        }
        sendInterval = (float)sampleLength / (float)sampleRate / 2;
    }
    void Update()
    {
        if (pc.isDead && source.spatialBlend != 0)
        {
            source.spatialBlend = 0; //once you die, set spatialblend to 0.
        }
        if (!IsOwner || micClip == null) return;

        sendTimer += Time.deltaTime;

        if (sendTimer >= sendInterval)
        {
            sendTimer = 0f;
            int micPos = Microphone.GetPosition(null);
            int startPos = micPos - sampleLength;
            if (startPos < 0) return;

            micClip.GetData(sampleBuffer, startPos);
            byteBuffer = FloatArrayToByteArray(sampleBuffer);
            if (IsServer) { SendAudioClientRpc(byteBuffer); }
            else { SendAudioServerRpc(byteBuffer); }
        }
    }


    [ClientRpc]
    void SendAudioClientRpc(byte[] audioBytes)
    {
        BroadcastAudio(audioBytes);
        //Debug.Log("Client");
    }

    [ServerRpc]
    void SendAudioServerRpc(byte[] audioBytes)
    {
        SendAudioClientRpc(audioBytes);
        Debug.Log("server");
    }

    void BroadcastAudio(byte[] audioBytes)
    {
        float[] floats = ByteArrayToFloatArray(audioBytes);
        float[] denoisedAudio = ApplyNoiseGate(floats, noiseThreshold);

        bool senderIsDead = pc != null && pc.isDead;

        foreach(var mic in allMics)
        {
            if (mic == this || mic.pc == null) continue;

            bool receiverIsDead = mic.pc.isDead;

            if (senderIsDead)
            {
                if (receiverIsDead)//dead to dead.
                {
                    mic.PlayReceivedAudio(denoisedAudio); 
                }
                //dead sender audio will only play to dead receivers.
            }
            else //alive will send to both alive and dead.
            {
                mic.PlayReceivedAudio(denoisedAudio);
            }
        }
    }
    private byte[] FloatArrayToByteArray(float[] floats)
    {
        byte[] bytes = new byte[floats.Length * 4];
        for (int i = 0; i < floats.Length; i++)
        {
            byte[] b = System.BitConverter.GetBytes(floats[i]);
            b.CopyTo(bytes, i * 4);
        }
        return bytes;
    }

    private float[] ByteArrayToFloatArray(byte[] bytes)
    {
        float[] floats = new float[bytes.Length / 4];
        for (int i = 0; i < floats.Length; i++)
        {
            floats[i] = System.BitConverter.ToSingle(bytes, i * 4);
        }
        return floats;
    }

    float[] ApplyNoiseGate(float[] input, float threshold)
    {
        float[] output = new float[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = Mathf.Abs(input[i]) < threshold ? 0f : input[i];
        }
        return output;
    }

    void PlayReceivedAudio(float[] floats)
    {
        if (playable)
        {
            AudioClip clip = AudioClip.Create("Received", floats.Length, 1, sampleRate, false);
            clip.SetData(floats, 0);
            source.clip = clip;
            source.Play();
        }
    }
}
