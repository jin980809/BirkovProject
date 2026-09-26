using UnityEngine;

// 적 총소리 / 재장전음. 위치에 따라 크게·작게 들리는 3D 소리로 재생한다.
//
// AudioManager 의 SFX 를 쓰지 않는 이유: 그쪽은 화면 전체에 같은 크기로 나오는 2D 소리라서,
// 맵 반대편 적의 총소리가 플레이어 총소리와 같은 크기로 들려버린다.
// 사운드 설정의 SFX 볼륨은 그대로 따른다 (AudioManager.SFXVolume).
//
// 붙이는 곳: 적 프리팹 루트 (EnemyShot 과 같은 오브젝트). EnemyShot 이 발사/재장전할 때 불러준다.
// 클립을 비워두면 그 소리만 나지 않는다.
public class EnemyWeaponSfx : MonoBehaviour
{
    [Tooltip("발사음. 여러 개면 넣은 순서대로 번갈아 재생한다 (무기에 맞는 것: gig_fire / shotgun_fire / dol_fire / sniper_fire)")]
    [SerializeField] private AudioClip[] fireClips;

    [Tooltip("재장전음 (gig_reload / shotgun_reload / dol_reload / sniper_reload)")]
    [SerializeField] private AudioClip reloadClip;

    [Tooltip("소리가 나는 위치. 비우면 이 오브젝트 위치에서 난다 (총구를 넣으면 더 정확하다)")]
    [SerializeField] private Transform soundOrigin;

    [Tooltip("재생에 쓸 AudioSource. 비워두면 3D 설정으로 하나 만든다")]
    [SerializeField] private AudioSource source;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.8f;

    [Tooltip("이 거리부터 소리가 줄어들기 시작한다")]
    [SerializeField] private float minDistance = 5f;

    [Tooltip("이 거리를 넘으면 들리지 않는다. 총소리는 발소리보다 멀리 들려야 한다")]
    [SerializeField] private float maxDistance = 40f;

    private int nextFireIndex;

    // EnemyShot 이 한 발 쏠 때마다 부른다
    public void PlayFire()
    {
        Play(NextFireClip());
    }

    // EnemyShot 이 재장전을 시작할 때 부른다
    public void PlayReload()
    {
        Play(reloadClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsureSource();
        source.volume = volume * GetSfxVolume();
        source.PlayOneShot(clip);
    }

    private AudioClip NextFireClip()
    {
        if (fireClips == null || fireClips.Length == 0)
        {
            return null;
        }

        if (nextFireIndex >= fireClips.Length)
        {
            nextFireIndex = 0;
        }

        AudioClip clip = fireClips[nextFireIndex];
        nextFireIndex = (nextFireIndex + 1) % fireClips.Length;

        return clip;
    }

    // 사운드 설정의 SFX 볼륨을 따라간다 (매니저가 없으면 그대로 1)
    private float GetSfxVolume()
    {
        if (AudioManager.instance != null)
        {
            return AudioManager.instance.SFXVolume;
        }

        return 1f;
    }

    private void EnsureSource()
    {
        if (source == null)
        {
            GameObject host = gameObject;

            if (soundOrigin != null)
            {
                host = soundOrigin.gameObject;
            }

            // 이미 있는 AudioSource 를 재사용하지 않고 전용으로 하나 만든다.
            // 발소리(EnemyFootstepSfx)와 같은 소스를 쓰면 서로의 거리 설정(min/max)을 덮어써서
            // 총소리가 발소리 거리로, 또는 그 반대로 들리게 된다.
            source = host.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;                        // 완전한 3D - 거리와 방향에 따라 들린다
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
    }
}
