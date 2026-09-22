using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Scriptable/EnemyShotData", fileName = "EnemyShotData")]

public class EnemyShotData : ScriptableObject
{
    [Header("공격력")]
    public float damage = 10f;

    [Header("연발")]
    public int minBulletsPerShot = 1;
    public int maxBulletsPerShot = 1;

    [Header("동시 발사")]
    public int minBulletsPerBurst = 1;
    public int maxBulletsPerBurst = 1;

    [Header("탄퍼짐 설정")]
    public float spreadAngle = 5.0f;

    [Header("발사 설정")]
    public float fireInterval = 0.1f;

    [Header("탄창")]
    public int magazineSize = 30;

    [Header("재장전")]
    public float reloadTime = 2.0f;
}
