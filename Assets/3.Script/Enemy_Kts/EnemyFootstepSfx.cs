using UnityEngine;

// 적 발소리. 적은 플레이어와 같은 걷기 클립(TinyBirdDuo 의 WalkFWDOneWeapon_IP 등)을 쓰기 때문에,
// 그 클립에 박아둔 PlayFootstepSFX 이벤트가 적에게도 전달된다. 받을 컴포넌트가 없으면
// "AnimationEvent 'PlayFootstepSFX' has no receiver!" 경고가 계속 찍힌다.
// 그래서 적 쪽에서도 같은 이름의 함수를 받아, 위치에 따라 크게/작게 들리는 3D 소리로 재생한다.
//
// AudioManager 의 SFX 를 쓰지 않는 이유: 그쪽은 화면 전체에 같은 크기로 나오는 2D 소리라서,
// 멀리 있는 적의 발소리가 플레이어 발소리와 똑같은 크기로 들려버린다.
// 대신 사운드 설정의 SFX 볼륨은 그대로 따른다 (AudioManager.SFXVolume).
//
// 붙이는 곳: 적 프리팹에서 Animator 가 붙어 있는 오브젝트 (애니메이션 이벤트는 그 오브젝트에만 전달된다).
// Clips 를 비워두면 소리 없이 경고만 사라진다.
public class EnemyFootstepSfx : MonoBehaviour
{
    [Tooltip("발소리 클립들. 여러 개면 넣은 순서대로 번갈아 재생한다. 비워두면 소리가 나지 않는다")]
    [SerializeField] private AudioClip[] clips;

    [Tooltip("재생에 쓸 AudioSource. 비워두면 이 오브젝트에 3D 설정으로 하나 만든다")]
    [SerializeField] private AudioSource source;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.7f;

    [Tooltip("이 거리부터 소리가 줄어들기 시작한다")]
    [SerializeField] private float minDistance = 3f;

    [Tooltip("이 거리를 넘으면 들리지 않는다")]
    [SerializeField] private float maxDistance = 20f;

    [Tooltip("이 시간 안에 다시 들어온 발소리는 무시한다 (블렌드 트리에서 여러 클립이 섞일 때 겹침 방지)")]
    [SerializeField] private float minInterval = 0.15f;

    private int nextClipIndex;
    private float lastPlayTime = -999f;

    // 애니메이션 이벤트가 부르는 함수. 이름이 클립의 Function 과 같아야 한다.
    public void PlayFootstepSFX()
    {
        if (Time.time - lastPlayTime < minInterval)
        {
            return;
        }

        AudioClip clip = NextClip();

        if (clip == null)
        {
            return;
        }

        lastPlayTime = Time.time;

        EnsureSource();
        source.volume = volume * GetSfxVolume();
        source.PlayOneShot(clip);
    }

    // 달리기 클립에 이벤트를 넣는 경우에도 같은 발소리로 처리한다 (소영님 AnimationEvents 와 이름을 맞춤)
    public void PlayRunSFX()
    {
        PlayFootstepSFX();
    }

    private AudioClip NextClip()
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        if (nextClipIndex >= clips.Length)
        {
            nextClipIndex = 0;
        }

        AudioClip clip = clips[nextClipIndex];
        nextClipIndex = (nextClipIndex + 1) % clips.Length;

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
            source = gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;                        // 완전한 3D - 거리와 방향에 따라 들린다
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
    }
}
