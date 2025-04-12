using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class NetMic2 : NetworkBehaviour
{
    public AudioClip micClip;
    public int sampleRate = 16000;
    public int sampleLength = 1024;
    private float[] sampleBuffer;
    private byte[] byteBuffer;
    public AudioStreamPlayer audioStreamPlayer;
    public AudioSource source;
    private float sendTimer = 0f;
    public float sendInterval = 0.1f;
    public float noiseThreshold = 0.002f;
    bool bufferingAudio;
    //public float refreshSpeed = 2;
    //float refresh;
    Queue<float[]> audioBufferQueue = new Queue<float[]>();
    void Start()
    {
        if (IsOwner)
        {
            micClip = Microphone.Start(null, true, 1, sampleRate);
            sampleBuffer = new float[sampleLength];
        }
        sendInterval = (float)sampleLength / (float)sampleRate;
    }
    void Update()
    {
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
        Debug.Log("Client");
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
        PlayReceivedAudio(ApplyNoiseGate(floats,noiseThreshold));
        /*if (audioStreamPlayer != null)
        {
            audioStreamPlayer.AddSamples(floats);
        }*/
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

    void PlayReceivedAudio(float[] floats)
    {
        AudioClip clip = AudioClip.Create("Received", floats.Length, 1, sampleRate, false);
        clip.SetData(floats, 0);
        source.clip = clip;
        source.Play();
        
    }
    void PlayBufferedAudio()
    {
        Debug.Log("Playing Buffered Audio");
        bufferingAudio = true;
        List<float> combined = new List<float>();
        while (audioBufferQueue.Count > 0)
        {
            combined.AddRange(audioBufferQueue.Dequeue());
        }
        AudioClip clip = AudioClip.Create("Buffered", combined.ToArray().Length, 1, sampleRate, false);
        clip.SetData(combined.ToArray(), 0);
        source.clip = clip;
        source.Play();
        bufferingAudio = false;
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
}
