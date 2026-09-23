using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleCameraIntro : MonoBehaviour
{
    //타이틀 화면 배경용 카메라 인트로 패닝입니다. 웨이포인트를 순서대로 등속 이동하며 루프하는 방식입니다

    [Header("이동 순서대로 배치 : \n왼쪽 아래 구석 - 오른쪽 위 구석 - \n 왼쪽 위 구석 - 오른쪽 아래 구석")]
    [SerializeField] private Transform[] waypoints;

    [Header("한 바퀴 loop로 할 때 전체 이동 시간(초)")]
    [SerializeField] private float totalDuration = 40f;

    private float moveSpeed;
    private float cam;  //Y값 고정 카메라

    private void Start()
    {
        cam = transform.position.y;

        float totalLength = 0f;
        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 current = waypoints[i].position;
            Vector3 next = waypoints[(i + 1) % waypoints.Length].position;
            totalLength += Vector3.Distance(current, next);
        }
        moveSpeed = totalLength / totalDuration;
        SetPos(waypoints[0].position);
        StartCoroutine(Loop());
    }
    private System.Collections.IEnumerator Loop()
    {
        int index = 0;
        while (true)
        {
            Vector3 start = waypoints[index].position;
            Vector3 target = waypoints[(index + 1) % waypoints.Length].position;
            float distance = Vector3.Distance(start, target);
            if (distance > 0.0001f)
            {
                float duration = distance / moveSpeed;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    SetPos(Vector3.Lerp(start, target, t));
                    yield return null;
                }
                SetPos(target); //오차 보정용 코드 
            }
            index = (index + 1) % waypoints.Length;
            yield return null;
        }
    }

    private void SetPos(Vector3 worldPos)
    {
        transform.position = new Vector3(worldPos.x, cam, worldPos.z);
    }
}
