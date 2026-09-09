using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Search,
        Battle,
        Die
    }

    [Header("추적할 대상 레이어")]
    public LayerMask Target_Layer;

    public EnemyState enemyState { get; private set; }
    private Vector3 respawnPoint;


    private void Start()
    {
        enemyState = EnemyState.Patrol;
        respawnPoint = transform.position;
    }

    private void Update()
    {
        
    }

    //------------------------------플레이어를 발견하지 못한 상태인 순찰 상태----------------------------------
    
    //private IEnumerator PatrolMove_co()
    //{
    //    
    //}
        


}
