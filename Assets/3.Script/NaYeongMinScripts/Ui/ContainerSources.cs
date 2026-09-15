using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using UnityEngine;

// 월드에 놓이는 외부 컨테이너 3종. 내용물은 대상별로 독립 보관하고 최초 1회만 추첨한다.
namespace Birdkov.NaYeongMin.Ui
{
    public enum ExternalContainerKind
    {
        Storage,
        Loot,
        MapChest
    }

    public interface IContainerSource
    {
        ExternalContainerKind Kind { get; }
        string Title { get; }
        GridContainerData Container { get; }
        Transform Anchor { get; }
        bool IsAvailable { get; }
        void EnsureContents(IItemCatalog itemCatalog);
        void NotifyContentsChanged();
    }

    // 허브 보관상자. 창고 데이터를 외부에서 주입받고 스스로 추첨하지 않는다.
    public sealed class StorageContainer : MonoBehaviour, IContainerSource
    {
        [SerializeField] private string title = "보관상자";

        private GridContainerData warehouse;

        public ExternalContainerKind Kind => ExternalContainerKind.Storage;
        public string Title => title;
        public GridContainerData Container => warehouse;
        public Transform Anchor => transform;
        public bool IsAvailable => isActiveAndEnabled && warehouse != null;

        // 세이브가 들고 있는 창고 데이터를 그대로 연결한다. 복제하지 않는다.
        public void BindWarehouse(GridContainerData data)
        {
            warehouse = data;
        }

        public void EnsureContents(IItemCatalog itemCatalog)
        {
        }

        public void NotifyContentsChanged()
        {
        }
    }

    // 적 사망 전리품. 풀에서 빌려온 LootDropObject 를 그대로 비춘다.
    [RequireComponent(typeof(LootDropObject))]
    public sealed class LootContainer : MonoBehaviour, IContainerSource
    {
        [SerializeField] private string title = "전리품";
        [SerializeField] private LootDropObject dropObject;

        public ExternalContainerKind Kind => ExternalContainerKind.Loot;
        public string Title => title;
        public GridContainerData Container => Drop != null ? Drop.Loot.loot : null;
        public Transform Anchor => transform;
        public bool IsAvailable => isActiveAndEnabled && Drop != null && Drop.gameObject.activeInHierarchy;

        private LootDropObject Drop => dropObject != null ? dropObject : dropObject = GetComponent<LootDropObject>();

        // 내용물은 적 사망 시점에 이미 채워져 있다. UI 개방으로 다시 굴리지 않는다.
        public void EnsureContents(IItemCatalog itemCatalog)
        {
        }

        // 비면 풀로 반환된다. LootDropObject.Emptied 를 LootDropPool 이 구독한다.
        public void NotifyContentsChanged()
        {
            if (Drop != null)
            {
                Drop.NotifyContentsChanged();
            }
        }
    }

    // 맵에 배치하는 상자. 최초 개방 전에 한 번만 추첨하고 남은 내용물은 이 오브젝트가 보관한다.
    public sealed class MapChestContainer : MonoBehaviour, IContainerSource
    {
        [SerializeField] private string title = "상자";
        [SerializeField] private ContainerDropSettings dropSettings;
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int seed = 1234;

        private LootContainerData contents;
        private bool rolled;

        public ExternalContainerKind Kind => ExternalContainerKind.MapChest;
        public string Title => title;
        public GridContainerData Container => contents != null ? contents.loot : null;
        public Transform Anchor => transform;
        public bool IsAvailable => isActiveAndEnabled;
        public bool Rolled => rolled;
        public ContainerDropSettings DropSettings => dropSettings;

        // 최초 1회만 추첨한다. 재개방해도 다시 굴리지 않고 획득분도 복구하지 않는다.
        public void EnsureContents(IItemCatalog itemCatalog)
        {
            if (rolled)
            {
                return;
            }

            rolled = true;
            IRandomSource random = useFixedSeed ? new SystemRandomSource(seed) : new SystemRandomSource();
            contents = ContainerLootRoller.Roll(dropSettings, itemCatalog, random);
        }

        public void NotifyContentsChanged()
        {
        }
    }
}
