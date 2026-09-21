using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float time;
    [SerializeField] private float curTime;

    int min;
    int sec;

    private void Awake()
    {
        time = 720;
        StartCoroutine(StartTimer());
    }
    IEnumerator StartTimer()
    {
        curTime = time;
        while (curTime > 0)
        {
            curTime -= Time.deltaTime;
            min = (int)curTime / 60;
            sec = (int)curTime % 60;
            text.text = min.ToString("00") + ":" + sec.ToString("00");
            yield return null;

            if (curTime <= 0)
            {
                Debug.Log("е╦юс╬ф©Т!");
                curTime = 0;
                yield break;
            }
        }
    }
}
