using UnityEngine;
using UnityEngine.UI;

// 스나이퍼 조준 오버레이. 커서 자리에 원형 구멍을 남기고 그 바깥을 반투명하게 덮는다 (구멍 안에 십자선).
// 켜고 끄는 것은 PlayerZoom 이 무기별 설정(Use Scope)을 보고 호출한다.
//
// 세팅:
//  - 유지되는 UI 캔버스(PersistentUiRoot) 안에 화면 전체를 덮는 Image 를 만들고 이 컴포넌트를 붙인다.
//  - 그 Image 의 Material 에 ScopeMask 머티리얼(Birkov/UI/ScopeMask 셰이더)을 연결한다.
//  - Image 의 Raycast Target 은 끈다 (클릭을 가로막지 않게).
//  - 그리기 순서: 크로스헤어보다 뒤(위에 덮도록), HUD·ESC 메뉴보다 앞에 둔다.
//    그러면 구멍 안의 크로스헤어는 그대로 보이고, 체력/탄약 HUD 는 어두운 영역 위에 계속 보인다.
//
// 커서 위치는 크로스헤어와 같은 값(입력 + 반동 킥)을 쓴다 - 그래야 구멍 중심과 조준선이 항상 붙어 있다.
public class SniperScopeOverlay : MonoBehaviour, ISceneRebindable
{
    [Header("연결")]
    [Tooltip("화면 전체를 덮는 Image (ScopeMask 머티리얼). 비우면 이 오브젝트에서 찾는다")]
    [SerializeField] private Image overlay;

    [Header("구멍")]
    [Tooltip("구멍 반지름 - 화면 높이 기준 비율 (0.18 = 화면 높이의 18%)")]
    [SerializeField, Range(0.02f, 0.6f)] private float holeRadius = 0.18f;
    [Tooltip("구멍 경계가 흐려지는 폭")]
    [SerializeField, Range(0f, 0.2f)] private float edgeSoftness = 0.03f;

    [Header("바깥 어둡기")]
    [Tooltip("구멍 밖을 덮는 색. 알파로 어둡기를 정한다 (1 = 완전히 가림)")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.85f);

    [Header("십자선")]
    [SerializeField] private Color crossColor = new Color(1f, 1f, 1f, 0.8f);
    [Tooltip("선 두께 - 화면 높이 기준 비율")]
    [SerializeField, Range(0.0002f, 0.01f)] private float crossThickness = 0.0015f;
    [Tooltip("선 길이 - 구멍 반지름의 배수 (1 = 구멍 경계까지, 1.5 = 구멍 밖으로 조금 더)")]
    [SerializeField, Range(0.2f, 3f)] private float crossLength = 1f;

    [Header("페이드")]
    [SerializeField] private float fadeDuration = 0.15f;

    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int DimColorId = Shader.PropertyToID("_DimColor");
    private static readonly int CrossColorId = Shader.PropertyToID("_CrossColor");
    private static readonly int CrossThicknessId = Shader.PropertyToID("_CrossThickness");
    private static readonly int CrossLengthId = Shader.PropertyToID("_CrossLength");

    private PlayerInputHandler input;
    private WeaponController weapon;
    private Material material;

    private bool shown;
    private float currentAlpha;
    private float activeRadius;

    private void Awake()
    {
        if (overlay == null)
        {
            TryGetComponent(out overlay);
        }

        activeRadius = holeRadius;
        EnsureMaterial();
        ApplyAlpha(0f); // 시작할 땐 안 보이게
    }

    // 씬이 바뀌면 플레이어가 새로 생기므로 입력/무기를 다시 찾고, 켜져 있던 오버레이는 닫는다
    public void RebindSceneReferences()
    {
        input = FindAnyObjectByType<PlayerInputHandler>();
        weapon = FindAnyObjectByType<WeaponController>();
        Hide();
    }

    // PlayerZoom 이 조준을 시작할 때 부른다. radius 가 0 보다 크면 그 값을 쓰고, 아니면 인스펙터 기본값.
    public void Show(float radius)
    {
        activeRadius = holeRadius;

        if (radius > 0f)
        {
            activeRadius = radius;
        }

        shown = true;
    }

    public void Hide()
    {
        shown = false;
    }

    private void Update()
    {
        UpdateFade();

        // 완전히 사라진 뒤에는 셰이더 값을 갱신할 필요가 없다
        if (currentAlpha > 0f)
        {
            UpdateMaterial();
        }
    }

    private void UpdateFade()
    {
        float target = 0f;

        if (shown)
        {
            target = 1f;
        }

        if (fadeDuration > 0f)
        {
            float step = Time.unscaledDeltaTime / fadeDuration;
            currentAlpha = Mathf.MoveTowards(currentAlpha, target, step);
        }
        else
        {
            currentAlpha = target;
        }

        ApplyAlpha(currentAlpha);
    }

    private void ApplyAlpha(float alpha)
    {
        if (overlay == null)
        {
            return;
        }

        // 정점 색 알파로 전체 페이드 (셰이더가 이 값을 곱한다)
        Color color = overlay.color;
        color.a = alpha;
        overlay.color = color;

        bool visible = alpha > 0f;

        if (overlay.enabled != visible)
        {
            overlay.enabled = visible; // 다 사라지면 그리기 자체를 멈춘다
        }
    }

    private void UpdateMaterial()
    {
        EnsureMaterial();

        if (material == null)
        {
            return;
        }

        material.SetVector(CenterId, GetCursorViewportPoint());
        material.SetFloat(RadiusId, activeRadius);
        material.SetFloat(SoftnessId, edgeSoftness);
        material.SetFloat(AspectId, GetScreenAspect());
        material.SetColor(DimColorId, dimColor);
        material.SetColor(CrossColorId, crossColor);
        material.SetFloat(CrossThicknessId, crossThickness);
        material.SetFloat(CrossLengthId, crossLength);
    }

    // 크로스헤어와 같은 위치를 쓴다 (마우스 + 반동 킥). 입력이 없으면 화면 중앙.
    private Vector4 GetCursorViewportPoint()
    {
        Vector2 screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (input != null)
        {
            screenPoint = input.LookScreenPosition;

            if (weapon != null)
            {
                screenPoint += weapon.RecoilKickOffset;
            }
        }

        float x = 0.5f;
        float y = 0.5f;

        if (Screen.width > 0 && Screen.height > 0)
        {
            x = screenPoint.x / Screen.width;
            y = screenPoint.y / Screen.height;
        }

        return new Vector4(x, y, 0f, 0f);
    }

    private float GetScreenAspect()
    {
        if (Screen.height > 0)
        {
            return (float)Screen.width / Screen.height;
        }

        return 1f;
    }

    // Image 에 연결된 머티리얼을 복제해서 쓴다 - 에셋 원본의 값을 런타임에 덮어쓰지 않기 위해서다.
    private void EnsureMaterial()
    {
        if (material != null || overlay == null)
        {
            return;
        }

        if (overlay.material == null)
        {
            Debug.LogWarning("SniperScopeOverlay: Image 에 ScopeMask 머티리얼이 연결되지 않아 오버레이가 보이지 않습니다.", this);
            return;
        }

        material = new Material(overlay.material);
        overlay.material = material;
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}
