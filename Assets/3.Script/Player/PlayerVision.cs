using System.Collections.Generic;
using UnityEngine;

// 플레이어 시야(부채꼴) 구현
//  - 매 프레임 부채꼴로 레이캐스트해서 FOV 메시를 만든다 (벽에 맞물림)
//  - FOV 메시는 스텐실 버퍼에 1을 써서 "보이는 영역" 을 표시한다 (VisionMask 셰이더)
//  - 화면을 덮는 오버레이 쿼드가 스텐실 != 1 인 곳(시야 밖)만 어둡게 덮는다 (VisionOverlay 셰이더)
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

    private Mesh viewMesh;
    private readonly List<Vector3> viewPoints = new List<Vector3>();

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
    }

    private void Start()
    {
        RefreshHideables();
    }

    private void LateUpdate()
    {
        DrawFieldOfView();
        UpdateHideableVisibility();
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

    // 특정 월드 좌표가 시야 안에 있고 벽에 가려지지 않았는지
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
        if (distance > viewRadius)
        {
            return false;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (Vector3.Angle(forward, to) > viewAngle * 0.5f)
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
        int stepCount = Mathf.Max(1, Mathf.RoundToInt(viewAngle * meshResolution));
        float stepAngleSize = viewAngle / stepCount;

        viewPoints.Clear();
        ViewCastInfo previous = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle * 0.5f + stepAngleSize * i;
            ViewCastInfo current = ViewCast(angle);

            if (i > 0)
            {
                bool thresholdExceeded = Mathf.Abs(previous.distance - current.distance) > edgeDistanceThreshold;
                if (previous.hit != current.hit || (previous.hit && current.hit && thresholdExceeded))
                {
                    EdgePoints edge = FindEdge(previous, current);
                    if (edge.hasA)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.hasB)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }

            viewPoints.Add(current.point);
            previous = current;
        }

        int vertexCount = viewPoints.Count + 1;

        // 매 프레임 배열 할당 - 광선 수가 많지 않아 문제 없음. 필요하면 버퍼 재사용으로 최적화
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[Mathf.Max(0, (vertexCount - 2) * 3)];

        // 부채꼴 중심(플레이어 위치)도 바닥 높이로
        Vector3 centerWorld = new Vector3(transform.position.x, groundY + meshHeight, transform.position.z);
        vertices[0] = transform.InverseTransformPoint(centerWorld);

        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
    }

    private ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 direction = DirectionFromAngle(globalAngle);
        Vector3 origin = transform.position + Vector3.up * sightHeight;

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, viewRadius, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 point = new Vector3(hit.point.x, groundY + meshHeight, hit.point.z);
            return new ViewCastInfo(true, point, hit.distance, globalAngle);
        }

        Vector3 endPoint = transform.position + direction * viewRadius;
        endPoint.y = groundY + meshHeight;
        return new ViewCastInfo(false, endPoint, viewRadius, globalAngle);
    }

    private EdgePoints FindEdge(ViewCastInfo minCast, ViewCastInfo maxCast)
    {
        float minAngle = minCast.angle;
        float maxAngle = maxCast.angle;

        EdgePoints result = new EdgePoints();

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) * 0.5f;
            ViewCastInfo current = ViewCast(angle);

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

    private bool IsInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;
        float half = viewAngle * 0.5f;
        Vector3 left = Quaternion.Euler(0f, -half, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, half, 0f) * transform.forward;
        Gizmos.DrawRay(origin, left * viewRadius);
        Gizmos.DrawRay(origin, right * viewRadius);
    }
}
