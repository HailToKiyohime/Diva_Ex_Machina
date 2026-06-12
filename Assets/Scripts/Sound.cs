using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    public float volume = 1f;
    public float minPitch = 1f;
    public float maxPitch = 1f;
    public bool loop = false;
}
