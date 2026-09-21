using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

// 플레이어가 죽었을 때의 처리를 한 곳에서 순서대로 한다.
//  1) 열려 있는 인벤토리/상자 UI 를 닫는다 (입력은 PlayerController.IsDead 가 막는다).
//  2) 가방/장비에 있던 아이템을 기록(deathbox.json)에 남기고, 죽은 자리에 상자를 세운다.
//     아이템이 하나도 없으면 기록을 건드리지 않는다 (이전 상자가 그대로 남는다).
//  3) 가방/장비/퀵슬롯을 비운다 (소지금과 창고는 유지). 바로 저장한다.
//  4) 사망 패널(플레이 시간 + 로비 버튼)을 띄운다.
// 로비 진입 시 저장과 다음 판에서 상자를 세우는 일은 SaveCoordinator / DeathBoxSpawner 가 한다.
//
// 세팅: 플레이어(PlayerController 와 같은 오브젝트)에 이 컴포넌트를 붙인다.
[RequireComponent(typeof(PlayerVitals))]
public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private InventoryTestBench inventoryBench;
    [Tooltip("사망 패널. 비우면 씬에서 찾는다 (꺼져 있어도 찾는다)")]
    [SerializeField] private DeathPanelUI deathPanel;
    [Tooltip("사망 위치에서 아래로 바닥을 찾는 최대 거리. 상자를 바닥에 붙이는 데 쓴다")]
    [SerializeField] private float groundSearchDistance = 6f;

    private const int GroundHitBufferSize = 16;

    private readonly DeathBoxStorage storage = new DeathBoxStorage();
    private readonly RaycastHit[] groundHits = new RaycastHit[GroundHitBufferSize];

    private PlayerVitals vitals;
    private bool handled;

    private void Awake()
    {
        vitals = GetComponent<PlayerVitals>();
    }

    private void OnEnable()
    {
        if (vitals != null)
        {
            vitals.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (vitals != null)
        {
            vitals.Died -= HandleDied;
        }
    }

    private void HandleDied()
    {
        if (handled)
        {
            return;
        }

        handled = true;

        // TODO: 사망 애니메이션 (나중에 연출을 넣을 자리)

        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }

        if (inventoryBench != null && inventoryBench.IsReady && inventoryBench.PlayerData != null)
        {
            LoseInventory(inventoryBench.PlayerData);
        }

        if (deathPanel == null)
        {
            deathPanel = FindAnyObjectByType<DeathPanelUI>(FindObjectsInactive.Include);
        }

        if (deathPanel != null)
        {
            // 이번 판의 시간: 이 전투 씬이 시작된 뒤부터 지금까지
            deathPanel.Show(Time.timeSinceLevelLoad);
        }
        else
        {
            Debug.LogWarning("PlayerDeathHandler: DeathPanelUI 가 씬에 없어 사망 패널을 띄우지 못했습니다.", this);
        }
    }

    private void LoseInventory(PlayerInventoryData data)
    {
        inventoryBench.CloseCurrent();

        // 비우기 전에 남길 아이템을 먼저 모은다
        DeathBoxRecord record = new DeathBoxRecord();
        DeathBoxRecord.AppendNonEmpty(data.equipmentSlots.slots, record.slots);
        DeathBoxRecord.AppendNonEmpty(data.inventory.slots, record.slots);

        if (record.slots.Count > 0)
        {
            record.sceneName = gameObject.scene.name;
            record.position = FindGroundPosition();
            record.rotationY = transform.eulerAngles.y;

            storage.Save(record);

            DeathBoxSpawner spawner = FindAnyObjectByType<DeathBoxSpawner>();
            if (spawner != null)
            {
                spawner.SpawnBox(record);
            }
        }

        // 소지금은 잃지 않는다. KillPlayer 가 소지금까지 비우는 버전이어도 값이 유지되도록 되돌려 놓는다.
        int keptCurrency = data.currency;
        inventoryBench.KillPlayer();
        data.currency = keptCurrency;
        inventoryBench.Refresh();

        inventoryBench.SaveGame();
    }

    // 사망 위치 바로 아래의 바닥 지점. 플레이어 자신과 트리거는 무시한다. 못 찾으면 사망 위치 그대로.
    private Vector3 FindGroundPosition()
    {
        Vector3 origin = transform.position + Vector3.up;
        int count = Physics.RaycastNonAlloc(
            origin, Vector3.down, groundHits, groundSearchDistance + 1f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        Vector3 result = transform.position;
        float nearest = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.distance < nearest && hit.collider.GetComponentInParent<PlayerController>() == null)
            {
                nearest = hit.distance;
                result = hit.point;
            }
        }

        return result;
    }
}
