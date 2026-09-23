using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyArmor : MonoBehaviour
{
    [Header("Çï¸ä")]
    [SerializeField] private EnemyArmorsData helmet;
    [SerializeField] private EnemyArmorsData[] helmetData;
    [Header("Çï¸ä À§Ä¡")]
    [SerializeField] private GameObject helmetPosition;
    [Header("Çï¸ä È®·ü(0~99)")]
    [SerializeField] private int oneHelmet = 25;
    [SerializeField] private int twoHelmet = 50;
    [SerializeField] private int threeHelmet = 75;

    [Header("°©¿Ê")]
    [SerializeField] private EnemyArmorsData armor;
    [SerializeField] private EnemyArmorsData[] armorData;
    [Header("°©¿Ê À§Ä¡")]
    [SerializeField] private GameObject armorPosition;
    [Header("°©¿Ê È®·ü(0~99)")]
    [SerializeField] private int oneArmor = 25;
    [SerializeField] private int twoArmor = 50;
    [SerializeField] private int threeArmor = 75;

    [Header("¹æ¾î·Â")]
    [SerializeField] private float helmetDefense = 0;
    [SerializeField] private float armorDefense = 0;
    
    public float declineRate { get; private set; }

    [SerializeField] int rndHelmet;
    [SerializeField] int rndArmor;

    private void OnEnable()
    {
        RandomHelmet();
        RandomArmor();
        DefenseCalculation();
    }

    private void RandomHelmet()
    {
        rndHelmet = Random.Range(0, 100);

        if(rndHelmet > threeHelmet)
        {
            helmet = helmetData[2];
        }
        else if (rndHelmet > twoHelmet)
        {
            helmet = helmetData[1];
        }
        else if (rndHelmet > oneHelmet)
        {
            helmet = helmetData[0];
        }
        else
        {
            return;
        }

        Instantiate(helmet.prefab, helmetPosition.transform);
        helmetDefense = helmet.defense;
    }
    private void RandomArmor()
    {
        rndArmor = Random.Range(0, 100);

        if (rndArmor > threeArmor)
        {
            armor = armorData[2];
        }
        else if (rndArmor > twoArmor)
        {
            armor = armorData[1];
        }
        else if (rndArmor > oneArmor)
        {
            armor = armorData[0];
        }
        else
        {
            return;
        }

        Instantiate(armor.prefab, armorPosition.transform);
        armorDefense = armor.defense;
    }

    private void DefenseCalculation()
    {
        declineRate = (1 - helmetDefense / 100) * (1 - armorDefense / 100);
    }
}



