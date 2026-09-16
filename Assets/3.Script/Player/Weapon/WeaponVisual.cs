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
    [Serializable]
    public struct WeaponModelMapping
    {
        public int weaponItemId;
        public GameObject modelPrefab;

        [Tooltip("손 소켓(Weapon_R) 기준 위치 오프셋 - 모델 원본 좌표가 소켓 기준이 아니라서 직접 맞춰야 한다")]
        public Vector3 positionOffset;
        [Tooltip("손 소켓 기준 회전 오프셋 (오일러 각)")]
        public Vector3 rotationOffsetEuler;
        [Tooltip("크기 배율")]
        public Vector3 scale;
    }

    [Tooltip("총 모델을 붙일 손 소켓 (Player 프리팹의 Weapon_R)")]
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private WeaponModelMapping[] models = Array.Empty<WeaponModelMapping>();

    private PlayerController player;
    private WeaponController weaponController;

    private readonly Dictionary<int, GameObject> spawnedModels = new Dictionary<int, GameObject>();
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

    private void ShowOnly(int itemId)
    {
        foreach (KeyValuePair<int, GameObject> entry in spawnedModels)
        {
            entry.Value.SetActive(entry.Key == itemId);
        }

        if (itemId < 0 || spawnedModels.ContainsKey(itemId))
        {
            return;
        }

        GameObject model = SpawnModel(itemId);
        if (model != null)
        {
            spawnedModels.Add(itemId, model);
        }
    }

    private GameObject SpawnModel(int itemId)
    {
        for (int i = 0; i < models.Length; i++)
        {
            if (models[i].weaponItemId != itemId || models[i].modelPrefab == null)
            {
                continue;
            }

            GameObject instance = Instantiate(models[i].modelPrefab, weaponSocket);
            instance.transform.localPosition = models[i].positionOffset;
            instance.transform.localRotation = Quaternion.Euler(models[i].rotationOffsetEuler);
            // scale 을 인스펙터에서 세팅 안 했으면(기본값 0,0,0) 원본 크기(1,1,1)를 그대로 쓴다
            instance.transform.localScale = models[i].scale == Vector3.zero ? Vector3.one : models[i].scale;
            return instance;
        }

        return null;
    }
}
