using UnityEngine;

// 상자 / 적의 전리품 등 IInteractable 대상과의 상호작용.
// 판정: 일정 거리 안 + 플레이어가 바라보는 방향 기준 시야각(콘) 안에 있는 대상 중 가장 가까운 것.
// 상호작용을 시작하면 IInteractDuration 만큼 시간이 걸리고, 그동안은 취소 조건 없이 끝까지 진행된다
// (PlayerController 가 이 컴포넌트의 IsInteracting 을 보고 이동/회전/사격을 막는다).
//
// 프롬프트 UI(예: "F" 아이콘)는 이 스크립트가 직접 띄우지 않는다 - 각 IInteractable 오브젝트가
// 자기 자신의 자식으로 미리 UI를 배치해 두고, ShowPrompt()/HidePrompt() 로 켜고 끈다.
[RequireComponent(typeof(PlayerController))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("판정")]
    [SerializeField] private float interactRange = 3f;
    [Tooltip("바라보는 방향 기준 이 각도(전체 폭) 안에 있어야 상호작용 가능")]
    [SerializeField] private float interactAngle = 90f;
    [SerializeField] private LayerMask interactableMask;

    private IInteractable currentTarget;
    private IInteractable promptShownFor; // 지금 ShowPrompt() 가 걸려 있는 대상 (HidePrompt() 짝을 맞추기 위해 추적)
    private float interactTimer;

    private readonly Collider[] buffer = new Collider[16];

    public bool HasTarget
    {
        get { return currentTarget != null; }
    }

    public bool IsInteracting { get; private set; }

    // UI 진행바가 참조하는 0~1 진행률
    public float InteractProgress01
    {
        get
        {
            if (currentTarget == null || currentTarget.InteractDuration <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(interactTimer / currentTarget.InteractDuration);
        }
    }

    private void OnDisable()
    {
        HidePromptIfShown();
    }

    private void Update()
    {
        if (IsInteracting)
        {
            UpdateInteractTimer();
        }
        else
        {
            FindTarget();
        }
    }

    // PlayerController.HandleInteract 에서 호출한다
    public void TryStartInteract()
    {
        if (IsInteracting || currentTarget == null)
        {
            return;
        }

        IInteractable target = currentTarget;
        HidePromptIfShown(); // 상호작용을 누르는 순간 프롬프트 아이콘은 꺼진다 (게이지가 대신 뜬다)

        // 상호작용 시간이 0이면 게이지 없이 바로 완료
        if (target.InteractDuration <= 0f)
        {
            currentTarget = null;
            target.OnInteractComplete(gameObject);
            return;
        }

        IsInteracting = true;
        interactTimer = 0f;
    }

    private void HidePromptIfShown()
    {
        if (promptShownFor != null)
        {
            promptShownFor.HidePrompt();
            promptShownFor = null;
        }
    }

    // PlayerController.HandleCancel 에서 호출한다 (ESC) - 진행 중이던 상호작용을 중단하고 원상태로 되돌린다.
    // OnInteractComplete 는 호출하지 않는다 (완료가 아니라 취소이므로).
    public void CancelInteract()
    {
        if (!IsInteracting)
        {
            return;
        }

        IsInteracting = false;
        interactTimer = 0f;
        currentTarget = null;
    }

    private void FindTarget()
    {
        currentTarget = null;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position, interactRange, buffer, interactableMask, QueryTriggerInteraction.Collide);

        float cosHalfAngle = Mathf.Cos(interactAngle * 0.5f * Mathf.Deg2Rad);
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            IInteractable candidate = buffer[i].GetComponentInParent<IInteractable>();
            if (candidate == null || !candidate.CanInteract(gameObject))
            {
                continue;
            }

            Vector3 toTarget = buffer[i].transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance < 0.01f || distance >= bestDistance)
            {
                continue;
            }

            Vector3 direction = toTarget / distance;
            if (Vector3.Dot(forward, direction) < cosHalfAngle)
            {
                continue;
            }

            bestDistance = distance;
            currentTarget = candidate;
        }

        if (currentTarget != promptShownFor)
        {
            HidePromptIfShown();

            if (currentTarget != null)
            {
                currentTarget.ShowPrompt();
                promptShownFor = currentTarget;
            }
        }
    }

    private void UpdateInteractTimer()
    {
        // Unity 오브젝트가 도중에 파괴된 경우(예: 상자가 다른 이유로 사라짐) 안전하게 중단
        if (currentTarget == null || (currentTarget as Object) == null)
        {
            IsInteracting = false;
            currentTarget = null;
            return;
        }

        interactTimer += Time.deltaTime;
        if (interactTimer < currentTarget.InteractDuration)
        {
            return;
        }

        IInteractable target = currentTarget;
        IsInteracting = false;
        interactTimer = 0f;
        currentTarget = null;

        target.OnInteractComplete(gameObject);
    }
}
