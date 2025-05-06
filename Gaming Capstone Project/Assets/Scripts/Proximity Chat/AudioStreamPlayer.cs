using UnityEngine;
using System.Collections.Generic;

public class AudioStreamPlayer : MonoBehaviour
{
    private Queue<float> audioBuffer = new Queue<float>();
    private object bufferLock = new object();
    public AudioSource source;

    void Start()
    {
        source.Play();
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        lock (bufferLock)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (audioBuffer.Count > 0)
                    data[i] = audioBuffer.Dequeue();
                else
                    data[i] = 0f; //silence if no data
            }
        }
    }

    public void AddSamples(float[] samples)
    {
        lock (bufferLock)
        {
            foreach (var sample in samples)
                audioBuffer.Enqueue(sample);
        }
    }
}
