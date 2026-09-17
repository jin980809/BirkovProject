using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyData enemyData;

    [Header("정보")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;

    private void Start()
    {
        maxHealth = enemyData.maxHealth;
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log(gameObject.name + " 이(가) " + amount + " 대미지를 입었습니다.", this);
        if (currentHealth < 0)
        {
            Destroy(gameObject);
        }
    }

}
