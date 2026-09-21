using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

// 테스트용: 게임 시작 시 돌격소총/스나이퍼를 무기 슬롯 1/2 에 장착해주고, 권총/샷건은
// 가방에 넣어준다(무기 슬롯 2개가 이미 차 있으니 테스트할 땐 직접 드래그로 바꿔 끼우면 된다).
// 각 무기 전용 탄약도 가방에 같이 넣어준다. 헬멧/조끼는 각각 가장 낮은 등급(구형)을 장비 슬롯에
// 장착해주고, 나머지 등급(튼튼/최고급)은 가방에 넣어서 드래그로 바꿔 낄 수 있게 한다.
// 실제 게임 진행용 스크립트가 아니라 테스트 편의를 위한 것 - 다 쓰면 이 컴포넌트를 씬에서
// 지우거나 비활성화하면 된다.
// NaYeongMin 파일은 건드리지 않고 공개 API(PlayerInventoryService)만 쓴다.
public class DebugInventorySeeder : MonoBehaviour
{
    [Tooltip("스택이 안 되는 아이템(무기/장비/특수)을 창고에 몇 개씩 넣을지")]
    [SerializeField] private int nonStackableCopies = 3;

    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private InventoryTestBench inventoryBench;

    // 인벤토리 데이터는 씬을 넘어서 유지되므로(PersistentUiRoot + 벤치), 씬마다 다시 지급하면 계속 쌓인다.
    // 게임 실행당 한 번만 지급한다. (static 이라 플레이 모드를 다시 시작하면 초기화된다)
    private static bool didSeedThisSession;

    private void Awake()
    {
        if (itemDatabase == null)
        {
            itemDatabase = FindAnyObjectByType<ItemDatabase>();
        }

        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }
    }

    private void Update()
    {
        // 씬 전환 직후 중복 벤치(곧 파괴됨)를 잡았을 수 있으니 참조가 죽었으면 다시 찾는다
        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }

        // InventoryTestBench 가 자기 데이터를 준비하는 타이밍이 늦을 수 있어서,
        // 준비될 때까지 매 프레임 확인하다가 딱 한 번만 넣어준다.
        if (didSeedThisSession || inventoryBench == null || !inventoryBench.IsReady ||
            itemDatabase == null || itemDatabase.Catalog == null)
        {
            return;
        }

        // 저장 파일이 있으면 SaveCoordinator 가 게임을 켠 직후 그걸 불러온다. 그 불러오기가 끝난 뒤에 판단해야
        // 지급한 것이 덮어써지지 않고, 이미 저장된 진행이 있을 땐 지급하지 않는다 (안 그러면 실행할 때마다 창고가 쌓인다).
        SaveCoordinator saveCoordinator = SaveCoordinator.Instance;
        if (!saveCoordinator.InitialLoadDone)
        {
            return;
        }

        didSeedThisSession = true;

        if (saveCoordinator.LoadedFromSave)
        {
            return;
        }

        Seed();
    }

    // 창고에 모든 아이템을 최대치로 채운다. 스택이 되는 아이템은 한 스택을 꽉 채우고,
    // 스택이 안 되는 아이템은 nonStackableCopies 개씩 넣는다.
    // 지푸라기(화폐)는 창고에 들어가지 않고, 특수 아이템(24002~24004)은 창고 제외 규칙이라 건너뛴다.
    private void Seed()
    {
        GridContainerData warehouse = inventoryBench.WarehouseData;
        if (warehouse == null || inventoryBench.itemCsv == null)
        {
            return;
        }

        InventoryService inventoryService = new InventoryService(itemDatabase.Catalog);

        foreach (ItemData item in ItemCsvLoader.Parse(inventoryBench.itemCsv.text))
        {
            if (item.itemType == ItemType.Currency || item.itemType == ItemType.Special)
            {
                continue;
            }

            InventoryMoveResult result = inventoryService.AddItem(warehouse, item.itemId, GetSeedAmount(item));
            if (result.Result != InventoryResult.Success)
            {
                Debug.LogWarning("DebugInventorySeeder: 창고 공간 부족으로 다 넣지 못했습니다. itemId=" + item.itemId, this);
            }
        }
    }

    private int GetSeedAmount(ItemData item)
    {
        bool stacks = item.stackable && item.itemType != ItemType.Weapon;
        return stacks ? Mathf.Max(1, item.maxStack) : Mathf.Max(1, nonStackableCopies);
    }
}
