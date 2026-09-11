using System;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

namespace Birdkov.NaYeongMin.Rng
{
    // 상자 담당과 적 담당이 쓰는 단일 진입점.
    // ItemDatabase -> DropTableDatabase -> LootDropPool 초기화 순서를 여기서 보장한다.
    // 호출 측은 Spawn 하나만 알면 되고 CSV 로드나 풀 관리는 신경 쓰지 않는다.
    //
    // 연결 방법:
    //   1. Assets/2.Model/Prefabs/NaYeongMin/LootRuntime.prefab 을 씬에 배치한다.
    //   2. 적 사망이 확정되면 Spawn(사망 위치, 회전, 적 유형, LootContainerSize.Box2x4) 를 호출한다.
    //   3. 상자를 열면 Spawn 대신 Roll 로 결과만 받아 상자 UI에 채워도 된다.
    //   4. 반환값이 null 이면 풀이 고갈된 것이다. 호출 측에서 반드시 확인한다.
    [RequireComponent(typeof(ItemDatabase))]
    [RequireComponent(typeof(DropTableDatabase))]
    [RequireComponent(typeof(LootDropPool))]
    public sealed class LootRuntime : MonoBehaviour
    {
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private DropTableDatabase dropTableDatabase;
        [SerializeField] private LootDropPool lootDropPool;

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

            return new DropRoller(randomSource).Roll(dropTableDatabase.Entries, sourceType, sizePreset);
        }

        // 추첨과 노란 오브제 배치를 한 번에. 풀이 비면 null 을 돌려주므로 호출 측에서 확인한다.
        public LootDropObject Spawn(
            Vector3 position,
            Quaternion rotation,
            DropSourceType sourceType,
            LootContainerSize sizePreset = LootContainerSize.Box2x4)
        {
            LootContainerData loot = Roll(sourceType, sizePreset);
            if (loot == null)
            {
                return null;
            }

            LootDropObject instance = lootDropPool.Rent(position, rotation, loot);
            if (instance == null)
            {
                Debug.LogWarning("드롭 풀이 고갈되어 오브제를 배치하지 못했다.", this);
            }

            return instance;
        }
    }
}
