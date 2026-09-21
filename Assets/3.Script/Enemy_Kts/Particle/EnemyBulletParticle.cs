using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletParticle : MonoBehaviour
{
    [SerializeField] private EnemyBulletPool enemyBulletPool;
    [SerializeField] private ParticleSystem particleSystem;
    private void Start()
    {
        enemyBulletPool = FindAnyObjectByType<EnemyBulletPool>();
        particleSystem = GetComponentInChildren<ParticleSystem>();
    }

    private void Update()
    {
        if (enemyBulletPool != null)
        {
            if (gameObject.activeSelf)
            {
                if (!particleSystem.isPlaying)
                {
                    enemyBulletPool.ReturnRicocheParticle(gameObject);
                }
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
        
    }

}
