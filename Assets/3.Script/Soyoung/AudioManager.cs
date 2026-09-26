using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string Name;
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;   //소리 너무 클 걸 대비해서

    //클립을 여러 개 넣으면 넣은 순서대로 하나씩 번갈아 재생한다 (마지막까지 가면 다시 처음으로).
    //재생 위치는 저장하지 않는다 - 게임을 다시 켜면 첫 클립부터 시작한다.
    private int nextClipIndex = 0;

    public AudioClip NextClip()
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        if (nextClipIndex >= clips.Length)
        {
            nextClipIndex = 0;   //인스펙터에서 클립 개수를 줄인 경우에도 벗어나지 않게 한다
        }

        AudioClip clip = clips[nextClipIndex];
        nextClipIndex = (nextClipIndex + 1) % clips.Length;

        return clip;
    }
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance = null;

    //OptionUI 의 슬라이더가 쓰는 PlayerPrefs 키와 같아야 한다
    private const string BgmVolumeKey = "BGMVolume";
    private const string SfxVolumeKey = "SFXVolume";

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

        EnsureSfxPlayers();
        LoadSavedVolumes();
    }

    [Header("Audio Clip")]
    [SerializeField] private Sound[] BGM;
    [SerializeField] private Sound[] SFX;

    [Header("Audio Source (인스펙터에서 직접 연결)")]
    [SerializeField] private AudioSource BGMPlayer;
    [SerializeField] private AudioSource[] SFXPlayer;

    [Header("SFX 소스 (SFXPlayer 를 비워두면 이 개수만큼 자동으로 만들어 쓴다)")]
    [SerializeField] private int autoSfxSourceCount = 8;

    private int sfxRoundRobinIndex = 0;  //혹시 SFX 풀이 꽉 차면 재생 중인 거 하나 끊고 재사용하라는 목적으로 만듦

    //설정 슬라이더로 정하는 전체 배율(0~1). Sound.volume(소리별 기준 세기)에 곱해서 쓴다.
    //AudioSource.volume 에 직접 넣어두면 다음 재생에서 Sound.volume 으로 덮어써지기 때문에 따로 들고 있어야 한다.
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    //지금 재생 중인 소리의 Sound.volume. 슬라이더를 움직였을 때 재생 중인 소리에도 바로 반영하기 위해 기억한다.
    private float bgmBaseVolume = 1f;
    private float[] sfxBaseVolumes;

    public float BGMVolume
    {
        get { return bgmVolume; }
    }

    public float SFXVolume
    {
        get { return sfxVolume; }
    }

    //저장된 설정을 읽어서 처음부터 그 크기로 재생되게 한다 (저장값이 없으면 1).
    //저장은 OptionUI 가 슬라이더를 움직일 때 한다.
    private void LoadSavedVolumes()
    {
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));

        EnsureSfxBaseVolumes();
        ApplyBGMVolume();
        ApplySFXVolume();
    }

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
                bgmBaseVolume = s.volume;
                ApplyBGMVolume();
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
        EnsureSfxPlayers();

        if (SFXPlayer.Length == 0)
        {
            return;   //재생할 AudioSource 가 없으면 아무것도 하지 않는다
        }

        foreach (Sound s in SFX)
        {
            if (s.Name.Equals(name))
            {
                if (s.clips == null || s.clips.Length == 0)
                {
                    Debug.LogWarning($"AudioManager: '{name}' SFX에 클립이 비어있습니다.");
                    return;
                }

                AudioClip clip = s.NextClip();   //넣은 순서대로 하나씩 번갈아 재생
             
                for (int i = 0; i < SFXPlayer.Length; i++)
                {
                    if (SFXPlayer[i] != null && !SFXPlayer[i].isPlaying)
                    {
                        PlayOn(i, clip, s.volume);
                        return;
                    }
                }
                PlayOn(sfxRoundRobinIndex, clip, s.volume);
                sfxRoundRobinIndex = (sfxRoundRobinIndex + 1) % SFXPlayer.Length;
                return;
            }
        }
        Debug.Log($"해당 SFX를 가진 sound가 없음. [{name}]");
    }
    private void PlayOn(int sourceIndex, AudioClip clip, float baseVolume)
    {
        EnsureSfxBaseVolumes();

        AudioSource source = SFXPlayer[sourceIndex];
        sfxBaseVolumes[sourceIndex] = baseVolume;

        source.clip = clip;
        source.volume = baseVolume * sfxVolume;   //설정 배율을 곱해서 넣는다
        source.Play();
    }

    public void SetBGMVolume(float v)  //나중에 볼륨 슬라이더 UI에 써도 될 거 같은 훅
    {
        bgmVolume = Mathf.Clamp01(v);
        ApplyBGMVolume();
    }
    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        ApplySFXVolume();
    }

    private void ApplyBGMVolume()
    {
        if (BGMPlayer != null)
        {
            BGMPlayer.volume = bgmBaseVolume * bgmVolume;
        }
    }

    private void ApplySFXVolume()
    {
        if (SFXPlayer == null)
        {
            return;
        }

        EnsureSfxBaseVolumes();

        for (int i = 0; i < SFXPlayer.Length; i++)
        {
            if (SFXPlayer[i] != null)
            {
                SFXPlayer[i].volume = sfxBaseVolumes[i] * sfxVolume;
            }
        }
    }

    //SFXPlayer 를 인스펙터에서 비워둔 경우, 효과음을 재생할 AudioSource 를 이 오브젝트 아래에 만들어 둔다.
    //(비어 있으면 PlaySFX 가 쓸 소스가 없어서 소리가 아예 나지 않는다)
    private void EnsureSfxPlayers()
    {
        if (SFXPlayer != null && SFXPlayer.Length > 0)
        {
            return;
        }

        int count = Mathf.Max(1, autoSfxSourceCount);
        SFXPlayer = new AudioSource[count];

        for (int i = 0; i < count; i++)
        {
            GameObject host = new GameObject("SFXPlayer_" + i);
            host.transform.SetParent(transform, false);

            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;   //위치와 무관하게 들리는 2D 효과음
            SFXPlayer[i] = source;
        }

        EnsureSfxBaseVolumes();
    }

    //SFXPlayer 개수에 맞춰 기준 세기 배열을 준비한다 (기본 1). 인스펙터에서 개수를 바꿔도 맞춰진다.
    private void EnsureSfxBaseVolumes()
    {
        int count = 0;
        if (SFXPlayer != null)
        {
            count = SFXPlayer.Length;
        }

        if (sfxBaseVolumes != null && sfxBaseVolumes.Length == count)
        {
            return;
        }

        float[] resized = new float[count];
        for (int i = 0; i < count; i++)
        {
            resized[i] = 1f;

            if (sfxBaseVolumes != null && i < sfxBaseVolumes.Length)
            {
                resized[i] = sfxBaseVolumes[i];
            }
        }

        sfxBaseVolumes = resized;
    }
}
