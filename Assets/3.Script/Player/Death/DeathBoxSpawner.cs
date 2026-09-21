using System.Collections;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.InventoryTest;
using Birdkov.NaYeongMin.Rng;
using UnityEngine;

// 죽은 자리에 놓이는 "잃어버린 장비 상자". 항상 하나만 존재한다.
//  - 전투 씬이 시작되면 deathbox.json 을 읽어서 기록된 자리에 상자를 세운다 (기록된 씬과 지금 씬이 같을 때만).
//  - 플레이어가 죽으면 PlayerDeathHandler 가 SpawnBox 를 불러서 죽은 자리에 바로 상자를 세운다.
//    이미 있던 상자는 지운다 (한 번에 하나만).
//  - 상자를 열어서 일부만 꺼냈다면 UI 를 닫을 때 남은 내용으로 기록을 갱신하고, 다 비우면 기록과 상자를 지운다.
//
// 상자는 적 사망 전리품과 같은 방식(LootDropObject + LootDropInteractable)으로 열린다. 그래서 상자 프리팹에는
// 이 둘과 트리거 콜라이더(PlayerInteraction 의 Interactable Mask 에 들어가는 레이어)가 있어야 한다.
//
// 세팅: 전투 씬에 빈 오브젝트를 만들어 이 컴포넌트를 붙이고 Box Prefab 을 연결한다.
public class DeathBoxSpawner : MonoBehaviour
{
    [Tooltip("잃어버린 장비 상자 프리팹. LootDropObject + LootDropInteractable + 트리거 콜라이더가 있어야 한다")]
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private InventoryTestBench inventoryBench;

    // 씬이 시작되고 나서 이 프레임 수만큼 기다렸다가 상자를 세운다. 씬에 있던 중복 벤치가 지워진 뒤여야
    // 상자의 상호작용(LootDropInteractable)이 살아있는 벤치를 찾는다.
    private const int SettleFrames = 2;

    private readonly DeathBoxStorage storage = new DeathBoxStorage();

    private DeathBoxRecord currentRecord;
    private GameObject spawnedBox;
    private LootDropObject spawnedDrop;
    private bool wasBenchOpen;

    private IEnumerator Start()
    {
        for (int i = 0; i < SettleFrames; i++)
        {
            yield return null;
        }

        DeathBoxRecord record = storage.Load();
        if (record != null && record.sceneName == gameObject.scene.name)
        {
            SpawnBox(record);
        }
    }

    private void Update()
    {
        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }

        if (spawnedDrop == null)
        {
            wasBenchOpen = false;
            return;
        }

        // UI 가 닫히는 순간에 남은 내용을 기록에 반영한다 (어떤 경로로 열렸든 상관없다)
        bool isOpen = inventoryBench != null && inventoryBench.IsOpen;
        if (wasBenchOpen && !isOpen)
        {
            SyncRecordWithBox();
        }

        wasBenchOpen = isOpen;
    }

    // record 의 자리에 상자를 세운다. 이미 있던 상자는 지운다.
    public void SpawnBox(DeathBoxRecord record)
    {
        RemoveBox();

        if (record == null)
        {
            return;
        }

        if (boxPrefab == null)
        {
            Debug.LogError("DeathBoxSpawner: Box Prefab 이 연결되지 않았습니다.", this);
            return;
        }

        // 큰 프리셋(Box5x6)이 아직 없으면 상자를 못 만든다. 기록은 남아 있어서 프리셋이 생기면 그때 나타난다.
        if (!record.TryBuildLoot(out LootContainerData loot))
        {
            Debug.LogWarning("DeathBoxSpawner: LootContainerSize.Box5x6 프리셋이 없어 상자를 세우지 못했습니다. " +
                             "기록(deathbox.json)은 남아 있습니다.", this);
            return;
        }

        GameObject box = Instantiate(boxPrefab, record.position, Quaternion.Euler(0f, record.rotationY, 0f));
        LootDropObject drop = box.GetComponentInChildren<LootDropObject>(true);
        if (drop == null)
        {
            Debug.LogError("DeathBoxSpawner: Box Prefab 에 LootDropObject 가 없습니다.", box);
            Destroy(box);
            return;
        }

        drop.SetLoot(loot);

        currentRecord = record;
        spawnedBox = box;
        spawnedDrop = drop;
        wasBenchOpen = false;
    }

    private void RemoveBox()
    {
        if (spawnedBox != null)
        {
            Destroy(spawnedBox);
        }

        spawnedBox = null;
        spawnedDrop = null;
        currentRecord = null;
    }

    // 상자 내용이 다 비었으면 기록과 상자를 지우고, 남았으면 남은 내용으로 기록을 갱신한다
    private void SyncRecordWithBox()
    {
        if (spawnedDrop == null || currentRecord == null)
        {
            return;
        }

        if (spawnedDrop.Loot.IsEmpty())
        {
            storage.Delete();
            RemoveBox();
            return;
        }

        currentRecord.slots.Clear();
        DeathBoxRecord.AppendNonEmpty(spawnedDrop.Loot.loot.slots, currentRecord.slots);
        storage.Save(currentRecord);
    }
}
