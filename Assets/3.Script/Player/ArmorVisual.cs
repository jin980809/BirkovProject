using System;
using System.Collections.Generic;
using UnityEngine;

// 실제 장착된 헬멧/조끼에 맞는 모델을 각자의 소켓(HeadGear/Belly)에 붙여서 보여준다.
// WeaponVisual 과 완전히 같은 패턴 - 최초로 그 방어구가 필요해질 때 딱 한 번 Instantiate 해서
// 캐시해두고, 그 다음부터는 SetActive 로 켜고 끄기만 한다.
// 헬멧/조끼는 서로 다른 부위라 항상 최대 하나씩 동시에 보일 수 있다 (둘 다 안 꺼짐).
public class ArmorVisual : MonoBehaviour
{
    [Serializable]
    public struct ArmorModelMapping
    {
        public int itemId;
        public GameObject modelPrefab;
    }

    [Header("헬멧")]
    [Tooltip("헬멧 모델을 붙일 소켓 (Player 프리팹의 HeadGear)")]
    [SerializeField] private Transform helmetSocket;
    [SerializeField] private ArmorModelMapping[] helmetModels = Array.Empty<ArmorModelMapping>();

    [Header("조끼")]
    [Tooltip("조끼 모델을 붙일 소켓 (Player 프리팹의 Belly)")]
    [SerializeField] private Transform vestSocket;
    [SerializeField] private ArmorModelMapping[] vestModels = Array.Empty<ArmorModelMapping>();

    private PlayerArmorBridge armorBridge;

    private readonly Dictionary<int, GameObject> spawnedHelmets = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, GameObject> spawnedVests = new Dictionary<int, GameObject>();
    private int currentHelmetItemId = -1;
    private int currentVestItemId = -1;

    private void Awake()
    {
        armorBridge = FindAnyObjectByType<PlayerArmorBridge>();
    }

    private void Update()
    {
        if (armorBridge == null)
        {
            return;
        }

        int wantHelmet = armorBridge.EquippedHelmetItemId;
        if (wantHelmet != currentHelmetItemId)
        {
            ShowOnly(wantHelmet, helmetSocket, helmetModels, spawnedHelmets);
            currentHelmetItemId = wantHelmet;
        }

        int wantVest = armorBridge.EquippedVestItemId;
        if (wantVest != currentVestItemId)
        {
            ShowOnly(wantVest, vestSocket, vestModels, spawnedVests);
            currentVestItemId = wantVest;
        }
    }

    private void ShowOnly(int itemId, Transform socket, ArmorModelMapping[] models, Dictionary<int, GameObject> spawned)
    {
        foreach (KeyValuePair<int, GameObject> entry in spawned)
        {
            entry.Value.SetActive(entry.Key == itemId);
        }

        if (itemId < 0 || socket == null || spawned.ContainsKey(itemId))
        {
            return;
        }

        GameObject model = SpawnModel(itemId, socket, models);
        if (model != null)
        {
            spawned.Add(itemId, model);
        }
    }

    private GameObject SpawnModel(int itemId, Transform socket, ArmorModelMapping[] models)
    {
        for (int i = 0; i < models.Length; i++)
        {
            if (models[i].itemId != itemId || models[i].modelPrefab == null)
            {
                continue;
            }

            // 프리팹에 맞춰 둔 위치/회전/크기를 소켓 기준 로컬 값으로 그대로 쓴다 (WeaponVisual 과 동일)
            return Instantiate(models[i].modelPrefab, socket);
        }

        return null;
    }
}
