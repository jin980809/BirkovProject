using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyDetect : MonoBehaviour, IHearing
{
    [SerializeField] private EnemyData enemyData;

    public enum EnemyState
    {
        Revert,     //복귀
        Patrol,     //순찰
        Search,     //수색
        Battle,     //전투
        Die         //사망
    }

    public EnemyState enemyState; // { get; private set; } 


    [Header("시야")]
    [SerializeField] private float rayDistance;
    [Range(0f, 360f)]
    [SerializeField] private float viewAngle;
    
    [Header("Raycast 설정")]
    [SerializeField] private int rayCount;
    [SerializeField] private float detectInterval;

    [Header("포착 마스크")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;
    
    [Header("감지된 타겟")]
    [SerializeField] private Transform visibleTargets;
    [SerializeField] private Vector3 visibleTargetsV3;
    
    private Vector3 dirToTarget;
    

    [Header("감지도")]
    [SerializeField] private float currentDetection;
    [SerializeField] private bool searchCheck = true;
    [SerializeField] private bool firstCheck = false;
    [SerializeField] private float decrease;
    [SerializeField] private float walkPoints;
    [SerializeField] private float runPoints;
    [SerializeField] private float shotPoints;

    //시야용 변수들
    int count;              //Ray 개수
    float startAngle;       //첫 각도
    float angleStep;        //Ray 사이 각도
    float currentAngle;     //발사되고 있는 Ray 각도
    bool isHit;             //맞았는지 확인용
    Vector3 rayDirection;   //각도 벡터값으로 변환용
    RaycastHit hit;         //타겟
    Transform target;       //타겟 위치값
    private WaitForSeconds delay;

    private void Awake()
    {
        //적 데이타 캐싱---------------------------------------
        rayDistance = enemyData.rayDistance;
        viewAngle = enemyData.viewAngle;
        rayCount = enemyData.rayCount;
        detectInterval = enemyData.detectInterval;
        decrease = enemyData.decrease;
        walkPoints = enemyData.walkPoints;
        runPoints = enemyData.runPoints;
        shotPoints = enemyData.shotPoints;
        //-----------------------------------------------------

        enemyState = EnemyState.Patrol;
        rayDistance = Mathf.Max(0f, rayDistance);
    }

    private void Start()
    {
        delay = new WaitForSeconds(detectInterval);
        StartCoroutine(FindTargetsWithDelay());
    }

    private void Update()
    {
        //게이지 감소
        if ((currentDetection > 0f && searchCheck) || currentDetection > 60f)
        {
            currentDetection -= decrease * Time.deltaTime;
        }
        //적 상태 변환
        if (currentDetection > 60f && !(visibleTargets == null))
        {
            enemyState = EnemyState.Battle;
        }
        else if (currentDetection > 30f)
        {
            PositionReset();
            enemyState = EnemyState.Search;
        }
        else if (currentDetection > 0f)
        {
            enemyState = EnemyState.Revert;
        }
    }

    //-------------------------적 소리 메서드 -----------------------------

    public void OnHeardNoise(Vector3 source, int a)
    {
        switch (a)
        {
            case 0: //걷기
                currentDetection += walkPoints;
                break;
            case 1: //뛰기
                currentDetection += runPoints;
                break;
            case 2: //총쏘기
                currentDetection += shotPoints;
                break;
        }
        //100을 넘지 않게
        if (currentDetection > 100f)
        {
            currentDetection = 100f;
        }

        visibleTargetsV3 = source;
    }

    //-------------------------적 시야 메서드 -----------------------------

    //죽었을때 조건 초가해야함---------------------------------------------<-----------------------------------------
    //실행문
    private IEnumerator FindTargetsWithDelay()
    {
        while (true)
        {
            yield return delay;
            FindVisibleTargets();
        }
    }
    //레이 탐지
    private void FindVisibleTargets()
    {
        count = Mathf.Max(1, rayCount); //ray 개수 최소 1개

        startAngle = -viewAngle / 2f; //첫번째 각도

        if (count == 1)
        {
            angleStep = 0f; //Ray가 1개일때
        }
        else
        {
            angleStep = viewAngle / (count - 1); //전체 시야각을 Ray 개수 - 1로 나누기
        }

        //Ray 발사
        for (int i = 0; i < count; i++)
        {
            currentAngle = startAngle + angleStep * i; //현재 Ray가 발사될 각도
            rayDirection = DirFromAngle(currentAngle, false); //현재 각도를 실제 Vector3 방향으로 변환

            int layerMask = targetMask | obstacleMask;
            isHit = Physics.Raycast(transform.position, rayDirection, out hit, rayDistance, layerMask);

            //아무것도 안맞으면
            if (!isHit)
            {
                continue;
            }

            if (IsInLayerMask(hit.collider.gameObject))
            {

                target = hit.collider.transform; //플레이어 발견


                dirToTarget = (target.position - transform.position).normalized; //실제로 플레이어가 있는 방향 저장

                if (visibleTargets == null)
                {
                    firstCheck = true;
                    visibleTargets = target; //현재 발견한 플레이어를 저장
                }
                visibleTargetsV3 = target.position; //마지막으로 본 플레이어 위치

               currentDetection = 100f; //감지도 변경

                return; //검사 종료
            }

            //장애물에 막힘
            if (IsInLayerMask(hit.collider.gameObject))
            {
                continue;
            }
        }
    }
    //Layer판단
    private bool IsInLayerMask(GameObject obj)
    {
        return LayerMask.LayerToName(obj.layer) == "Player";
    }

    //---------------------------참조용 메서드 -----------------------------

    public float ViewRadius()
    {
        return rayDistance;
    }

    public float ViewAngle()
    {
        return viewAngle;
    }

    public Transform VisibleTargets()
    {
        if (visibleTargets != null)
        {
            return visibleTargets;
        }
        else
        {
            return null;
        }
    }

    public float CurrentDetection()
    {
        return currentDetection;
    }

    public bool SearchCheck()
    {
        return searchCheck;

    }

    public bool FirstCheck()
    {
        return firstCheck;
    }

    public Vector3 VisibleTargetsV3()
    {
        return visibleTargetsV3;
    }

    public EnemyState EnemyStatePatrolChange()
    {
        return enemyState = EnemyState.Patrol;
    }

    //--------------------------- 기타 메서드 -----------------------------

    //플레이어 트렌스폼 값 초기화
    public void PositionReset()
    {
        visibleTargets = null;
    }
    //수색끝
    public void SearchCheckOn()
    {
        searchCheck = true;
    }
    //수색시작
    public void SearchCheckOff()
    {
        searchCheck = false;
    }
    //첫 발견인지
    public void FirstCheckOn()
    {
        firstCheck = true;
    }
    public void FirstCheckOff()
    {
        firstCheck = false;
    }
    //각도 변환
    public Vector3 DirFromAngle(float angleDegrees, bool angleIsGlobal)
    {
        // angleIsGlobal이 false라면
        // 현재 Enemy의 Y 회전값을 추가함.
        //
        // 따라서 Enemy가 회전하면
        // Ray의 방향도 같이 회전함.
        if (!angleIsGlobal)
        {
            angleDegrees += transform.eulerAngles.y;
        }

        return new Vector3(Mathf.Cos((-angleDegrees + 90f) * Mathf.Deg2Rad), 0f, Mathf.Sin((-angleDegrees + 90f) * Mathf.Deg2Rad));
    }
    //디버기용
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, rayDistance);

        if (count == 1)
        {
            angleStep = 0f;
        }
        else
        {
            angleStep = viewAngle / (count - 1);
        }

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            Vector3 direction = DirFromAngle(currentAngle, false);
            Gizmos.DrawRay(transform.position, direction * rayDistance);
        }
    }
}