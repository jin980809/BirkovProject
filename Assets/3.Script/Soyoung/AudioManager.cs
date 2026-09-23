using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string Name;
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;   //소리 너무 클 걸 대비해서
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance = null;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        if (BGMPlayer != null)
        {
            BGMPlayer.loop = true; //BGM은 기본적으로 루프 재생
        }
    }

    [Header("Audio Clip")]
    [SerializeField] private Sound[] BGM;
    [SerializeField] private Sound[] SFX;

    [Header("Audio Source (인스펙터에서 직접 연결)")]
    [SerializeField] private AudioSource BGMPlayer;
    [SerializeField] private AudioSource[] SFXPlayer;

    private int sfxRoundRobinIndex = 0;  //혹시 SFX 풀이 꽉 차면 재생 중인 거 하나 끊고 재사용하라는 목적으로 만듦

    public void PlayBGM(string name)
    {
        if (BGMPlayer == null)
        {
            Debug.Log("AudioManager: BGMPlayer가 연결되지 않았습니다.");
            return;
        }
        foreach (Sound s in BGM)
        {
            if (s.Name.Equals(name))
            {
                if (s.clips == null || s.clips.Length == 0)
                {
                    Debug.LogWarning($"AudioManager : '{name}' BGM에 클립이 비어있습니다.");
                    return;
                }
                BGMPlayer.clip = s.clips[0];
                BGMPlayer.volume = s.volume;
                BGMPlayer.Play();
                return;
            }
        }
    }

    public void StopBGM()
    {
        BGMPlayer.Stop();
    }

    public void PlaySFX(string name)
    {
        foreach (Sound s in SFX)
        {
            if (s.Name.Equals(name))
            {
                if (s.clips == null || s.clips.Length == 0)
                {
                    Debug.LogWarning($"AudioManager: '{name}' SFX에 클립이 비어있습니다.");
                    return;
                }

                AudioClip clip = s.clips[Random.Range(0, s.clips.Length)];
             
                for (int i = 0; i < SFXPlayer.Length; i++)
                {
                    if (!SFXPlayer[i].isPlaying)
                    {
                        PlayOn(SFXPlayer[i],clip,s.volume);
                        return;
                    }
                }
                PlayOn(SFXPlayer[sfxRoundRobinIndex], clip, s.volume);
                sfxRoundRobinIndex = (sfxRoundRobinIndex + 1) % SFXPlayer.Length;
                return;
            }
        }
        Debug.Log($"해당 SFX를 가진 sound가 없음. [{name}]");
    }
    private void PlayOn(AudioSource source, AudioClip clip, float volume)
    {
        source.clip = clip;
        source.volume = volume;
        source.Play();
    }

    public void SetBGMVolume(float v)  //나중에 볼륨 슬라이더 UI에 써도 될 거 같은 훅
    {
        if (BGMPlayer != null)
        {
            BGMPlayer.volume = v;
        }
    }
    public void SetSFXVolume(float v)
    {
        if (SFXPlayer == null)
        {
            return;
        }
        foreach (AudioSource source in SFXPlayer)
        {
            source.volume = v;
        }
    }
}