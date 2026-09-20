using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

// NaYeongMin 의 InventoryWorldContainer(상자/전리품/맵배치상자)를 내 상호작용 시스템으로 여는 어댑터.
// NaYeongMin 파일은 건드리지 않고 공개 API(InventoryTestBench.OpenStorage/OpenLoot/OpenMapChest)만 쓴다.
//
// 주의: NaYeongMin 의 PlayerInventoryBridge 는 씬에 넣지 않는다. 그게 있으면 F키를 자기가 직접
// 가로채서(범위만 보고, 게이지 없이) 즉시 열어버리기 때문에, 이 어댑터의 게이지/잠금과 동시에
// 작동할 수 없다 - 둘 중 하나만 써야 한다. (E키 인벤토리 토글/무기선택/퀵슬롯도 그 브릿지 안에
// 같이 있어서 지금은 같이 못 쓴다 - 나중에 F키 라우팅을 정리할 때 다시 합친다.)
//
// 흐름: 감지되면 내 프롬프트 아이콘이 뜨고, F를 누르면 내 게이지(이동/회전 잠금, 시야는 계속 회전)가
// 돈다. 게이지가 끝나면 NaYeongMin 의 실제 UI를 연다 (OpenStorage/OpenLoot/OpenMapChest 는 내부적으로
// OpenInventory() 도 같이 호출해서 가방 화면까지 함께 뜬다 - 이미 NaYeongMin 쪽에 그렇게 되어 있다).
// 잠금을 푸는 건 여기서 하지 않는다 - PlayerInventoryToggle.cs 가 NaYeongMin UI 전체(단독 인벤토리든
// 상자로 연 것이든)의 열림/닫힘을 한 곳에서 감지해서 처리한다 (중복 방지).
[RequireComponent(typeof(InventoryWorldContainer))]
public class WorldContainerInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private float interactDuration = 1.5f;
    [SerializeField] private InventoryTestBench inventoryBench;
    [Tooltip("감지됐을 때 뜨는 프롬프트 아이콘 프리팹 (ScreenAnchoredUI 가 붙어 있어야 함). DebugInteractable 과 같은 방식")]
    [SerializeField] private GameObject promptPrefab;

    private InventoryWorldContainer container;
    private GameObject promptInstance;
    private ScreenAnchoredUI promptAnchoredUI;

    public float InteractDuration
    {
        get { return interactDuration; }
    }

    private void Awake()
    {
        container = GetComponent<InventoryWorldContainer>();

        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        EnsureBench();
        return inventoryBench != null && container != null;
    }

    // 씬 전환 직후에는 이 씬에 있다가 곧 파괴되는 중복 벤치를 잡았을 수 있다 (PersistentUiRoot 참고).
    // 참조가 죽었으면 살아남은 벤치로 다시 찾는다.
    private void EnsureBench()
    {
        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }
    }

    public void OnInteractComplete(GameObject interactor)
    {
        EnsureBench();

        if (inventoryBench == null || container == null)
        {
            return;
        }

        switch (container.kind)
        {
            case InventoryWorldKind.Storage:
                inventoryBench.OpenStorage(container);
                break;
            case InventoryWorldKind.Loot:
                inventoryBench.OpenLoot(container);
                break;
            case InventoryWorldKind.MapChest:
                inventoryBench.OpenMapChest(container);
                break;
            case InventoryWorldKind.Shop:
                inventoryBench.OpenShop(transform);
                break;
            case InventoryWorldKind.Crafting:
                inventoryBench.OpenCrafting(transform);
                break;
        }
    }

    public void ShowPrompt()
    {
        EnsurePromptInstance();

        if (promptInstance != null)
        {
            if (promptAnchoredUI != null)
            {
                promptAnchoredUI.SnapToAnchor();
            }

            promptInstance.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (promptInstance != null)
        {
            promptInstance.SetActive(false);
        }
    }

    private void EnsurePromptInstance()
    {
        if (promptInstance != null || promptPrefab == null)
        {
            return;
        }

        Transform parent = InteractionPromptUI.PromptParent;
        promptInstance = Instantiate(promptPrefab, parent);
        promptAnchoredUI = promptInstance.GetComponent<ScreenAnchoredUI>();

        if (promptAnchoredUI != null)
        {
            promptAnchoredUI.SetAnchor(transform);
            promptAnchoredUI.SetText(container.displayName);
        }

        promptInstance.SetActive(false);
    }
}
