using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Scriptable/EnemyData", fileName = "EnemyData")]

public class EnemyData : ScriptableObject
{
    [Header("Ã¼·Â")]
    public float maxHealth = 100f;

    [Header("¼Óµµ")]
    public float moveSpeed = 3f;
    public float angleSpeed = 360f;

    [Header("½Ã¾ß")]
    public float rayDistance = 10f;
    [Range(0f, 360f)]
    public float viewAngle = 90f;

    [Header("ÃÖ¼Ò °Å¸®")]
    public float minDistance = 5f;

    [Header("Raycast ¼³Á¤")]
    public int rayCount = 5;
    public float detectInterval = 0.2f;

    [Header("°¨Áöµµ")]
    public float decrease = 10f;
    public float walkPoints = 30f;
    public float runPoints = 40f;
    public float shotPoints = 50f;

    [Header("¼øÂû Äð")]
    public float patrolWaitingTime = 10;

    [Header("Çï¸ä ·¹º§ È®·ü(0~99)")]
    [SerializeField] public int oneHelmet = 25;
    [SerializeField] public int twoHelmet = 50;
    [SerializeField] public int threeHelmet = 75;

    [Header("°©¿Ê ·¹º§ È®·ü(0~99)")]
    [SerializeField] public int oneArmor = 25;
    [SerializeField] public int twoArmor = 50;
    [SerializeField] public int threeArmor = 75;
}
