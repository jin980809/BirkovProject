using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Scriptable/EnemyData", fileName = "EnemyData")]

public class EnemyData : ScriptableObject
{
    [Header("체력")]
    public float maxHealth = 100f;

    [Header("데미지")]
    public float damage = 20f; //애매함 장비에 따라 달라질듯

    [Header("속도")]
    public float moveSpeed = 3f;
    public float angleSpeed = 360f;

    [Header("시야")]
    public float rayDistance = 10f;
    [Range(0f, 360f)]
    public float viewAngle = 90f;

    [Header("Raycast 설정")]
    public int rayCount = 5;
    public float detectInterval = 0.2f;

    [Header("감지도")]
    public float decrease = 10f;
    public float walkPoints = 30f;
    public float runPoints = 40f;
    public float shotPoints = 50f;

    [Header("순찰 쿨")]
    public float patrolWaitingTime = 10;
}
