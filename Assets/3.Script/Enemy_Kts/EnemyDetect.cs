using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDetect : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Search,
        Battle,
        Die
    }
    public EnemyState enemyState { get; private set; }

    [Header("시야")]
    [SerializeField] private float viewRadius;
    [Range(0, 360)]
    [SerializeField] private float viewAngle;
    [Header("마스크")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;
    [Header("포착 마스크")]
    [SerializeField] private Transform visibleTargets;
    [SerializeField] private Collider[] targetsInViewRadius;
    [Header("감지도")]
    [SerializeField] private float currentDetection;
    [SerializeField] private float decrease;

    private Transform target;
    private Vector3 dirToTarget;
    private void Awake()
    {
        enemyState = EnemyState.Patrol;
        targetsInViewRadius = new Collider[1];
    }
    private void Start()
    {
        StartCoroutine(FindTargetsWithDelay(0.2f));
    }

    private void Update()
    {
        if (currentDetection > 0)
        {
            currentDetection -= decrease * Time.deltaTime;
        }

        if (currentDetection > 60)
        {
            enemyState = EnemyState.Battle;
        }
        else if (currentDetection > 30 && currentDetection < 60)
        {
            enemyState = EnemyState.Search;
        }
    }
    //-------------------------적 소리 메서드 -----------------------------





















    //-------------------------적 시야 메서드 -----------------------------
    private IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    private void FindVisibleTargets()
    {
        //리스트 초기화
        visibleTargets = null;

        //viewRadius를 반지름으로 한 원 영역 내 targetMask 레이어인 콜라이더를 모두 가져옴
        int count = Physics.OverlapSphereNonAlloc(transform.position, viewRadius, targetsInViewRadius, targetMask);

        if (count > 0)
        {
            target = targetsInViewRadius[0].transform;
            dirToTarget = (target.position - transform.position).normalized;

            // 플레이어와 forward와 target이 이루는 각이 설정한 각도 내라면
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.transform.position);

                // 타겟으로 가는 레이캐스트에 obstacleMask가 걸리지 않으면 visibleTargets에 Add
                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    visibleTargets = target;
                    currentDetection = 100f;
                }
            }
        }

    }
    //---------------------------참조용 메서드 -----------------------------
    public float ViewAngle()
    {
        return viewAngle;
    }

    public Transform VisibleTargets()
    {
        return visibleTargets;
    }


    //---------------------------확인용 메서드 -----------------------------
    public Vector3 DirFromAngle(float angleDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleDegrees += transform.eulerAngles.y;
        }

        return new Vector3(Mathf.Cos((-angleDegrees + 90) * Mathf.Deg2Rad), 0, Mathf.Sin((-angleDegrees + 90) * Mathf.Deg2Rad));
    }

}
