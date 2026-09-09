using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Search,
        Battle,
        Die
    }
    public EnemyState enemyState { get; private set; }

    private Animator ani;
    private NavMeshAgent agent;

    
    private Vector3 respawnPoint;
    private int rnd;
    private bool canPatrol;
    private WaitForSeconds wfs;

    [Header("추적할 대상 레이어")]
    [SerializeField] private LayerMask targetLayer;

    [Header("순찰 정보")]
    [SerializeField] private Transform[] wayPoints;
    [SerializeField] private float patrolTime;


    private void Awake()
    {
        TryGetComponent(out agent);
        TryGetComponent(out ani);
    }

    private void Start()
    {
        enemyState = EnemyState.Patrol;
        respawnPoint = transform.position;

        wfs = new WaitForSeconds(patrolTime);
        canPatrol = true;
    }

    private void Update()
    {
        if(canPatrol && !wayPoints.Length.Equals(0))
        {
            StartCoroutine(PatrolMove_co());
            
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            ani.SetBool("Walk", false);
        }
    }

    //------------------------------플레이어를 발견하지 못한 상태인 순찰 상태----------------------------------
    
    private IEnumerator PatrolMove_co()
    {
        canPatrol = false;

        ani.SetBool("Walk", true);

        rnd = Random.Range(0, wayPoints.Length);
        agent.destination = wayPoints[rnd].position;

        yield return wfs;

        canPatrol = true;
    }
        


}
