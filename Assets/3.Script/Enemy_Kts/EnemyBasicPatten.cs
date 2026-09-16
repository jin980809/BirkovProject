using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBasicPatten : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;

    private Animator ani;
    private NavMeshAgent agent;
    private EnemyDetect enemyDetect;
    private EnemyShot enemyShot;

    private Vector3 respawnPoint;
    private int rnd;

    private bool canRevert = true;
    private bool canPatrol = true;
    private bool canBattle = true;

    private WaitForSeconds patrolWaitingTimeWfs;
    private WaitForSeconds exclamationTimeWfs;
    private WaitForSeconds delayTimeWfs;

    [Header("순찰 정보")]
    [SerializeField] private Transform[] wayPoints;
    [SerializeField] private float patrolWaitingTime;

    [Header("속도")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float angleSpeed;

    [Header("타겟")]
    [SerializeField] private Transform target;

    [Header("이미지")]
    [SerializeField] private GameObject questionMark;
    [SerializeField] private GameObject exclamationMark;

    private void Awake()
    {
        //적 데이타 캐싱---------------------------------------
        moveSpeed = enemyData.moveSpeed;
        angleSpeed = enemyData.angleSpeed;
        //-----------------------------------------------------
        TryGetComponent(out agent);
        TryGetComponent(out ani);
        TryGetComponent(out enemyDetect);
        TryGetComponent(out enemyShot);

    }   

    private void Start()
    {
        agent.speed = moveSpeed;
        agent.angularSpeed = angleSpeed;
        patrolWaitingTime = enemyData.patrolWaitingTime;

        patrolWaitingTimeWfs = new WaitForSeconds(patrolWaitingTime);
        exclamationTimeWfs = new WaitForSeconds(0.7f);
        delayTimeWfs = new WaitForSeconds(1.5f);

        respawnPoint = transform.position;
    }

    private void Update()
    {
        
        //죽었을때 조건 추가해야함---------------------------------------------------------------------------------------
        if (enemyDetect.enemyState.Equals(EnemyDetect.EnemyState.Patrol))
        {
            if (enemyDetect.VisibleTargets() == null)
            {
                target = null;
            }
            PatrolMove();
        }
        else if (enemyDetect.enemyState.Equals(EnemyDetect.EnemyState.Battle))
        {
            if (enemyDetect.VisibleTargets() != null)
            {
                target = enemyDetect.VisibleTargets();
                BattleMove();
            }
            else
            {
                Debug.Log("플레이어를 찾지 못함");
            }
        }
        else if (enemyDetect.enemyState.Equals(EnemyDetect.EnemyState.Search))
        {
            SearchMove();
        }
        else if (enemyDetect.enemyState.Equals(EnemyDetect.EnemyState.Revert))
        {
            RevertMove();
        }
    }

    //------------------------------플레이어를 발견하지 못한 상태인 순찰 상태----------------------------------

    //실행
    private void PatrolMove()
    {
        if (canPatrol && !wayPoints.Length.Equals(0))
        {
            StartCoroutine(PatrolMove_co());
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            ani.SetBool("Walk", false);
        }
    }
    //지정 포인트 순찰
    private IEnumerator PatrolMove_co()
    {
        canPatrol = false;

        ani.SetBool("Walk", true);

        rnd = Random.Range(0, wayPoints.Length);
        agent.destination = wayPoints[rnd].position;

        yield return patrolWaitingTimeWfs;

        canPatrol = true;
    }

    //---------------------플레이어를 아직 발견하지 못했거나 놓쳤을 때  수색 상태-------------------------

    //플레이어를 본 마지막 지점 수색
    public void SearchMove()
    {
        agent.isStopped = false;
        agent.updateRotation = true;
        ani.SetBool("Walk", true);

        enemyDetect.SearchCheckOff();
        questionMark.SetActive(true);

        agent.destination = enemyDetect.VisibleTargetsV3();
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            questionMark.SetActive(false);
            enemyDetect.SearchCheckOn();
            ani.SetBool("Walk", false);
        }
    }

    //----------------------------------플레이어를 발견해 전투 상태--------------------------------------

    //실행
    public void BattleMove()
    {
        LookAtTarget();
        if (!enemyDetect.SearchCheck())
        {
            enemyDetect.SearchCheckOn();
            agent.ResetPath();
        }

        if (canBattle)
        {
            StartCoroutine(BattleActionLoop_co());
        }
    }
    //상황별 패턴묶음
    private IEnumerator BattleActionLoop_co()
    {
        canBattle = false;
    
        int a = Random.Range(0, 2);

        if (enemyDetect.FirstCheck())
        {
            agent.isStopped = true;
            ani.SetBool("Walk", false);

            if (questionMark.activeSelf)
            {
                questionMark.SetActive(false);
            }
            agent.updateRotation = false;
            exclamationMark.SetActive(true);


            yield return exclamationTimeWfs;

            exclamationMark.SetActive(false);

            agent.isStopped = false;
            enemyDetect.FirstCheckOff();
        }

        if (Vector3.Distance(target.position, transform.position) < 7f)
        {
            switch (a)
            {
                case 0:
                    yield return StartCoroutine(Patten3_co());
                    break;
                case 1:
                    yield return StartCoroutine(Patten2_co());
                    break;
            }
        }
        else if(Vector3.Distance(target.position, transform.position) < enemyData.rayDistance)
        {
            switch (a)
            {
                case 0:
                    yield return StartCoroutine(Patten1_co());
                    break;
                case 1:
                    yield return StartCoroutine(Patten2_co());
                    break;
            }
        }
        else
        {
            yield return StartCoroutine(Patten1_co());
        }
        canBattle = true ;
    
    }
    //딜레이 정도 무기에 따라 수정해야함------------------------<--------------
    //전진 공격 패턴
    private IEnumerator Patten1_co()
    {
        ani.SetBool("Run", true);
        agent.isStopped = false;

        agent.destination = enemyDetect.VisibleTargets().position;

        yield return delayTimeWfs;

        agent.isStopped = true;
        ani.SetBool("Run", false);

        yield return StartCoroutine(enemyShot.Fire_co());
    }
    //정지 공격 패턴
    private IEnumerator Patten2_co()
    {
        yield return delayTimeWfs;

        yield return StartCoroutine(enemyShot.Fire_co());
    }
    //랜덤 위치 이동 패턴
    private IEnumerator Patten3_co()
    {
        ani.SetBool("Run", true);
        agent.isStopped = false;

        agent.destination = GetRandomPositionOnNavMesh();

        yield return delayTimeWfs;

        agent.isStopped = true;
        ani.SetBool("Run", false);

        yield return StartCoroutine(enemyShot.Fire_co());
    }

    //----------------------------------플레이어를 발견해 전투 상태--------------------------------------

    //스폰포인트로 돌아가기
    public void RevertMove()
    {
        if (canRevert)
        {
            canRevert = false;

            agent.destination = respawnPoint;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            ani.SetBool("Walk", false);
            enemyDetect.PositionReset();
            enemyDetect.EnemyStatePatrolChange();
            canRevert = true;
        }
    }

    //-----------------------------------------기타 메서드-----------------------------------------------

    //플레이어 바라보기
    private void LookAtTarget()
    {
        Vector3 direction = enemyDetect.VisibleTargets().position - transform.position;

        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime
            );
        }
    }
    //랜덤 위치 찾기
    private Vector3 GetRandomPositionOnNavMesh()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 20f; //범위 내의 랜덤한 방향 벡터 생성
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 20f, NavMesh.AllAreas)) //NavMesh 위에 있는지 확인
        {
            return hit.position; //NavMesh 위의 랜덤 위치 반환
        }
        else
        {
            return transform.position; //현재 위치 반환
        }
    }
}

