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

    [Tooltip("프롬프트에 표시할 이름. 비워두면 프리팹에 적어둔 문구를 그대로 쓴다")]
    [SerializeField] private string displayName = "전리품";

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
            inventoryBench = (PersistentUiRoot.Find<InventoryTestBench>() ?? FindAnyObjectByType<InventoryTestBench>());
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

    // 씬을 나갈 때(이 오브젝트가 파괴될 때) 프롬프트도 같이 지운다.
    // 프롬프트 인스턴스는 씬을 넘어 유지되는 부모(InteractionPromptUI.PromptParent) 밑에 있어서,
    // 정리하지 않으면 앵커만 사라진 채 마지막 상태(근접 아이콘 켜짐)로 다음 씬 화면에 남는다.
    private void OnDestroy()
    {
        if (promptInstance != null)
        {
            Destroy(promptInstance);
            promptInstance = null;
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

                // 이름표 문구. 비워두면 프리팹에 적어둔 문구를 그대로 남긴다
                if (!string.IsNullOrEmpty(displayName))
                {
                    promptAnchoredUI.SetText(displayName);
                }
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
