using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletParticle : MonoBehaviour
{
    [SerializeField] private EnemyBulletPool enemyBulletPool;
    [SerializeField] private ParticleSystem particleSystem;
    private void OnEnable()
    {
        enemyBulletPool = FindAnyObjectByType<EnemyBulletPool>();
        particleSystem = GetComponentInChildren<ParticleSystem>();
    }

    private void Update()
    {
        //if (enemyBulletPool != null)
        //{
        //    if (!particleSystem.isPlaying)
        //    {
        //        enemyBulletPool.ParticleReturn(gameObject);
        //    }
        //}
        //else
        //{
        //    gameObject.SetActive(false);
        //}
        
    }

}
