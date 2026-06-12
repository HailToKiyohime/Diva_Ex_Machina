using UnityEngine;
using System.Collections.Generic;
using System.Collections.Generic;
public class AudioManager : MonoBehaviour
{
    private Dictionary<string, AudioSource> loopingSources = new Dictionary<string, AudioSource>();
    public static AudioManager Instance { get; private set; }

    public Sound[] sounds;

    [SerializeField] private int poolSize = 16; // 可在 Inspector 調整
    private List<AudioSource> sourcePool = new List<AudioSource>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 預先建立 AudioSource 池
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            sourcePool.Add(src);
        }
    }

    // 從池中取得一個空閒的 AudioSource
    private AudioSource GetAvailableSource()
    {
        foreach (var src in sourcePool)
        {
            if (!src.isPlaying) return src;
        }

        // 池子全滿時，新增一個（自動擴容）
        Debug.LogWarning("AudioManager: Pool exhausted, expanding.");
        AudioSource newSrc = gameObject.AddComponent<AudioSource>();
        newSrc.playOnAwake = false;
        sourcePool.Add(newSrc);
        return newSrc;
    }

    public void Play(string soundName, float volume, float minPitch, float maxPitch, bool loop)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == soundName);
        if (s == null) { Debug.LogWarning($"Sound '{soundName}' not found."); return; }

        AudioSource source = GetAvailableSource();
        source.clip = s.clip;
        source.volume = volume;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.loop = loop;
        source.Play();
    }

    public void Play(string soundName)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == soundName);
        if (s == null) { Debug.LogWarning($"Sound '{soundName}' not found."); return; }
        Play(soundName, s.volume, s.minPitch, s.maxPitch, s.loop);
    }
    public void PlayExclusive(string soundName, float volume, float minPitch, float maxPitch, bool loop)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == soundName);
        if (s == null) return;

        // 找正在播同一個 clip 的 source，直接重播
        foreach (var src in sourcePool)
        {
            if (src.clip == s.clip && src.isPlaying)
            {
                src.volume = volume;
                src.pitch = Random.Range(minPitch, maxPitch);
                src.Play();
                return;
            }
        }

        // 沒有在播才開新的
        Play(soundName, volume, minPitch, maxPitch, loop);
    }
    public void PlayCapped(string soundName, float volume, float minPitch, float maxPitch, bool loop, int maxConcurrent = 3)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == soundName);
        if (s == null) return;

        // 計算目前有幾個同 clip 的 source 正在播
        int activeCount = 0;
        AudioSource oldest = null;
        float oldestTime = float.MaxValue;

        foreach (var src in sourcePool)
        {
            if (src.clip == s.clip && src.isPlaying)
            {
                activeCount++;
                // 記錄播最久的（time 越小代表越早開始）
                float remaining = src.clip.length - src.time;
                if (remaining < oldestTime)
                {
                    oldestTime = remaining;
                    oldest = src;
                }
            }
        }

        // 未超上限：直接開新的
        if (activeCount < maxConcurrent)
        {
            Play(soundName, volume, minPitch, maxPitch, loop);
            return;
        }

        // 超過上限：搶佔剩餘時間最短的那個（反正快播完了）
        if (oldest != null)
        {
            oldest.volume = volume;
            oldest.pitch = Random.Range(minPitch, maxPitch);
            oldest.Play();
        }
    }
    public void PlayLooping(string soundName)
    {
        // 已經在播就不重複開
        if (loopingSources.ContainsKey(soundName) && loopingSources[soundName].isPlaying)
            return;

        Sound s = System.Array.Find(sounds, sound => sound.name == soundName);
        if (s == null) { Debug.LogWarning($"Sound '{soundName}' not found."); return; }

        AudioSource source = GetAvailableSource();
        source.clip = s.clip;
        source.volume = s.volume;
        source.pitch = Random.Range(s.minPitch, s.maxPitch);
        source.loop = true;
        source.Play();

        loopingSources[soundName] = source;
    }

    public void StopLooping(string soundName)
    {
        if (loopingSources.TryGetValue(soundName, out AudioSource src))
        {
            src.Stop();
            loopingSources.Remove(soundName);
        }
    }
}