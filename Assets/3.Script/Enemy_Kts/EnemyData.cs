using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Scriptable/ZombieData", fileName = "EnemyData")]

public class EnemyData : MonoBehaviour
{
    //[Header("기본정보")]
    //public string enemyName;
    //public int enemyID;

    [Header("시야")]
    [Range(0f, 360f)]
    public float fieldOfView = 90f;
    public float viewDistance = 10f;

    [Header("청각")]
    public float hearingRange = 10f;
    public LayerMask hearingLayer;

    [Header("최대 소리 갯수")]
    public int hearingRaycastMaxHits = 10;
}
