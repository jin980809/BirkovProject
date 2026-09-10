using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleMove : MonoBehaviour
{
    [Header("카메라 설정")]
    public float moveSpeed = 10f;
    public float rotSpeed = 720f;
    public float jumpForce = 5f;

    [Header("카메라 설정")]
    public Vector3 camOffset = new Vector3(0f, 10f, -5f);
    public float camSpeed = 5f;

    private Rigidbody r;
    private Transform maincamT;

    private void Start()
    {
        r = GetComponent <Rigidbody>();
        if (Camera.main != null)  // 씬에 있는 메인 카메라를 자동으로 찾아 가져옵니다.
        {
            maincamT = Camera.main.transform;
        }
    }
    private void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 moveD = new Vector3(moveX, 0f, moveZ);
        transform.Translate(moveD * moveSpeed * Time.deltaTime);
        Spin();
    }
    private void LateUpdate()
    {
        if (maincamT != null)
        {
            // 카메라가 가야 할 목표 위치 계산
            Vector3 desiredPosition = transform.position + camOffset;

            // 현재 카메라 위치에서 목표 위치로 부드럽게 이동
            Vector3 smoothedPosition = Vector3.Lerp(maincamT.position, desiredPosition, camSpeed * Time.deltaTime);

            // 메인 카메라 위치 업데이트
            maincamT.position = smoothedPosition;
        }
    }
    private void Spin()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Vector3 tPos = new Vector3(hit.point.x, transform.position.y, hit.point.z);
            Vector3 d = tPos - transform.position;
            if (d != Vector3.zero)
            {
                Quaternion tRo = Quaternion.LookRotation(d);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, tRo, rotSpeed * Time.deltaTime);
            }
        }
    }
}
