using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Scriptable/EnemyShotData", fileName = "EnemyShotData")]

public class EnemyShotData : ScriptableObject
{
    [Header("총알 설정")]
    [SerializeField] public float damage = 10f;

    [Header("연발")]
    [SerializeField] public int minBulletsPerShot = 1;
    [SerializeField] public int maxBulletsPerShot = 1;

    [Header("동시 발사")]
    [SerializeField] public int minBulletsPerBurst = 1;
    [SerializeField] public int maxBulletsPerBurst = 1;

    [Header("탄퍼짐 설정")]
    [SerializeField] public float spreadAngle = 5.0f;

    [Header("발사 설정")]
    [SerializeField] public float fireInterval = 0.1f;

    [Header("탄창")]
    [SerializeField] public int magazineSize = 30;

    [Header("재장전")]
    [SerializeField] public float reloadTime = 2.0f;
}
