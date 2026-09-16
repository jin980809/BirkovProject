using System;

// 아이템 한 건의 스탯 정의와 분류 enum.
namespace Birdkov.NaYeongMin.InventorySystem
{
    public enum ItemType
    {
        Equipment,
        Weapon,
        Consumable,
        Material,
        Ammo,
        Currency,
        Sale,
        Special
    }

    public enum EquipmentSlotType
    {
        None,
        Helmet,
        Armor,
        PrimaryGun,
        SecondaryGun
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public class ItemData
    {
        public int itemId;
        public string displayName;
        public string description;
        public string iconKey;
        public ItemType itemType;
        public EquipmentSlotType equipmentSlotType;
        public ItemRarity rarity;
        public int price;
        public float weight;
        public bool stackable;
        public int maxStack = 1;
        public int gridWidth = 1;
        public int gridHeight = 1;

        // 아래는 기획 확정 대기(B-1) 항목이다. 확정되면 CSV 열만 채우면 된다.
        // 지금은 읽는 코드가 없고 총기와 탄약도 등록되어 있지 않다.
        public float attackDamage;
        // 사거리. 1 Unit = 1m, Vector3.Distance 기준. 기획서 7.3.
        public float range;
        public float fireRate;              // 발/초. 연사 간격이 필요하면 1f / fireRate 로 계산한다.
        public float reloadSpeed;
        public int magazineSize;
        public float projectileSpeed;
        public bool automatic;
        public int pelletCount = 1;         // 샷건 산탄 수
        public float maxSpread;
        public int maxDurability;
        public int durabilityCostPerHit;
        public int repairAmountPerCurrency;

        // 방어력. 정수 차감식. 최종 피격 데미지 = MAX(1, 공격의 고정 공격력 - 총 방어력).
        // 총 방어력 = 장착 중인 머리 + 몸통 합산. 피격 위치와 무관하다. 기획서 6.7.
        // 내구도는 피격 1회당 장착 중인 모든 부위가 동시에 -1 된다.
        public float defense;
        public float healthRecovery;
        public float hungerRecovery;
        public float waterRecovery;

        // 사용(소모)에 걸리는 시간(초). 0 이하면 즉시 사용.
        public float usageTime;
    }
}
