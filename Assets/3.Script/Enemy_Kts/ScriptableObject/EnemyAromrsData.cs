using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Scriptable/EnemyArmorsData", fileName = "EnemyArmorData")]
public class EnemyArmorsData : ScriptableObject
{
    [Header("프리펩")]
    public GameObject prefab;

    [Header("방어력")]
    public float defense = 10f;
}

