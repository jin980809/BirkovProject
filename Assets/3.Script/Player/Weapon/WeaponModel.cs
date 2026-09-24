using UnityEngine;

// 총 모델 프리팹 루트에 붙인다. 이 모델에서 총알이 나갈 위치(총구)를 인스펙터에서 직접 연결해 둔다.
// WeaponVisual 이 모델을 스폰한 뒤 이 컴포넌트를 읽어 WeaponController 에 발사 위치로 넘겨준다.
public class WeaponModel : MonoBehaviour
{
    [Tooltip("총알이 나갈 위치. 프리팹 안의 총구 쪽 빈 오브젝트를 연결한다")]
    [SerializeField] private Transform firePoint;

    [Tooltip("발사할 때 재생할 머즐플래시(파티클). 비우면 FirePoint 자식에서 처음 찾은 ParticleSystem 을 쓴다")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [Tooltip("머즐플래시와 함께 잠깐 켜질 조명. 비우면 머즐플래시 자식에서 찾고, 없으면 조명은 쓰지 않는다")]
    [SerializeField] private Light muzzleLight;
    [Tooltip("발사할 때 재생할 탄피 배출(파티클). 총의 탄피 배출구 위치에 두고 연결한다. 비우면 탄피가 없다")]
    [SerializeField] private ParticleSystem shellEject;

    public Transform FirePoint
    {
        get { return firePoint; }
    }

    public ParticleSystem MuzzleFlash
    {
        get { return muzzleFlash; }
    }

    public Light MuzzleLight
    {
        get { return muzzleLight; }
    }

    public ParticleSystem ShellEject
    {
        get { return shellEject; }
    }

    // 머즐플래시가 자동 재생/반복 상태로 남아 있어도(WarFX 원본 프리팹은 둘 다 켜져 있다) 평소엔 가만히 있고
    // 발사할 때만 재생되게 정리해 둔다. 조명도 꺼 둔다 (WeaponEffects 가 발사할 때만 잠깐 켠다).
    private void Awake()
    {
        if (muzzleFlash == null && firePoint != null)
        {
            muzzleFlash = firePoint.GetComponentInChildren<ParticleSystem>(true);
        }

        if (muzzleLight == null && muzzleFlash != null)
        {
            muzzleLight = muzzleFlash.GetComponentInChildren<Light>(true);
        }

        if (muzzleFlash != null)
        {
            ParticleSystem[] systems = muzzleFlash.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
                main.loop = false;
                main.playOnAwake = false;

                // WarFX 플래시는 초당 10개씩 계속 내보내는 연사 데모용 설정(Rate over Time)이라, 그대로 한 번 재생하면
                // 1초 동안 여러 번 번쩍인다. 한 발에 한 번만 나오도록 Rate 는 끄고 시간 0에 1개만 내보내게 바꾼다.
                // 프리팹에는 개수가 0인 빈 버스트가 들어 있으므로, 실제로 내보내는 버스트가 있는지를 보고 없을 때만 넣는다.
                ParticleSystem.EmissionModule emission = systems[i].emission;
                emission.rateOverTime = 0f;
                if (!HasEffectiveBurst(emission))
                {
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
                }
            }

            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (muzzleLight != null)
        {
            muzzleLight.enabled = false;
        }
    }

    // 파티클 하나를 재생했을 때 실제로 입자를 내보내는 버스트(개수 1 이상)가 있는지
    private bool HasEffectiveBurst(ParticleSystem.EmissionModule emission)
    {
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
        emission.GetBursts(bursts);

        for (int i = 0; i < bursts.Length; i++)
        {
            ParticleSystem.MinMaxCurve count = bursts[i].count;
            bool isConstant = count.mode == ParticleSystemCurveMode.Constant ||
                              count.mode == ParticleSystemCurveMode.TwoConstants;

            if (!isConstant || count.constantMax > 0f)
            {
                return true;
            }
        }

        return false;
    }
}
