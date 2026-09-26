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
    [Tooltip("프롬프트에 표시할 이름 (예: 출격, 탈출). 비워두면 프리팹에 적어둔 문구를 그대로 쓴다")]
    [SerializeField] private string displayName = "출격";

    private GameObject promptInstance;
    private ScreenAnchoredUI promptAnchoredUI;

    public float InteractDuration
    {
        get { return interactDuration; }
    }

    // Awake 가 아니라 Start 에서 만든다 - InteractionPromptUI.PromptParent 는 그쪽 Awake() 에서
    // 설정되는데, 스크립트 간 Awake 실행 순서는 보장되지 않는다 (이쪽이 먼저 돌면 PromptParent 가
    // 아직 null). 모든 Awake 가 끝난 뒤에 도는 Start 라면 항상 준비되어 있다.
    private void Start()
    {
        // 근접 아이콘(ScreenAnchoredUI.proximityIcon)은 F 프롬프트 감지 범위보다 훨씬 먼 거리에서도
        // 보여야 하므로, 감지될 때(ShowPrompt)까지 기다리지 않고 미리 만들어 둔다.
        EnsurePromptInstance();
    }

    public bool CanInteract(GameObject interactor)
    {
        // 이미 전환이 시작됐으면 더 받지 않는다
        return !string.IsNullOrEmpty(targetSceneName) && !GameSession.Instance.IsLoading;
    }

    public void OnInteractComplete(GameObject interactor)
    {
        // promptInstance 는 이 오브젝트가 아니라 씬을 넘어 유지되는 프롬프트 부모(PromptParent) 밑에 있어서,
        // 씬 전환으로 이 오브젝트(anchor)가 사라져도 같이 없어지지 않고 마지막 상태(근접 아이콘 켜짐)로
        // 화면에 남는다. HidePrompt() 는 F 패널만 끄므로, 아예 통째로 지워서 근접 아이콘까지 정리한다.
        if (promptInstance != null)
        {
            Destroy(promptInstance);
        }

        GameSession.Instance.LoadScene(targetSceneName);
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
        EnsurePromptInstance();

        if (promptAnchoredUI != null)
        {
            promptAnchoredUI.SetPanelActive(true);
        }
    }

    public void HidePrompt()
    {
        if (promptAnchoredUI != null)
        {
            promptAnchoredUI.SetPanelActive(false);
        }
    }

    // 루트(promptInstance)는 항상 켜 둔다 - 근접 아이콘이 계속 갱신되려면 Update() 가 멈추면 안 된다.
    // F 프롬프트만 ShowPrompt/HidePrompt 로 따로 켜고 끈다 (SetPanelActive 참고).
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

            // 이름표 문구. 비워두면 프리팹에 적어둔 문구를 그대로 남긴다
            if (!string.IsNullOrEmpty(displayName))
            {
                promptAnchoredUI.SetText(displayName);
            }

            promptAnchoredUI.SetPanelActive(false);
        }
    }
}
