using System;

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

        public float defense;
        public float healthRecovery;
        public float hungerRecovery;
        public float waterRecovery;
    }
}
