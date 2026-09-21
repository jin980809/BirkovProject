using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 커서 자리에 뜨는 커스텀 크로스헤어.
//  - OS 마우스 커서를 숨기고 이 UI 가 대신 마우스 위치를 따라간다
//  - 상하좌우 4개 이미지가 WeaponController 의 현재 퍼짐(블룸)에 따라 벌어졌다 좁혀졌다 한다
//  - 중앙 원 이미지는 이 오브젝트의 정중앙 자식으로 두면 된다 (스크립트가 따로 움직이지 않음, 위치는 고정)
//  - 인벤토리/상자/상점 등 다른 UI 를 열 때는 SetCrosshairActive(false) 로 꺼서
//    크로스헤어를 숨기고 OS 커서를 다시 보이게 한다 (호출부는 그 UI 쪽이 나중에 붙는다)
//  - 줌(조준) 중에는 SetZoomVisual(true) 로 일반 크로스헤어를 끄고 줌 전용 UI 오브젝트를 켠다
//    (PlayerZoom.cs 가 호출). zoomUI 를 이 오브젝트(root)의 자식으로 두면 마우스 위치를 같이
//    따라가고, 캔버스 밑 다른 자리에 독립적으로 두면 고정된 자리에 뜬다.
//
// 이 오브젝트는 Screen Space - Overlay 캔버스 밑에 있어야 한다
// (root.position 을 스크린 좌표로 바로 대입하기 때문)
public class CrosshairUI : MonoBehaviour, ISceneRebindable
{
    [Header("연결")]
    [SerializeField] private WeaponController weapon;
    [SerializeField] private RectTransform upArm;
    [SerializeField] private RectTransform downArm;
    [SerializeField] private RectTransform leftArm;
    [SerializeField] private RectTransform rightArm;

    [Header("줌 UI 교체")]
    [Tooltip("평소(줌 아닐 때) 크로스헤어 화살표+중앙점을 담은 그룹")]
    [SerializeField] private GameObject normalCrosshairRoot;
    [Tooltip("줌 중에 대신 보여줄 UI 오브젝트")]
    [SerializeField] private GameObject zoomUI;
    [Tooltip("줌 UI 안에도 퍼짐에 따라 벌어지는 화살표가 있다면 연결 (없으면 비워둬도 됨)")]
    [SerializeField] private RectTransform zoomUpArm;
    [SerializeField] private RectTransform zoomDownArm;
    [SerializeField] private RectTransform zoomLeftArm;
    [SerializeField] private RectTransform zoomRightArm;

    [Header("크기")]
    [Tooltip("퍼짐 0 일 때 중앙에서 화살표까지 거리 (px)")]
    [SerializeField] private float baseRadius = 10f;
    [Tooltip("무기의 maxSpread(도) 1당 늘어나는 반경 (px). 무기마다 maxSpread 가 다르므로 최대 반경이 자동으로 무기에 비례한다")]
    [SerializeField] private float radiusPerSpreadDegree = 15f;
    [Tooltip("크기 변화 부드러움 (클수록 빠르게 반응)")]
    [SerializeField] private float sizeSharpness = 15f;

    private RectTransform root;
    private float currentRadius;

    private void Awake()
    {
        root = GetComponent<RectTransform>();

        RebindSceneReferences();

        currentRadius = baseRadius;
        Cursor.visible = false;

        SetZoomVisual(false);
    }

    // 씬이 바뀌면 무기를 든 플레이어가 새로 생기므로 다시 찾는다 (PersistentUiRoot 가 호출)
    public void RebindSceneReferences()
    {
        weapon = FindAnyObjectByType<WeaponController>();

        // 사망 패널이나 인벤토리 때문에 꺼진 채로 씬을 넘어왔을 수 있다. 새 씬에서는 항상 켠 상태로 시작한다.
        // (그 씬에서 바로 인벤토리를 열면 PlayerInventoryToggle 이 다시 꺼준다)
        SetCrosshairActive(true);
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

    // PlayerZoom 이 줌 시작/종료마다 호출한다 - 평소 크로스헤어와 줌 UI를 서로 바꿔 켠다
    public void SetZoomVisual(bool zoomed)
    {
        if (normalCrosshairRoot != null)
        {
            normalCrosshairRoot.SetActive(!zoomed);
        }

        if (zoomUI != null)
        {
            zoomUI.SetActive(zoomed);
        }
    }

    private void FollowMouse()
    {
        Vector2 mouse = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width, Screen.height) * 0.5f;

        // WeaponController 가 관리하는 반동 킥 오프셋을 그대로 더한다 - PlayerController 의
        // 조준 계산도 같은 값을 보므로, 크로스헤어가 튄 방향과 실제 탄착 방향이 항상 일치한다.
        Vector2 recoilKick = weapon != null ? weapon.RecoilKickOffset : Vector2.zero;
        root.position = new Vector3(mouse.x + recoilKick.x, mouse.y + recoilKick.y, 0f);
    }

    private void UpdateSpreadVisual()
    {
        // 무기가 없으면(맨손) 퍼짐 0 으로 취급한다. 맨손이어도 크로스헤어 자체는 계속 보인다 (나중에 바뀔 수 있음)
        float normalized = weapon != null ? weapon.CurrentSpreadNormalized : 0f;
        float maxSpreadDegrees = weapon != null ? weapon.EquippedMaxSpreadDegrees : 0f;
        float maxRadius = baseRadius + maxSpreadDegrees * radiusPerSpreadDegree;
        float targetRadius = Mathf.Lerp(baseRadius, maxRadius, normalized);

        currentRadius = Mathf.Lerp(currentRadius, targetRadius, 1f - Mathf.Exp(-sizeSharpness * Time.deltaTime));

        SetArm(upArm, Vector2.up);
        SetArm(downArm, Vector2.down);
        SetArm(leftArm, Vector2.left);
        SetArm(rightArm, Vector2.right);

        // 줌 UI 쪽 화살표도 같은 반경으로 같이 움직인다 (둘 중 하나는 꺼져 있어도 그냥 무해하게 계산됨)
        SetArm(zoomUpArm, Vector2.up);
        SetArm(zoomDownArm, Vector2.down);
        SetArm(zoomLeftArm, Vector2.left);
        SetArm(zoomRightArm, Vector2.right);
    }

    private void SetArm(RectTransform arm, Vector2 direction)
    {
        if (arm != null)
        {
            arm.anchoredPosition = direction * currentRadius;
        }
    }
}
