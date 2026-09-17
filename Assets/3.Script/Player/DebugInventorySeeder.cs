using System.Collections.Generic;
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
    private const int PistolItemId = 10001;
    private const int ShotgunItemId = 10002;
    private const int AssaultRifleItemId = 10003;
    private const int SniperItemId = 10004;
    private const int PistolAmmoItemId = 11001;
    private const int ShotgunAmmoItemId = 11002;
    private const int AssaultRifleAmmoItemId = 11003;
    private const int SniperAmmoItemId = 11004;
    private const int AmmoBoxCount = 30; // 상자당 20발 (ItemData.csv 참고) - 3상자 = 60발

    private const int VestBasicItemId = 12001; // 구형 조끼
    private const int VestSturdyItemId = 12002; // 튼튼 조끼
    private const int VestPremiumItemId = 12003; // 최고급 조끼
    private const int HelmetBasicItemId = 13001; // 구형 헬멧
    private const int HelmetSturdyItemId = 13002; // 튼튼 헬멧
    private const int HelmetPremiumItemId = 13003; // 최고급 헬멧

    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private InventoryTestBench inventoryBench;

    private PlayerInventoryService service;
    private bool didSeed;

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
        // InventoryTestBench 가 자기 데이터를 준비하는 타이밍이 늦을 수 있어서,
        // 준비될 때까지 매 프레임 확인하다가 딱 한 번만 넣어준다.
        if (didSeed || inventoryBench == null || !inventoryBench.IsReady ||
            itemDatabase == null || itemDatabase.Catalog == null)
        {
            return;
        }

        didSeed = true;
        service = new PlayerInventoryService(itemDatabase.Catalog);
        Seed();
    }

    private void Seed()
    {
        PlayerInventoryData data = inventoryBench.PlayerData;
        if (data == null)
        {
            return;
        }

        EquipToSlot(data, AssaultRifleItemId, EquipmentSlots.PrimaryWeapon);
        EquipToSlot(data, SniperItemId, EquipmentSlots.SecondaryWeapon);
        EquipToSlot(data, HelmetBasicItemId, EquipmentSlots.Helmet);
        EquipToSlot(data, VestBasicItemId, EquipmentSlots.Armor);

        // 무기 슬롯 2개는 이미 위에서 채웠으니, 권총/샷건은 장착하지 않고 가방에만 넣어준다
        service.AddToInventory(data, PistolItemId, 1);
        service.AddToInventory(data, ShotgunItemId, 1);

        service.AddToInventory(data, PistolAmmoItemId, AmmoBoxCount);
        service.AddToInventory(data, ShotgunAmmoItemId, AmmoBoxCount);
        service.AddToInventory(data, AssaultRifleAmmoItemId, AmmoBoxCount);
        service.AddToInventory(data, SniperAmmoItemId, AmmoBoxCount);

        // 헬멧/조끼도 장비 슬롯 하나는 이미 위에서 채웠으니(등급별로 하나씩만 낄 수 있음),
        // 나머지 등급은 가방에 넣어서 드래그로 바꿔 낄 수 있게 한다
        service.AddToInventory(data, HelmetSturdyItemId, 1);
        service.AddToInventory(data, HelmetPremiumItemId, 1);
        service.AddToInventory(data, VestSturdyItemId, 1);
        service.AddToInventory(data, VestPremiumItemId, 1);
    }

    private void EquipToSlot(PlayerInventoryData data, int itemId, int equipmentSlotIndex)
    {
        service.AddToInventory(data, itemId, 1);

        // 방금 가방에 넣은 자리를 찾아서 장비 슬롯으로 옮긴다
        List<GridSlotData> slots = data.inventory.slots;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].itemId == itemId)
            {
                service.EquipFromInventory(data, i, equipmentSlotIndex);
                return;
            }
        }
    }
}
