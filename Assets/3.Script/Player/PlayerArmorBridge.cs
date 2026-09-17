using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

// 장착된 헬멧/조끼(Equipment)의 defense 값을 읽어서 최종 피해 배율을 계산한다.
// defense 값 자체를 퍼센트로 취급한다 (예: 20 = 20% 감소). 헬멧/조끼를 곱연산으로 겹친다
// (10%+20% 방어구를 같이 착용하면 1 - 0.9*0.8 = 28% 감소) - 여러 부위를 겹쳐도 100%
// 무효화에는 절대 도달하지 않는다 (상한선 없이 곱연산만으로 충분히 안전함).
// NaYeongMin 파일은 건드리지 않고 공개 API(PlayerInventoryService)만 쓴다.
public class PlayerArmorBridge : MonoBehaviour
{
    [SerializeField] private ItemDatabase itemDatabase;

    private PlayerInventoryService playerInventoryService;
    private PlayerInventoryData playerData;

    private void Awake()
    {
        if (itemDatabase == null)
        {
            itemDatabase = FindAnyObjectByType<ItemDatabase>();
        }
    }

    // ItemDatabase 는 자기 Awake() 에서 CSV 를 읽어 Catalog 를 채우는데, 이 컴포넌트의 Awake() 가
    // 그보다 먼저 실행되면 Catalog 가 아직 null 이라 PlayerInventoryService 생성자가 예외를 던진다.
    // 그래서 여기서 즉시 만들지 않고, 실제로 필요할 때(GetDamageMultiplier) 딱 한 번만 만든다.
    private void EnsureService()
    {
        if (playerInventoryService != null || itemDatabase == null || itemDatabase.Catalog == null)
        {
            return;
        }

        playerInventoryService = new PlayerInventoryService(itemDatabase.Catalog);
    }

    // InventoryTestBenchLink 가 진짜 인벤토리 데이터를 주입한다 (WeaponInventoryBridge 와 동일한 패턴)
    public void SetPlayerInventoryData(PlayerInventoryData data)
    {
        playerData = data;
    }

    // 지금 장착된 헬멧/조끼의 itemId (없으면 -1). ArmorVisual 이 어떤 모델을 보여줄지 결정할 때 쓴다.
    public int EquippedHelmetItemId
    {
        get { return GetSlotItemId(EquipmentSlots.Helmet); }
    }

    public int EquippedVestItemId
    {
        get { return GetSlotItemId(EquipmentSlots.Armor); }
    }

    private int GetSlotItemId(int equipmentSlotIndex)
    {
        EnsureService();

        if (playerInventoryService != null && playerData != null &&
            playerInventoryService.TryGetEquipped(playerData, equipmentSlotIndex, out ItemData item))
        {
            return item.itemId;
        }

        return -1;
    }

    // 원래 데미지에 곱하면 방어구가 감쇄한 최종 데미지가 나오는 배율 (0~1). 장비/데이터가 아직 없으면 1(감쇄 없음).
    public float GetDamageMultiplier()
    {
        EnsureService();

        if (playerInventoryService == null || playerData == null)
        {
            return 1f;
        }

        return GetSlotMultiplier(EquipmentSlots.Helmet) * GetSlotMultiplier(EquipmentSlots.Armor);
    }

    private float GetSlotMultiplier(int equipmentSlotIndex)
    {
        return 1f - Mathf.Clamp01(GetSlotDefense(equipmentSlotIndex) / 100f);
    }

    // 장착된 게 없으면 0
    private float GetSlotDefense(int equipmentSlotIndex)
    {
        EnsureService();

        if (playerInventoryService != null && playerData != null &&
            playerInventoryService.TryGetEquipped(playerData, equipmentSlotIndex, out ItemData item))
        {
            return item.defense;
        }

        return 0f;
    }
}
