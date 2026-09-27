using System;
using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

// 드롭 시스템 단일 진입점. 상자와 적 담당은 Spawn 또는 Roll 만 호출하면 된다.
namespace Birdkov.NaYeongMin.Rng
{
    // 상자 담당과 적 담당이 쓰는 단일 진입점.
    [RequireComponent(typeof(ItemDatabase))]
    [RequireComponent(typeof(DropTableDatabase))]
    [RequireComponent(typeof(LootDropPool))]
    public sealed class LootRuntime : MonoBehaviour
    {
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private DropTableDatabase dropTableDatabase;
        [SerializeField] private LootDropPool lootDropPool;

        [Header("드롭 내구도")]
        [Tooltip("드롭되는 무기·방어구의 남은 내구도 비율 범위. 0.35 = 35% 남은 상태. 둘을 같게 하면 항상 그 값")]
        [SerializeField, Range(0f, 1f)] private float minDurabilityPercent = 0.35f;
        [SerializeField, Range(0f, 1f)] private float maxDurabilityPercent = 1f;

        [Header("추첨 시드")]
        [Tooltip("켜면 seed 값으로 고정 난수를 쓴다. 재현이 필요한 검증에서만 사용한다.")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int seed = 1234;

        private IRandomSource randomSource;

        public bool IsReady { get; private set; }

        public IItemCatalog Catalog => itemDatabase != null ? itemDatabase.Catalog : null;

        private void Awake()
        {
            Initialize();
        }

        // 여러 번 불러도 안전하다. 실패하면 IsReady 가 false 로 남는다.
        public void Initialize()
        {
            IsReady = false;

            if (itemDatabase == null)
            {
                itemDatabase = GetComponent<ItemDatabase>();
            }

            if (dropTableDatabase == null)
            {
                dropTableDatabase = GetComponent<DropTableDatabase>();
            }

            if (lootDropPool == null)
            {
                lootDropPool = GetComponent<LootDropPool>();
            }

            try
            {
                // ItemDatabase 는 자체 Awake 에서도 로드하지만 실행 순서를 믿지 않는다.
                if (itemDatabase.Catalog == null)
                {
                    itemDatabase.Load();
                }

                dropTableDatabase.Load(itemDatabase.Catalog);
            }
            catch (Exception exception)
            {
                Debug.LogError("드롭 데이터 초기화 실패: " + exception.Message, this);
                return;
            }

            randomSource = useFixedSeed ? new SystemRandomSource(seed) : new SystemRandomSource();
            lootDropPool.Prewarm();
            IsReady = true;
        }

        // 결과만 필요할 때. 상자 UI 를 직접 채우는 쪽에서 쓴다.
        public LootContainerData Roll(DropSourceType sourceType, LootContainerSize sizePreset)
        {
            if (!IsReady)
            {
                Debug.LogError("드롭 데이터가 준비되지 않았다. Initialize 결과를 확인할 것.", this);
                return null;
            }

            // 카탈로그를 같이 넘겨서 헬멧·조끼가 부위당 한 개만 나오게 한다
            LootContainerData rolled = new DropRoller(randomSource, Catalog).Roll(
                dropTableDatabase.Entries, sourceType, sizePreset);

            ApplyRandomDurability(rolled);
            return rolled;
        }

        // 추첨과 노란 오브제 배치를 한 번에. 풀이 비면 null 을 돌려주므로 호출 측에서 확인한다.
        public LootDropObject Spawn(
            Vector3 position,
            Quaternion rotation,
            DropSourceType sourceType,
            LootContainerSize sizePreset = LootContainerSize.Box2x4,
            IList<int> guaranteedItemIds = null)
        {
            LootContainerData loot = Roll(sourceType, sizePreset);
            if (loot == null)
            {
                return null;
            }

            // 보장 아이템이 없어도 거쳐 간다. 아이템별 독립 추첨이라 추첨만으로도 등급이 다른 헬멧이
            // 두 개 당첨될 수 있어서, 부위별 중복을 여기서 함께 정리한다.
            loot = PutFirst(loot, guaranteedItemIds, sizePreset);

            LootDropObject instance = lootDropPool.Rent(position, rotation, loot);
            if (instance == null)
            {
                Debug.LogWarning("드롭 풀이 고갈되어 오브제를 배치하지 못했다.", this);
            }

            return instance;
        }

        // 보장 아이템(적이 입고 있던 방어구 등)을 앞칸에 1개씩 넣고 추첨 결과를 뒤에 잇는다.
        // 칸을 넘는 추첨분은 기존 규칙대로 버린다. 보장 아이템은 버려지지 않는다.
        // 헬멧과 조끼는 부위마다 한 개까지만 넣는다. 먼저 놓인 쪽(= 적이 입고 있던 것)이 남고
        // 같은 부위의 추첨분은 건너뛴다. 총기는 제한하지 않는다.
        private LootContainerData PutFirst(LootContainerData rolled, IList<int> itemIds, LootContainerSize sizePreset)
        {
            LootContainerData result = new LootContainerData(sizePreset);
            int index = 0;
            bool helmetPlaced = false;
            bool armorPlaced = false;

            if (itemIds != null)
            {
                foreach (int itemId in itemIds)
                {
                    if (index >= result.SlotCount) break;
                    if (IsDuplicateArmor(itemId, ref helmetPlaced, ref armorPlaced)) continue;
                    result.loot.slots[index].itemId = itemId;
                    result.loot.slots[index].amount = 1;
                    ApplyRandomDurability(result.loot.slots[index]); // 적이 입고 있던 장비도 닳은 상태로 나온다
                    index++;
                }
            }

            foreach (GridSlotData slot in rolled.loot.slots)
            {
                if (index >= result.SlotCount) break;
                if (slot.IsEmpty()) continue;
                if (IsDuplicateArmor(slot.itemId, ref helmetPlaced, ref armorPlaced)) continue;
                result.loot.slots[index].itemId = slot.itemId;
                result.loot.slots[index].amount = slot.amount;
                // 추첨 단계에서 정해진 내구도·개봉 상태를 그대로 가져간다 (예전에는 여기서 사라졌다)
                result.loot.slots[index].remainingRounds = slot.remainingRounds;
                result.loot.slots[index].durabilityDamage = slot.durabilityDamage;
                index++;
            }

            return result;
        }

        // 컨테이너 안의 무기·방어구에 남은 내구도를 무작위로 넣는다 (내구도가 없는 아이템은 건드리지 않는다).
        private void ApplyRandomDurability(LootContainerData container)
        {
            if (container == null)
            {
                return;
            }

            foreach (GridSlotData slot in container.loot.slots)
            {
                if (!slot.IsEmpty())
                {
                    ApplyRandomDurability(slot);
                }
            }
        }

        // 남은 내구도 = maxDurability × (min~max 사이 무작위 비율). durabilityDamage 는 "닳은 양"이라 그 차이를 넣는다.
        // 최소 1 은 남겨서 줍는 즉시 부서진 상태로 나오지 않게 한다.
        private void ApplyRandomDurability(GridSlotData slot)
        {
            if (Catalog == null || !Catalog.TryGetItem(slot.itemId, out ItemData item) || item.maxDurability <= 0)
            {
                return;
            }

            float minPercent = Mathf.Clamp01(Mathf.Min(minDurabilityPercent, maxDurabilityPercent));
            float maxPercent = Mathf.Clamp01(Mathf.Max(minDurabilityPercent, maxDurabilityPercent));
            float percent = minPercent + (float)randomSource.NextUnit() * (maxPercent - minPercent);

            int remaining = Mathf.Clamp(Mathf.RoundToInt(item.maxDurability * percent), 1, item.maxDurability);
            slot.durabilityDamage = item.maxDurability - remaining;
        }

        // 그 부위가 이미 채워져 있으면 true(건너뛴다). 아직 비어 있으면 채운 것으로 기록하고 false.
        // 방어구가 아닌 아이템은 항상 false 라서 개수 제한을 받지 않는다.
        private bool IsDuplicateArmor(int itemId, ref bool helmetPlaced, ref bool armorPlaced)
        {
            if (Catalog == null || !Catalog.TryGetItem(itemId, out ItemData item) ||
                item.itemType != ItemType.Equipment)
            {
                return false;
            }

            if (item.equipmentSlotType == EquipmentSlotType.Helmet)
            {
                if (helmetPlaced) return true;
                helmetPlaced = true;
                return false;
            }

            if (item.equipmentSlotType == EquipmentSlotType.Armor)
            {
                if (armorPlaced) return true;
                armorPlaced = true;
                return false;
            }

            return false;
        }
    }
}
