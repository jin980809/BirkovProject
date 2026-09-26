using UnityEngine;

// 테스트용: 아무 오브젝트에 붙이면 IInteractable 을 구현해서 상호작용 판정/게이지/ChestUI 까지
// 전체 흐름이 되는지 확인할 수 있다. interactDuration 을 0으로 두면 게이지 없이 즉시 완료되는
// 경로를 테스트할 수 있다.
// 실제 게임 로직 아님 - 검증 끝나면 지운다.
public class DebugInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private float interactDuration = 2f;
    [Tooltip("체크하면 한 번 완료된 뒤에는 다시 상호작용이 안 된다")]
    [SerializeField] private bool oneTimeUse;
    [Tooltip("완료되면 이 ChestUI 를 연다 (비워두면 로그만 찍는다)")]
    [SerializeField] private ChestUI chestUI;
    [Tooltip("감지됐을 때 뜨는 프롬프트 아이콘 프리팹 (ScreenAnchoredUI 가 붙어 있어야 함). 씬에 미리 배치할 필요 없음 - 처음 감지될 때 하나 생성해서 재사용한다")]
    [SerializeField] private GameObject promptPrefab;

    private bool used;
    private GameObject promptInstance; // 처음 ShowPrompt() 할 때 생성, 이후엔 켰다 껐다만 한다
    private ScreenAnchoredUI promptAnchoredUI;

    public float InteractDuration
    {
        get { return interactDuration; }
    }

    public bool CanInteract(GameObject interactor)
    {
        return !oneTimeUse || !used;
    }

    public void OnInteractComplete(GameObject interactor)
    {
        used = true;
        Debug.Log(gameObject.name + " 상호작용 완료 (" + interactor.name + ")", this);

        if (chestUI != null)
        {
            chestUI.Open();
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
        EnsurePromptInstance();

        if (promptInstance != null)
        {
            // 켜기 전에 위치부터 맞춰야 한 프레임 동안 엉뚱한 자리(프리팹 기본 위치)에 보였다가
            // 튀는 현상이 없다.
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

    // 아직 생성한 적 없으면 프리팹으로 하나 만들어서 이 오브젝트를 추적하게 연결한다.
    // 이미 만들어져 있으면 아무것도 안 한다 (재사용).
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
