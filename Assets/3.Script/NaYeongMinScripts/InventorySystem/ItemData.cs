using System;

namespace Birdkov.NaYeongMin.InventorySystem
{
    public enum ItemType
    {
        Equipment,
        Weapon,
        Consumable,
        Material,
        Other,
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

        public float attackDamage;
        public float reloadSpeed;
        public float fireRate;
        public int magazineSize;
        public float projectileSpeed;
        public bool automatic;
        public int pelletCount = 1;
        public float maxSpread;
        public float range;
        public float attacksPerSecond;
        public int maxDurability;
        public int durabilityCostPerHit;
        public int repairAmountPerCurrency;

        public float defense;
        public float healthRecovery;
        public float hungerRecovery;
        public float waterRecovery;
    }
}
