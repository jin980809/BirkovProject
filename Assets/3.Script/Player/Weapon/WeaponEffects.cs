using UnityEngine;

// 총을 한 발 쏠 때마다 머즐플래시와 탄피 배출 이펙트를 재생한다.
//  - WeaponController.ShotFired 를 구독한다. 실제로 총알이 나간 발에만 재생되고(탄이 없어 막힌 발은 제외),
//    샷건 펠릿 수와는 무관하게 1회.
//  - 재생할 파티클은 지금 장착한 총 모델의 WeaponModel(머즐플래시/조명/탄피)에서 가져온다.
//    총 모델에 해당 슬롯이 비어 있으면 그 이펙트는 건너뛴다.
//  - 파티클은 발사마다 처음부터 다시 재생한다 (연사 무기도 한 발마다 한 번씩 번쩍인다).
//    이미 나와 있는 입자는 지우지 않으므로, 앞 발의 탄피는 남아서 떨어진다.
//  - 머즐플래시 조명은 발사 순간에만 잠깐 켜진다.
//
// 세팅: 플레이어(PlayerController 와 같은 오브젝트)에 이 컴포넌트만 붙이면 된다.
[RequireComponent(typeof(PlayerController))]
public class WeaponEffects : MonoBehaviour
{
    [Tooltip("머즐플래시 조명이 켜져 있는 시간(초). 연사 무기는 짧아야 깜빡이지 않고 번쩍인다")]
    [SerializeField] private float muzzleLightDuration = 0.05f;

    private WeaponController weapon;
    private WeaponVisual weaponVisual;

    private Light activeLight;
    private float lightOffTime;

    private void Awake()
    {
        TryGetComponent(out weapon);
        TryGetComponent(out weaponVisual);
    }

    private void OnEnable()
    {
        if (weapon != null)
        {
            weapon.ShotFired += HandleShotFired;
        }
    }

    private void OnDisable()
    {
        if (weapon != null)
        {
            weapon.ShotFired -= HandleShotFired;
        }

        TurnOffLight();
    }

    private void Update()
    {
        if (activeLight != null && Time.time >= lightOffTime)
        {
            TurnOffLight();
        }
    }

    private void HandleShotFired()
    {
        if (weapon == null || weaponVisual == null || !weapon.HasWeaponEquipped)
        {
            return;
        }

        WeaponModel model = weaponVisual.GetWeaponModel(weapon.EquippedWeaponItemId);
        if (model == null)
        {
            return;
        }

        Restart(model.MuzzleFlash);
        Restart(model.ShellEject);
        FlashLight(model.MuzzleLight);
    }

    // 재생 중이어도 처음부터 다시 재생한다. 이미 나온 입자는 그대로 두고 새로 내보내는 것만 멈췄다 다시 시작한다.
    private void Restart(ParticleSystem particles)
    {
        if (particles == null || !particles.gameObject.activeInHierarchy)
        {
            return;
        }

        particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        particles.Play(true);
    }

    private void FlashLight(Light muzzleLight)
    {
        if (muzzleLight == null || !muzzleLight.gameObject.activeInHierarchy)
        {
            return;
        }

        // 무기를 바꾼 직후 앞 총의 조명이 아직 켜져 있으면 끄고 넘어간다
        if (activeLight != null && activeLight != muzzleLight)
        {
            TurnOffLight();
        }

        muzzleLight.enabled = true;
        activeLight = muzzleLight;
        lightOffTime = Time.time + muzzleLightDuration;
    }

    private void TurnOffLight()
    {
        if (activeLight != null)
        {
            activeLight.enabled = false;
            activeLight = null;
        }
    }
}
