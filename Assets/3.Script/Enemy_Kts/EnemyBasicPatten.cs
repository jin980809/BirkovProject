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
    private bool canPatrol;
    private WaitForSeconds wfs;

    [Header("추적할 대상 레이어")]
    [SerializeField] private LayerMask targetLayer;

    [Header("순찰 정보")]
    [SerializeField] private Transform[] wayPoints;
    [SerializeField] private float patrolWaitingTime;


    private void Awake()
    {
        TryGetComponent(out agent);
        TryGetComponent(out ani);
        TryGetComponent(out enemyDetect);

    }

    private void Start()
    {
        respawnPoint = transform.position;

        wfs = new WaitForSeconds(patrolWaitingTime);
        canPatrol = true;

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
            agent.destination = enemyDetect.VisibleTargets().position;
        }
        else if (enemyDetect.enemyState.Equals(EnemyDetect.EnemyState.Search))
        {
            agent.destination = enemyDetect.VisibleTargetsV3();
        }
        else if (enemyDetect.enemyState.Equals(EnemyDetect.EnemyState.Revert))
        {
            agent.destination = respawnPoint;
        }
    }

    //------------------------------플레이어를 발견하지 못한 상태인 순찰 상태----------------------------------
    private void PatrolMove()
    {
        if (canPatrol && !wayPoints.Length.Equals(0))
        {
            StartCoroutine(PatrolMove_co());
            Debug.Log("2");
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            ani.SetBool("Walk", false);
        }
    }

    private IEnumerator PatrolMove_co()
    {
        canPatrol = false;
        Debug.Log("3");
        ani.SetBool("Walk", true);

        rnd = Random.Range(0, wayPoints.Length);
        agent.destination = wayPoints[rnd].position;

        yield return wfs;

        canPatrol = true;
    }
    //---------------------플레이어를 아직 발견하지 못했거나 놓쳤을 때  수색 상태-------------------------












    //----------------------------------플레이어를 발견해 전투 상태--------------------------------------
}
