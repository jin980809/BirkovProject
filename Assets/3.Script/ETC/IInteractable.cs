using UnityEngine;

// 상호작용 가능한 대상(상자, 적의 전리품 등)이 구현한다.
// IHearing/IDamageable 과 같은 방식: 플레이어 쪽은 이 인터페이스만 알고, 실제 구현(아이템 지급 등)은 각자 담당한다.
public interface IInteractable
{
    // 상호작용에 걸리는 시간(초)
    float InteractDuration { get; }

    // 지금 상호작용을 시작할 수 있는지 (이미 열림/잠김 등)
    bool CanInteract(GameObject interactor);

    // 상호작용 시간이 다 찼을 때 호출된다
    void OnInteractComplete(GameObject interactor);

    // 이 대상이 상호작용 가능 범위+시야각 안에 들어와 "현재 대상"이 됐을 때 호출된다.
    // 보통 자기 자신의 자식으로 미리 배치해 둔 UI(예: World Space 캔버스 프롬프트 이미지)를 켠다.
    void ShowPrompt();

    // 이 대상이 더 이상 "현재 대상"이 아니게 됐을 때(범위/시야각을 벗어나거나, 상호작용을 시작하거나) 호출된다.
    void HidePrompt();
}
