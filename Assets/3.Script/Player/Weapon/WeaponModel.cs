using UnityEngine;

// 총 모델 프리팹 루트에 붙인다. 이 모델에서 총알이 나갈 위치(총구)를 인스펙터에서 직접 연결해 둔다.
// WeaponVisual 이 모델을 스폰한 뒤 이 컴포넌트를 읽어 WeaponController 에 발사 위치로 넘겨준다.
public class WeaponModel : MonoBehaviour
{
    [Tooltip("총알이 나갈 위치. 프리팹 안의 총구 쪽 빈 오브젝트를 연결한다")]
    [SerializeField] private Transform firePoint;

    public Transform FirePoint
    {
        get { return firePoint; }
    }
}
