using UnityEngine;

// 다른 씬으로 넘어가는 출구. 상호작용은 상자와 똑같은 방식이다 - 가까이 가면 프롬프트 아이콘이 뜨고,
// F 를 누르고 있으면 게이지가 차고, 다 차면 GameSession 이 페이드 + 로딩 화면으로 씬을 바꾼다.
//
// 로비의 출격 지점(→ 전투맵)과 전투맵의 탈출 지점(→ 로비) 둘 다 이 스크립트로 처리한다.
// 목적지는 인스펙터의 Target Scene Name 만 바꾸면 된다.
//
// 세팅: 빈 오브젝트에 콜라이더(Is Trigger 체크)를 붙이고 PlayerInteraction 의 Interactable Mask 에 들어가는
// 레이어로 지정한 뒤, 이 스크립트와 프롬프트 프리팹을 연결한다 (WorldContainerInteractable 과 동일).
public class ExitInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("이동할 씬 이름. Build Settings 에 등록돼 있어야 한다")]
    [SerializeField] private string targetSceneName = "BattleScene";
    [Tooltip("상호작용 게이지가 차는 데 걸리는 시간(초). 0 이면 즉시")]
    [SerializeField] private float interactDuration = 2f;
    [Tooltip("감지됐을 때 뜨는 프롬프트 아이콘 프리팹 (ScreenAnchoredUI 가 붙어 있어야 함)")]
    [SerializeField] private GameObject promptPrefab;

    private GameObject promptInstance;
    private ScreenAnchoredUI promptAnchoredUI;

    public float InteractDuration
    {
        get { return interactDuration; }
    }

    public bool CanInteract(GameObject interactor)
    {
        // 이미 전환이 시작됐으면 더 받지 않는다
        return !string.IsNullOrEmpty(targetSceneName) && !GameSession.Instance.IsLoading;
    }

    public void OnInteractComplete(GameObject interactor)
    {
        HidePrompt();
        GameSession.Instance.LoadScene(targetSceneName);
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
        }

        promptInstance.SetActive(false);
    }
}
