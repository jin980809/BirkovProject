using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBasicPatten : MonoBehaviour
{
    //public enum EnemyState
    //{
    //    Revert,
    //    Patrol,
    //    Search,
    //    Battle,
    //    Die
    //}

    private Animator ani;
    private NavMeshAgent agent;
    private EnemyDetect enemyDetect;

    private Vector3 respawnPoint;
    private int rnd;

    private bool canRevert = true;
    private bool canPatrol = true;
    private bool canBattle = true;
    private WaitForSeconds wfs;

    [Header("추적할 대상 레이어")]
    [SerializeField] private LayerMask targetLayer;

    [Header("순찰 정보")]
    [SerializeField] private Transform[] wayPoints;
    [SerializeField] private float patrolWaitingTime;

    [Header("속도")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float angleSpeed = 360f;

    [Header("사거리")]
    [SerializeField] private float maximumRange = 7f;
    [SerializeField] private float effectiveRange = 3f;

    private float moveDistance = 3f;

    private void Awake()
    {
        TryGetComponent(out agent);
        TryGetComponent(out ani);
        TryGetComponent(out enemyDetect);

    }

    private void Start()
    {
        agent.speed = moveSpeed;
        agent.angularSpeed = angleSpeed;

        respawnPoint = transform.position;

        wfs = new WaitForSeconds(patrolWaitingTime);

        //적 기본 데이터 추가해야함


    }

    private void Update()
    {

        if (enemyDetect.enemyState.Equals(EnemyDetect.EnemyState.Patrol))
        {
            PatrolMove();
        }
        else if (enemyDetect.enemyState.Equals(EnemyDetect.EnemyState.Battle))
        {
            BattleMove();
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

    private IEnumerator PatrolMove_co()
    {
        canPatrol = false;

        ani.SetBool("Walk", true);

        rnd = Random.Range(0, wayPoints.Length);
        agent.destination = wayPoints[rnd].position;

        yield return wfs;

        canPatrol = true;
    }
    //---------------------플레이어를 아직 발견하지 못했거나 놓쳤을 때  수색 상태-------------------------

    public void SearchMove()
    {
        ani.SetBool("Walk", true);
        enemyDetect.SearchCheckOff();
        agent.destination = enemyDetect.VisibleTargetsV3();
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            enemyDetect.SearchCheckOn();
            ani.SetBool("Walk", false);
        }
    }

    //----------------------------------플레이어를 발견해 전투 상태--------------------------------------

    public void BattleMove()
    {
        LookAtTarget();

        agent.ResetPath();
        ani.SetBool("Walk", false);
        
        if (canBattle)
        {
            StartCoroutine(BattleActionLoop_co());
        }
    }
    
    private IEnumerator BattleActionLoop_co()
    {
        canBattle = false;
    
        int a = Random.Range(0, 4);
    
        switch (a)
        {
            case 0:
                yield return StartCoroutine(Shoot_co());
                break;
            case 1:
                yield return StartCoroutine(Wait_co());
                break;
            case 2:
                yield return StartCoroutine(MoveForward());
                break;
            case 3:
                yield return StartCoroutine(MoveBackward());
                break;
        }
        canBattle = true ;
    
    }
        private IEnumerator Shoot_co()
    {
        Debug.Log("총 쏘기 시작");
        ani.SetBool("Shot", true);
        // Fire();

        yield return new WaitForSeconds(1.5f);
        ani.SetBool("Shot", false);
    
        Debug.Log("총 쏘기 끝");
    }
    private IEnumerator Wait_co()
    {
        Debug.Log("대기 시작");
    
        float waitTime = Random.Range(3f, 5f);
    
        yield return new WaitForSeconds(waitTime);
    
        Debug.Log("대기 끝");
    }
    private IEnumerator MoveForward()
    {
        Debug.Log("앞으로 이동 시작");
    
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + transform.forward * moveDistance;
    
        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            yield return null;
        }

        Debug.Log("앞으로 이동 끝");
    }
    private IEnumerator MoveBackward()
    {
        Debug.Log("뒤로 이동 시작");
    
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition - transform.forward * moveDistance;
    
        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        Debug.Log("뒤로 이동 끝");
    }

    


    //----------------------------------플레이어를 발견해 전투 상태--------------------------------------
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
}

