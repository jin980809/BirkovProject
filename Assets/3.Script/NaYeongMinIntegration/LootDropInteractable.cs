using Birdkov.NaYeongMin.InventoryTest;
using Birdkov.NaYeongMin.Rng;
using UnityEngine;

// 적 사망 전리품 오브제를 팀원 상호작용 시스템으로 여는 어댑터.
// WorldContainerInteractable 과 같은 역할이지만, 그쪽은 InventoryWorldContainer 를 요구해서
// 풀링되는 LootDropObject 에는 붙지 않는다. 팀원 파일은 건드리지 않고 공개 API 만 쓴다.
[RequireComponent(typeof(LootDropObject))]
public class LootDropInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private float interactDuration = 1.5f;
    [SerializeField] private InventoryTestBench inventoryBench;
    [Tooltip("감지됐을 때 뜨는 프롬프트 아이콘 프리팹. WorldContainerInteractable 과 같은 것을 쓴다.")]
    [SerializeField] private GameObject promptPrefab;

    private LootDropObject drop;
    private GameObject promptInstance;
    private ScreenAnchoredUI promptAnchoredUI;

    public float InteractDuration
    {
        get { return interactDuration; }
    }

    private void Awake()
    {
        drop = GetComponent<LootDropObject>();

        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }
    }

    // 풀에 반납된(비활성) 오브제는 대상이 되지 않는다. 비어 있으면 열 것도 없다.
    public bool CanInteract(GameObject interactor)
    {
        return inventoryBench != null && drop != null && !drop.Loot.IsEmpty();
    }

    public void OnInteractComplete(GameObject interactor)
    {
        if (inventoryBench != null && drop != null)
        {
            inventoryBench.OpenLoot(drop);
        }
    }

    public void ShowPrompt()
    {
        if (promptInstance == null && promptPrefab != null)
        {
            promptInstance = Instantiate(promptPrefab, InteractionPromptUI.PromptParent);
            promptAnchoredUI = promptInstance.GetComponent<ScreenAnchoredUI>();

            if (promptAnchoredUI != null)
            {
                promptAnchoredUI.SetAnchor(transform);
            }
        }

        if (promptInstance == null)
        {
            return;
        }

        if (promptAnchoredUI != null)
        {
            promptAnchoredUI.SnapToAnchor();
        }

        promptInstance.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptInstance != null)
        {
            promptInstance.SetActive(false);
        }
    }

    // 풀로 돌아갈 때 프롬프트가 화면에 남지 않도록 한다.
    private void OnDisable()
    {
        HidePrompt();
    }
}
