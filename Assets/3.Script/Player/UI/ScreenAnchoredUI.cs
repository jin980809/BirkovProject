using UnityEngine;
using UnityEngine.UI;

// 이 UI(RectTransform)를 지정된 월드 좌표(anchor.position + worldOffset)의 화면 투영 위치로
// 매 프레임 옮긴다. 카메라 각도/거리와 무관하게 항상 납작한 2D 이미지로 보인다
// (World Space 캔버스는 카메라가 기울어진 각도만큼 찌그러져 보이고 거리에 따라 원근으로 작아짐 - 그게 싫어서 이 방식을 쓴다).
//
// Screen Space - Overlay 캔버스 밑에 있어야 한다.
// anchor 는 인스펙터에서 직접 연결하거나, SetAnchor() 로 코드에서 연결한다
// (예: DebugInteractable.Awake() 가 자기 자신의 transform 을 SetAnchor 로 넘겨준다).
public class ScreenAnchoredUI : MonoBehaviour
{
    [SerializeField] private Transform anchor;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private Camera worldCamera;
    [Tooltip("표시할 문구가 있을 때 쓰는 텍스트 (없어도 됨 - 순수 아이콘 프롬프트는 비워둔다)")]
    [SerializeField] private Text label;

    [Header("근접 아이콘")]
    [Tooltip("플레이어가 일정 거리 안으로 들어오면 켜지는 아이콘. 비워두면 이 기능을 쓰지 않는다")]
    [SerializeField] private GameObject proximityIcon;
    [Tooltip("위 아이콘이 켜지는 거리(월드 단위, 수평 거리 기준)")]
    [SerializeField] private float proximityDistance = 2f;

    [Header("F 프롬프트 패널")]
    [Tooltip("F 상호작용 감지(ShowPrompt/HidePrompt)로 켜고 끄는 대상. 이 오브젝트(루트)를 통째로 껐다 켜면 " +
             "근접 아이콘도 같이 꺼져서 먼 거리에서 안 보이므로, 루트는 항상 켜 두고 이 하위 오브젝트만 껐다 켠다. " +
             "비워두면 SetPanelActive 가 아무 일도 하지 않는다")]
    [SerializeField] private GameObject panel;

    private RectTransform rect;
    private PlayerController player;
    private bool proximityIconActive;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        // 켜기 전 authored 상태와 상관없이, 실제로 가까이 오기 전까지는 꺼둔다
        if (proximityIcon != null)
        {
            proximityIcon.SetActive(false);
            proximityIconActive = false;
        }
    }

    public void SetAnchor(Transform target)
    {
        anchor = target;
    }

    // label 이 연결돼 있을 때만 문구를 바꾼다 (없으면 조용히 무시 - 아이콘만 있는 프롬프트도 그대로 쓸 수 있게)
    public void SetText(string text)
    {
        if (label != null)
        {
            label.text = text;
        }
    }

    // F 프롬프트(이름/배경) 표시를 켜고 끈다. 루트 오브젝트 자체는 건드리지 않는다 - 루트를 끄면
    // Update() 가 멈춰서 근접 아이콘 판정도 같이 멈추기 때문에, 루트는 항상 켜 둔 채로 이 하위
    // 오브젝트만 껐다 켠다 (panel 이 비어 있으면 아무 일도 하지 않는다).
    public void SetPanelActive(bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    // 켜기 직전에 호출한다 - 비활성 상태에서는 Update() 가 안 돌아서 프리팹의 기본 위치(예: 화면 중앙)에
    // 그대로 있는데, 그 상태로 SetActive(true) 하면 한 프레임 동안 엉뚱한 자리에 보였다가 다음 프레임에
    // 튀어서 제자리로 간다. 그걸 막기 위해 켜기 전에 위치를 미리 한 번 맞춰둔다.
    public void SnapToAnchor()
    {
        UpdatePosition();
    }

    private void Update()
    {
        UpdatePosition();
        UpdateProximityIcon();
    }

    private void UpdatePosition()
    {
        // Awake 시점에 MainCamera 태그 카메라가 아직 없었으면(씬 로드 순서 등) 여기서 다시 찾는다
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (anchor == null || worldCamera == null || rect == null)
        {
            return;
        }

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(anchor.position + worldOffset);
        rect.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
    }

    // 플레이어가 anchor 로부터 proximityDistance 안으로 들어오면 proximityIcon 을 켜고, 벗어나면 끈다
    // (상태가 실제로 바뀔 때만 SetActive 를 호출해서 매 프레임 불필요한 호출을 하지 않는다)
    private void UpdateProximityIcon()
    {
        if (proximityIcon == null || anchor == null)
        {
            return;
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                return;
            }
        }

        Vector3 toPlayer = player.transform.position - anchor.position;
        toPlayer.y = 0f; // 높이 차이는 무시하고 수평 거리로만 판정한다
        bool shouldBeActive = toPlayer.sqrMagnitude <= proximityDistance * proximityDistance;

        if (shouldBeActive != proximityIconActive)
        {
            proximityIconActive = shouldBeActive;
            proximityIcon.SetActive(shouldBeActive);
        }
    }
}
