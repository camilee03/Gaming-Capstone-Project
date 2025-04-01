using UnityEngine;
using System.Collections.Generic;

public class SoundEffector : MonoBehaviour
{
    [System.Serializable]
    public class effectList
    {
        public string name;
        public AudioClip[] clips = new AudioClip[0];
    }
    public List<effectList> soundEffects = new List<effectList>();
    public AudioSource source;
    void PlaySoundEffect(int effectIndex)
    {
        AudioClip[] array = soundEffects[effectIndex].clips;
        AudioClip clip = array[Mathf.RoundToInt(Random.Range(0, array.Length))];
        source.pitch = Random.Range(0.8f, 1.1f);
        source.PlayOneShot(clip);
    }
}
