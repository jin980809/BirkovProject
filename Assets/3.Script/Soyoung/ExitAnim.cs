using System.Collections;
using UnityEngine;

public class ExitAnim : MonoBehaviour
{
    [Header("움직일 오브제")]
    public Transform target;

    [Header("오브제가 닫힐 때랑 열릴 때 위치할 좌표")]
    public Vector3 closedPos;
    public Vector3 openPos;

    [Header("애니메이션 설정")]
    public float moveDuration = 1.0f;

    private Coroutine moveCoroutine;

    private void Start()
    {
        if (target == null)
        {
            target = transform;
        }
            target.localPosition = closedPos;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        MoveTo(openPos);
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        MoveTo(closedPos);
    }

    private void MoveTo(Vector3 targetPos)
    {
        if (moveCoroutine != null)
         {
            StopCoroutine(moveCoroutine);
         }
        moveCoroutine = StartCoroutine(MoveCover(targetPos));
    }
    private IEnumerator MoveCover(Vector3 targetPos)
    {
        Vector3 startPos = target.localPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            t = t * t * (3f - 2f * t);  //smooth step 이라고 클로드가 알려줌
            target.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        target.localPosition = targetPos;
        moveCoroutine = null;
    }
}
