using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    //효과음들을 추가하는 스크립트입니다. 꼭 사운드 매니저의 Name에서 입력한 것과 동일하게 입력해야지 소리가 출력됨!
    public void PlayFootstepSFX()
    {
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
