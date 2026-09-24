using System;
using System.Collections.Generic;
using UnityEngine;

// 실제 장착 무기에 맞는 총 모델을 손 소켓(Weapon_R)에 붙여서 보여준다.
// 최초로 그 무기가 필요해질 때 딱 한 번 Instantiate 해서 캐시해두고, 그 다음부터는
// SetActive 로 켜고 끄기만 한다 (매번 새로 만들고 지우지 않는다).
// 아이템 사용 중 등으로 무기가 SetArmed(false) 되면(PlayerController.IsArmed) 전부 숨긴다.
[RequireComponent(typeof(PlayerController))]
public class WeaponVisual : MonoBehaviour
{
    // 무기 itemId -> 총 모델 프리팹. 손 소켓 기준 위치/회전/크기는 프리팹 루트 Transform 에 직접 맞춰 둔다
    // (스폰할 때 프리팹의 로컬 Transform 값이 그대로 소켓 기준으로 적용된다).
    [Serializable]
    public struct WeaponModelMapping
    {
        public int weaponItemId;
        public GameObject modelPrefab;
    }

    [Tooltip("총 모델을 붙일 손 소켓 (Player 프리팹의 Weapon_R)")]
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private WeaponModelMapping[] models;

    private PlayerController player;
    private WeaponController weaponController;

    private readonly Dictionary<int, GameObject> spawnedModels = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, Transform> firePoints = new Dictionary<int, Transform>(); // 모델에 FirePoint 가 없으면 null
    private readonly Dictionary<int, WeaponModel> weaponModels = new Dictionary<int, WeaponModel>();
    private int currentShownItemId = -1;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        TryGetComponent(out weaponController);
    }

    private void Update()
    {
        if (weaponController == null || weaponSocket == null)
        {
            return;
        }

        int wantItemId = (player.IsArmed && weaponController.HasWeaponEquipped)
            ? weaponController.EquippedWeaponItemId
            : -1;

        if (wantItemId == currentShownItemId)
        {
            return;
        }

        ShowOnly(wantItemId);
        currentShownItemId = wantItemId;
    }

    // 해당 무기 모델의 FirePoint 를 돌려준다 (모델이나 FirePoint 가 없으면 null).
    // WeaponController 가 발사할 때 호출한다. 아직 스폰 전이면 여기서 먼저 스폰하므로,
    // 무기를 바꾼 직후 이 컴포넌트의 Update 보다 발사가 먼저 와도 올바른 총구에서 나간다.
    public Transform GetFirePoint(int itemId)
    {
        Transform point = null;

        if (itemId >= 0 && weaponSocket != null)
        {
            EnsureModel(itemId);
            firePoints.TryGetValue(itemId, out point);
        }

        return point;
    }

    // 해당 무기 모델의 WeaponModel 을 돌려준다 (모델이 없거나 WeaponModel 이 안 붙어 있으면 null).
    // 머즐플래시/탄피 같은 이펙트를 재생할 때 쓴다. GetFirePoint 와 마찬가지로 아직 스폰 전이면 먼저 스폰한다.
    public WeaponModel GetWeaponModel(int itemId)
    {
        WeaponModel found = null;

        if (itemId >= 0 && weaponSocket != null)
        {
            EnsureModel(itemId);
            weaponModels.TryGetValue(itemId, out found);
        }

        return found;
    }

    private void ShowOnly(int itemId)
    {
        if (itemId >= 0)
        {
            EnsureModel(itemId);
        }

        foreach (KeyValuePair<int, GameObject> entry in spawnedModels)
        {
            entry.Value.SetActive(entry.Key == itemId);
        }
    }

    // 처음 필요할 때 한 번만 스폰하고, 프리팹 루트의 WeaponModel 에 연결된 FirePoint 를 같이 캐시한다.
    // 새로 만든 모델은 일단 숨겨두고, 보일지 말지는 ShowOnly 가 정한다.
    private void EnsureModel(int itemId)
    {
        if (!spawnedModels.ContainsKey(itemId))
        {
            GameObject model = SpawnModel(itemId);
            if (model != null)
            {
                model.SetActive(itemId == currentShownItemId);
                spawnedModels.Add(itemId, model);

                Transform point = null;
                if (model.TryGetComponent(out WeaponModel weaponModel) && weaponModel.FirePoint != null)
                {
                    point = weaponModel.FirePoint;
                }
                else
                {
                    Debug.LogWarning("WeaponVisual: '" + model.name + "' 프리팹 루트에 WeaponModel(FirePoint 연결)이 없어 " +
                                     "WeaponController 의 기본 firePoint 에서 발사합니다.", model);
                }

                firePoints[itemId] = point;

                if (model.TryGetComponent(out WeaponModel modelComponent))
                {
                    weaponModels[itemId] = modelComponent;
                }
            }
        }
    }

    private GameObject SpawnModel(int itemId)
    {
        GameObject instance = null;

        if (models != null)
        {
            for (int i = 0; i < models.Length; i++)
            {
                if (models[i].weaponItemId == itemId && models[i].modelPrefab != null)
                {
                    // 프리팹에 맞춰 둔 위치/회전/크기를 소켓 기준 로컬 값으로 그대로 쓴다
                    instance = Instantiate(models[i].modelPrefab, weaponSocket);
                    break;
                }
            }
        }

        return instance;
    }
}
