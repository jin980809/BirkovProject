using UnityEngine;

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

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    public void SetAnchor(Transform target)
    {
        anchor = target;
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
    }

    private void UpdatePosition()
    {
        if (anchor == null || worldCamera == null || rect == null)
        {
            return;
        }

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(anchor.position + worldOffset);
        rect.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
    }
}
