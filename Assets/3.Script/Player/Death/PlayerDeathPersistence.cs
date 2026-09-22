using System.Collections;
using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Integration;
using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

// 사망 오브젝트(분실물)를 씬을 나갔다 와도 그 자리에 남아 있게 한다.
// 생성/UI/회수는 NaYeongMin 쪽(PlayerDeathSpawner / PlayerDeathContainer)이 담당하고,
// 여기서는 그쪽이 알려주는 시점에 맞춰 위치와 내용물을 파일(deathbox.json)에 기록하고 되살린다.
//  - 죽어서 생성될 때(Spawned): 위치 + 내용물을 기록한다.
//  - 내용물이 바뀔 때(ContentsChanged): 기록을 갱신한다. 다 꺼내면 기록을 지운다.
//  - 씬이 시작될 때: 기록된 씬과 같으면 그 자리에 다시 세운다 (SpawnFromItems).
//  - 항상 하나만 남긴다. 새로 죽으면 이전 오브젝트를 지운다.
//
// 세팅: 전투 씬의 아무 오브젝트(또는 PlayerDeathSpawner 가 붙은 플레이어)에 이 컴포넌트를 붙인다.
public class PlayerDeathPersistence : MonoBehaviour
{
    [Tooltip("사망 오브젝트를 만드는 NaYeongMin 쪽 스포너. 비우면 씬에서 찾는다")]
    [SerializeField] private PlayerDeathSpawner spawner;
    [Tooltip("다 꺼낸 빈 사망 오브젝트를 씬에서 치울지. 끄면 빈 상태로 남는다")]
    [SerializeField] private bool destroyWhenEmptied = true;

    // 씬이 시작되고 이 프레임 수만큼 기다렸다가 되살린다. 씬에 있던 중복 벤치/UI 가 정리된 뒤여야
    // 사망 오브젝트의 상호작용이 살아있는 벤치를 찾는다 (PersistentUiRoot 참고).
    private const int SettleFrames = 2;

    private readonly DeathBoxStorage storage = new DeathBoxStorage();

    private PlayerDeathContainer currentCorpse;

    private void Awake()
    {
        if (spawner == null)
        {
            spawner = FindAnyObjectByType<PlayerDeathSpawner>();
        }
    }

    // 스포너가 인스펙터로 물고 있는 벤치/플레이어 참조가 죽었으면 지금 씬 것으로 다시 물려준다.
    // 인벤토리 UI 캔버스는 씬을 넘어 유지되고(PersistentUiRoot) 새 씬에 있던 중복 벤치는 지워지므로,
    // 씬에 저장된 참조는 전환 직후 null 이 된다. 그 상태로 죽으면 스포너가 사망 처리를 통째로 건너뛴다
    // (분실물도 안 생기고 인벤토리도 안 비워진다). 팀원 파일은 건드리지 않고 여기서 채워 넣는다.
    private void EnsureSpawnerLinks()
    {
        if (spawner == null)
        {
            return;
        }

        if (spawner.inventoryBench == null)
        {
            spawner.inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }

        if (spawner.playerVitals == null)
        {
            spawner.playerVitals = FindAnyObjectByType<PlayerVitals>();

            // 스포너는 OnEnable 에서 Died 를 구독한다. 그때 playerVitals 가 없었으면 구독을 못 한 상태이므로,
            // 지금 채워 넣은 뒤 껐다 켜서 다시 구독하게 한다.
            if (spawner.playerVitals != null && spawner.isActiveAndEnabled)
            {
                spawner.enabled = false;
                spawner.enabled = true;
            }
        }
    }

    private void OnEnable()
    {
        if (spawner != null)
        {
            spawner.Spawned += HandleSpawned;
        }
    }

    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.Spawned -= HandleSpawned;
        }

        Unsubscribe(currentCorpse);
    }

    private IEnumerator Start()
    {
        if (spawner == null)
        {
            Debug.LogWarning("PlayerDeathPersistence: 씬에서 PlayerDeathSpawner 를 찾지 못했습니다. " +
                             "사망 오브젝트가 저장/복원되지 않습니다.", this);
            yield break;
        }

        for (int i = 0; i < SettleFrames; i++)
        {
            yield return null;
        }

        // 중복 UI 캔버스가 정리된 뒤에 한 번만 맞춰 준다 (매 프레임 찾지 않는다)
        EnsureSpawnerLinks();
        RestoreFromRecord();
    }

    // 저장된 기록이 이 씬 것이면 그 자리에 다시 세운다
    private void RestoreFromRecord()
    {
        DeathBoxRecord record = storage.Load();

        if (record != null && record.sceneName == gameObject.scene.name)
        {
            PlayerDeathContainer corpse = spawner.SpawnFromItems(record.position, record.slots);
            if (corpse == null)
            {
                Debug.LogWarning("PlayerDeathPersistence: 저장된 분실물을 되살리지 못했습니다. " +
                                 "스포너의 Death Prefab 연결을 확인하세요.", this);
            }
        }
    }

    // 죽어서 새로 생겼을 때와, 위에서 되살렸을 때 모두 불린다
    private void HandleSpawned(PlayerDeathContainer corpse)
    {
        if (corpse == null)
        {
            return;
        }

        // 분실물은 항상 하나만 남긴다 - 새로 죽었으면 이전 것은 치운다
        if (currentCorpse != null && currentCorpse != corpse)
        {
            Unsubscribe(currentCorpse);
            Destroy(currentCorpse.gameObject);
        }

        currentCorpse = corpse;
        corpse.ContentsChanged += HandleContentsChanged;

        // 생성 시점에는 비어 있어도 오브젝트를 치우지 않는다 - 빈손으로 죽으면 분실물이 생기자마자
        // 사라져 버린다. 치우는 건 플레이어가 실제로 다 꺼내 갔을 때(HandleContentsChanged)만 한다.
        SaveCorpse(corpse, false);
    }

    // 플레이어가 일부만 꺼내 갔을 때도 남은 내용으로 갱신한다
    private void HandleContentsChanged(PlayerDeathContainer corpse)
    {
        SaveCorpse(corpse, destroyWhenEmptied);
    }

    private void SaveCorpse(PlayerDeathContainer corpse, bool destroyIfEmpty)
    {
        List<GridSlotData> items = CopyNonEmpty(corpse.CopyItems());

        if (items.Count == 0)
        {
            // 남은 게 없으면 기록을 지운다 - 다음 판에 다시 생기지 않게
            storage.Delete();

            if (destroyIfEmpty)
            {
                Unsubscribe(corpse);

                if (currentCorpse == corpse)
                {
                    currentCorpse = null;
                }

                Destroy(corpse.gameObject);
            }

            return;
        }

        DeathBoxRecord record = new DeathBoxRecord
        {
            sceneName = gameObject.scene.name,
            position = corpse.transform.position,
            slots = items
        };

        storage.Save(record);
    }

    private void Unsubscribe(PlayerDeathContainer corpse)
    {
        if (corpse != null)
        {
            corpse.ContentsChanged -= HandleContentsChanged;
        }
    }

    // 슬롯 목록에서 비어 있지 않은 것만 복사해 모은다
    private List<GridSlotData> CopyNonEmpty(IEnumerable<GridSlotData> source)
    {
        List<GridSlotData> result = new List<GridSlotData>();

        if (source != null)
        {
            foreach (GridSlotData slot in source)
            {
                if (slot != null && !slot.IsEmpty())
                {
                    result.Add(new GridSlotData
                    {
                        itemId = slot.itemId,
                        amount = slot.amount,
                        remainingRounds = slot.remainingRounds,
                        durabilityDamage = slot.durabilityDamage
                    });
                }
            }
        }

        return result;
    }
}
