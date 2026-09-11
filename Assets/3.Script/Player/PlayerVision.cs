using System.Collections.Generic;
using UnityEngine;

// 플레이어 시야 구현
//  - 정면 부채꼴(viewAngle / viewRadius) + 플레이어 주변 360도 원(nearVisionRadius)
//  - 매 프레임 레이캐스트해서 FOV 메시를 만든다 (벽에 맞물림)
//  - FOV 메시는 스텐실 버퍼에 1을 써서 "보이는 영역" 을 표시한다 (VisionMask 셰이더)
//  - 화면을 덮는 오버레이 쿼드가 스텐실 != 1 인 곳(시야 밖) 바닥만 어둡게 덮는다 (VisionOverlay 셰이더)
//  - hideableMask 레이어의 오브젝트는 시야 밖 / 벽에 가려짐이면 렌더러를 끈다
//
// 배치: 플레이어 자식으로 빈 오브젝트 "Vision" 을 만들고 (로컬 위치 0,0,0 / 회전 0)
//       MeshFilter + MeshRenderer(VisionMask 머티리얼) + 이 스크립트를 붙인다.
[RequireComponent(typeof(MeshFilter))]
public class PlayerVision : MonoBehaviour
{
    [Header("시야 범위")]
    [SerializeField, Range(0f, 360f)] private float viewAngle = 100f;
    [SerializeField] private float viewRadius = 15f;
    [Tooltip("플레이어 주변 360도로 항상 보이는 반경 (0 이면 사용 안 함)")]
    [SerializeField] private float nearVisionRadius = 3f;

    [Header("레이어")]
    [Tooltip("시야를 막는 벽")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("시야 밖이면 숨길 대상 (적 등)")]
    [SerializeField] private LayerMask hideableMask;

    [Header("시야 높이")]
    [Tooltip("레이를 쏘는 높이 (벽 콜라이더에 맞도록)")]
    [SerializeField] private float sightHeight = 1f;

    [Header("바닥")]
    [Tooltip("바닥의 월드 Y 좌표. FOV 메시가 이 높이에 그려진다 (플레이어 피벗 위치와 무관하게 고정)")]
    [SerializeField] private float groundY = 0f;
    [Tooltip("FOV 메시를 바닥 위로 띄우는 높이")]
    [SerializeField] private float meshHeight = 0.05f;

    [Header("메시 품질")]
    [Tooltip("1도당 광선 수")]
    [SerializeField] private float meshResolution = 1f;
    [SerializeField] private int edgeResolveIterations = 6;
    [SerializeField] private float edgeDistanceThreshold = 0.5f;

    [Header("경계 그라데이션")]
    [Tooltip("거리 경계(부채꼴 끝 / 근접 원)에서 부드럽게 페이드되는 폭 (m)")]
    [SerializeField] private float edgeFade = 2f;
    [Tooltip("부채꼴 좌우 경계에서 부드럽게 페이드되는 각도 (deg)")]
    [SerializeField] private float angleFade = 12f;

    [Header("조준 방향")]
    [Tooltip("설정하면 시야가 이 컨트롤러의 조준(마우스) 방향을 향한다. 비우면 자기 transform.forward. 부모에서 자동 탐색.")]
    [SerializeField] private PlayerController aimSource;

    private Mesh viewMesh;

    // 재사용 버퍼 (프레임당 힙 할당 방지)
    private readonly List<Vector3> conePoints = new List<Vector3>();
    private readonly List<Vector3> nearPoints = new List<Vector3>();
    private readonly List<Vector3> meshVertices = new List<Vector3>();
    private readonly List<int> meshTriangles = new List<int>();

    // 숨김 대상 하나
    private class Hideable
    {
        public Collider collider;
        public Renderer[] renderers;
        public bool visible;
    }

    private readonly List<Hideable> hideables = new List<Hideable>();

    private struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float distance;
        public float angle;

        public ViewCastInfo(bool hit, Vector3 point, float distance, float angle)
        {
            this.hit = hit;
            this.point = point;
            this.distance = distance;
            this.angle = angle;
        }
    }

    private struct EdgePoints
    {
        public bool hasA;
        public bool hasB;
        public Vector3 pointA;
        public Vector3 pointB;
    }

    private void Awake()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMesh.MarkDynamic();
        GetComponent<MeshFilter>().mesh = viewMesh;

        if (aimSource == null)
        {
            aimSource = GetComponentInParent<PlayerController>();
        }
    }

    private void Start()
    {
        RefreshHideables();
    }

    private void LateUpdate()
    {
        DrawFieldOfView();
        UpdateVisionShaderGlobals();
        UpdateHideableVisibility();
    }

    // 오버레이 셰이더(VisionOverlaySoft)가 경계 그라데이션을 계산하는 데 쓰는 전역 값
    private void UpdateVisionShaderGlobals()
    {
        Vector3 forward = GetFacingDirection();

        float half = viewAngle * 0.5f;
        float cosOuter = Mathf.Cos(half * Mathf.Deg2Rad);
        float cosInner = Mathf.Cos(Mathf.Max(0f, half - angleFade) * Mathf.Deg2Rad);

        Shader.SetGlobalVector("_VisionPlayerPos", transform.position);
        Shader.SetGlobalVector("_VisionPlayerDir", forward);
        Shader.SetGlobalVector("_VisionParams", new Vector4(viewRadius, nearVisionRadius, edgeFade, groundY + meshHeight));
        Shader.SetGlobalVector("_VisionConeAngles", new Vector4(cosOuter, cosInner, 0f, 0f));
    }

    // ---------- 숨김 대상 관리 ----------

    // 씬에 있는 hideableMask 레이어 오브젝트를 모두 수집 (시작 시 1회)
    public void RefreshHideables()
    {
        hideables.Clear();

        Collider[] all = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (IsInMask(all[i].gameObject.layer, hideableMask))
            {
                AddHideable(all[i]);
            }
        }
    }

    // 스폰된 적 등을 나중에 등록
    public void RegisterHideable(Collider target)
    {
        if (target == null || !IsInMask(target.gameObject.layer, hideableMask))
        {
            return;
        }

        for (int i = 0; i < hideables.Count; i++)
        {
            if (hideables[i].collider == target)
            {
                return;
            }
        }

        AddHideable(target);
    }

    private void AddHideable(Collider target)
    {
        Hideable h = new Hideable();
        h.collider = target;
        h.renderers = target.GetComponentsInChildren<Renderer>(true);
        h.visible = true;
        SetHideableVisible(h, false); // 기본은 숨김
        hideables.Add(h);
    }

    private void UpdateHideableVisibility()
    {
        for (int i = hideables.Count - 1; i >= 0; i--)
        {
            Hideable h = hideables[i];
            if (h.collider == null)
            {
                hideables.RemoveAt(i);
                continue;
            }

            SetHideableVisible(h, CanSee(h.collider.bounds.center));
        }
    }

    private void SetHideableVisible(Hideable h, bool value)
    {
        if (h.visible == value)
        {
            return;
        }

        h.visible = value;
        for (int i = 0; i < h.renderers.Length; i++)
        {
            if (h.renderers[i] != null)
            {
                h.renderers[i].forceRenderingOff = !value;
            }
        }
    }

    // 특정 월드 좌표가 시야(정면 부채꼴 또는 근접 원) 안이고 벽에 가려지지 않았는지
    public bool CanSee(Vector3 worldPoint)
    {
        Vector3 origin = transform.position;

        Vector3 to = worldPoint - origin;
        to.y = 0f;

        float distance = to.magnitude;
        if (distance < 0.001f)
        {
            return true; // 거의 같은 위치
        }

        bool inNear = distance <= nearVisionRadius;

        bool inCone = false;
        if (distance <= viewRadius)
        {
            Vector3 forward = GetFacingDirection();
            inCone = Vector3.Angle(forward, to) <= viewAngle * 0.5f;
        }

        if (!inNear && !inCone)
        {
            return false;
        }

        Vector3 a = origin + Vector3.up * sightHeight;
        Vector3 b = new Vector3(worldPoint.x, origin.y, worldPoint.z) + Vector3.up * sightHeight;
        if (Physics.Linecast(a, b, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    // ---------- FOV 메시 ----------

    private void DrawFieldOfView()
    {
        BuildArc(GetFacingYaw() - viewAngle * 0.5f, viewAngle, viewRadius, conePoints);
        BuildArc(0f, 360f, nearVisionRadius, nearPoints);

        meshVertices.Clear();
        meshTriangles.Clear();

        // 정점 0 = 부채꼴 중심(플레이어 위치, 바닥 높이)
        Vector3 centerWorld = new Vector3(transform.position.x, groundY + meshHeight, transform.position.z);
        meshVertices.Add(transform.InverseTransformPoint(centerWorld));

        AppendFan(conePoints);
        AppendFan(nearPoints);

        // List 오버로드는 내부 할당 없이 리스트 버퍼를 그대로 읽는다
        viewMesh.Clear();
        viewMesh.SetVertices(meshVertices);
        viewMesh.SetTriangles(meshTriangles, 0);
    }

    // points 를 중심(정점 0) 기준 삼각형 부채꼴로 meshVertices/meshTriangles 에 이어붙인다
    private void AppendFan(List<Vector3> points)
    {
        int startIndex = meshVertices.Count;

        for (int i = 0; i < points.Count; i++)
        {
            meshVertices.Add(transform.InverseTransformPoint(points[i]));

            if (i < points.Count - 1)
            {
                int current = startIndex + i;
                meshTriangles.Add(0);
                meshTriangles.Add(current);
                meshTriangles.Add(current + 1);
            }
        }
    }

    private void BuildArc(float startAngle, float sweepAngle, float radius, List<Vector3> output)
    {
        output.Clear();

        if (radius <= 0.01f || sweepAngle <= 0.01f)
        {
            return;
        }

        int stepCount = Mathf.Max(1, Mathf.RoundToInt(sweepAngle * meshResolution));
        float stepAngleSize = sweepAngle / stepCount;

        ViewCastInfo previous = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = startAngle + stepAngleSize * i;
            ViewCastInfo current = ViewCast(angle, radius);

            if (i > 0)
            {
                bool thresholdExceeded = Mathf.Abs(previous.distance - current.distance) > edgeDistanceThreshold;
                if (previous.hit != current.hit || (previous.hit && current.hit && thresholdExceeded))
                {
                    EdgePoints edge = FindEdge(previous, current, radius);
                    if (edge.hasA)
                    {
                        output.Add(edge.pointA);
                    }
                    if (edge.hasB)
                    {
                        output.Add(edge.pointB);
                    }
                }
            }

            output.Add(current.point);
            previous = current;
        }
    }

    private ViewCastInfo ViewCast(float globalAngle, float radius)
    {
        Vector3 direction = DirectionFromAngle(globalAngle);
        Vector3 origin = transform.position + Vector3.up * sightHeight;

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, radius, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 point = new Vector3(hit.point.x, groundY + meshHeight, hit.point.z);
            return new ViewCastInfo(true, point, hit.distance, globalAngle);
        }

        Vector3 endPoint = transform.position + direction * radius;
        endPoint.y = groundY + meshHeight;
        return new ViewCastInfo(false, endPoint, radius, globalAngle);
    }

    private EdgePoints FindEdge(ViewCastInfo minCast, ViewCastInfo maxCast, float radius)
    {
        float minAngle = minCast.angle;
        float maxAngle = maxCast.angle;

        EdgePoints result = new EdgePoints();

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) * 0.5f;
            ViewCastInfo current = ViewCast(angle, radius);

            bool thresholdExceeded = Mathf.Abs(minCast.distance - current.distance) > edgeDistanceThreshold;
            if (current.hit == minCast.hit && !thresholdExceeded)
            {
                minAngle = angle;
                result.pointA = current.point;
                result.hasA = true;
            }
            else
            {
                maxAngle = angle;
                result.pointB = current.point;
                result.hasB = true;
            }
        }

        return result;
    }

    private Vector3 DirectionFromAngle(float globalAngleDegrees)
    {
        float rad = globalAngleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }

    // 시야가 향하는 방향 (XZ 평면 정규화).
    // aimSource 가 있으면 그 조준점(마우스) 방향, 없으면 몸통 방향.
    // → 달릴 때 몸은 이동 방향을 봐도 시야는 마우스를 향한다.
    private Vector3 GetFacingDirection()
    {
        if (aimSource != null && aimSource.HasAimPoint)
        {
            Vector3 dir = aimSource.AimWorldPoint - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                return dir.normalized;
            }
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }
        return forward.normalized;
    }

    private float GetFacingYaw()
    {
        Vector3 f = GetFacingDirection();
        return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
    }

    private bool IsInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;

        Gizmos.color = Color.yellow;
        float half = viewAngle * 0.5f;
        Vector3 facing = GetFacingDirection();
        Vector3 left = Quaternion.Euler(0f, -half, 0f) * facing;
        Vector3 right = Quaternion.Euler(0f, half, 0f) * facing;
        Gizmos.DrawRay(origin, left * viewRadius);
        Gizmos.DrawRay(origin, right * viewRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, nearVisionRadius);
    }
}
