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

    private Vector3 respawnPoint;
    private int rnd;

    private bool canRevert = true;
    private bool canPatrol = true;
    private bool canBattle = true;
    private WaitForSeconds wfs;

    [Header("순찰 정보")]
    [SerializeField] private Transform[] wayPoints;
    [SerializeField] private float patrolWaitingTime;

    [Header("속도")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float angleSpeed;

    private float moveDistance = 3f;

    private void Awake()
    {
        //적 데이타 캐싱---------------------------------------
        moveSpeed = enemyData.moveSpeed;
        angleSpeed = enemyData.angleSpeed;
        //-----------------------------------------------------
        TryGetComponent(out agent);
        TryGetComponent(out ani);
        TryGetComponent(out enemyDetect);

    }

    private void Start()
    {
        agent.speed = moveSpeed;
        agent.angularSpeed = angleSpeed;
        patrolWaitingTime = enemyData.patrolWaitingTime;
        wfs = new WaitForSeconds(patrolWaitingTime);

        respawnPoint = transform.position;
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
        agent.isStopped = false;
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
        //agent.ResetPath();
        LookAtTarget();

        if (canBattle)
        {
            StartCoroutine(BattleActionLoop_co());
        }
    }
    
    private IEnumerator BattleActionLoop_co()
    {
        canBattle = false;
    
        int a = Random.Range(0, 2);
        
        switch (a)
        {
            case 0:
                yield return StartCoroutine(Patten1_co());
                break;
            case 1:
                yield return StartCoroutine(Patten2_co());
                break;
        }
        canBattle = true ;
    
    }
    private IEnumerator Patten1_co()
    {
        ani.SetBool("Walk", true);
        agent.isStopped = false;

        agent.destination = enemyDetect.VisibleTargets().position;
        
        yield return new WaitForSeconds(1.5f);

        agent.isStopped = true;

        ani.SetBool("Walk", false);

        ani.SetBool("Shot", true);
        yield return new WaitForSeconds(1.0f);
        ani.SetBool("Shot", false);
    }
    private IEnumerator Patten2_co()
    {

        yield return new WaitForSeconds(1.5f);

        ani.SetBool("Shot", true);
        yield return new WaitForSeconds(1.0f);
        ani.SetBool("Shot", false);


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

