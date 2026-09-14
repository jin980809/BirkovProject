using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 커서 자리에 뜨는 커스텀 크로스헤어.
//  - OS 마우스 커서를 숨기고 이 UI 가 대신 마우스 위치를 따라간다
//  - 상하좌우 4개 이미지가 WeaponController 의 현재 퍼짐(블룸)에 따라 벌어졌다 좁혀졌다 한다
//  - 중앙 원 이미지는 이 오브젝트의 정중앙 자식으로 두면 된다 (스크립트가 따로 움직이지 않음)
//  - 인벤토리/상자/상점 등 다른 UI 를 열 때는 SetCrosshairActive(false) 로 꺼서
//    크로스헤어를 숨기고 OS 커서를 다시 보이게 한다 (호출부는 그 UI 쪽이 나중에 붙는다)
//
// 이 오브젝트는 Screen Space - Overlay 캔버스 밑에 있어야 한다
// (root.position 을 스크린 좌표로 바로 대입하기 때문)
public class CrosshairUI : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private WeaponController weapon;
    [SerializeField] private RectTransform upArm;
    [SerializeField] private RectTransform downArm;
    [SerializeField] private RectTransform leftArm;
    [SerializeField] private RectTransform rightArm;

    [Header("크기")]
    [Tooltip("퍼짐 0 일 때 중앙에서 화살표까지 거리 (px)")]
    [SerializeField] private float baseRadius = 10f;
    [Tooltip("퍼짐 최대일 때 중앙에서 화살표까지 거리 (px)")]
    [SerializeField] private float maxRadius = 60f;
    [Tooltip("크기 변화 부드러움 (클수록 빠르게 반응)")]
    [SerializeField] private float sizeSharpness = 15f;

    private RectTransform root;
    private float currentRadius;

    private void Awake()
    {
        root = GetComponent<RectTransform>();

        if (weapon == null)
        {
            weapon = FindAnyObjectByType<WeaponController>();
        }

        currentRadius = baseRadius;
        Cursor.visible = false;
    }

    private void Update()
    {
        FollowMouse();
        UpdateSpreadVisual();
    }

    // 인벤토리/상자/상점 등을 열고 닫을 때 호출한다 (지금은 기능만 준비, 실제 호출부는 나중에 연결)
    public void SetCrosshairActive(bool active)
    {
        Cursor.visible = !active;
        gameObject.SetActive(active);
    }

    private void FollowMouse()
    {
        Vector2 mouse = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width, Screen.height) * 0.5f;

        root.position = new Vector3(mouse.x, mouse.y, 0f);
    }

    private void UpdateSpreadVisual()
    {
        // 무기가 없으면(맨손) 퍼짐 0 으로 취급한다. 맨손이어도 크로스헤어 자체는 계속 보인다 (나중에 바뀔 수 있음)
        float normalized = weapon != null ? weapon.CurrentSpreadNormalized : 0f;
        float targetRadius = Mathf.Lerp(baseRadius, maxRadius, normalized);

        currentRadius = Mathf.Lerp(currentRadius, targetRadius, 1f - Mathf.Exp(-sizeSharpness * Time.deltaTime));

        SetArm(upArm, Vector2.up);
        SetArm(downArm, Vector2.down);
        SetArm(leftArm, Vector2.left);
        SetArm(rightArm, Vector2.right);
    }

    private void SetArm(RectTransform arm, Vector2 direction)
    {
        if (arm != null)
        {
            arm.anchoredPosition = direction * currentRadius;
        }
    }
}
