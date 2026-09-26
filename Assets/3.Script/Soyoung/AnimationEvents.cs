using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    //블렌드 트리에서 여러 클립이 섞이거나(대각선 이동) 상태가 전환되는 동안에는 여러 클립의
    //이벤트가 거의 동시에 들어와서 발소리가 겹쳐 들린다. 이 시간 안에 다시 들어온 발소리는 무시한다.
    //실제 걸음 간격보다는 짧게 둬야 한다 (걷기 약 0.5초, 달리기 약 0.3초 간격).
    [SerializeField] private float minFootstepInterval = 0.15f;

    private float lastFootstepTime = -999f;

    //효과음들을 추가하는 스크립트입니다. 꼭 사운드 매니저의 Name에서 입력한 것과 동일하게 입력해야지 소리가 출력됨!
    public void PlayFootstepSFX()
    {
        if (Time.time - lastFootstepTime < minFootstepInterval)
        {
            return;
        }

        lastFootstepTime = Time.time;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("Footstep");
        }
    }

    public void PlayRollSFX()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("Roll");
        }
    }
    public void PlayRunSFX()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("Run");
        }
    }
    public void PlayShotReload()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("ShotgunReload");
        }
    }
}
