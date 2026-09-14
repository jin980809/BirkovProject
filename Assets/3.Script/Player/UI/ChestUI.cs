using UnityEngine;

// 상자를 열었을 때 뜨는 UI. 지금은 열림/닫힘만 하는 빈 패널이다.
// 아이템 목록 표시, 실제 인벤토리 연동(꺼내기/넣기 등)은 나중에 인벤토리 쪽에서 붙인다.
//
// 상호작용이 완료되면 그 대상(IInteractable 구현체)이 직접 Open() 을 호출한다
// (상자류만 여는 UI 라서 PlayerInteraction 이 알 필요는 없다 - 예: DebugInteractable 참고).
// ESC 는 이 스크립트가 PlayerInputHandler.CancelPressed 를 직접 구독해서 처리한다
// (Player 쪽은 ChestUI 가 있는지 몰라도 된다 - 열려 있을 때만 닫는다).
// 열려 있는 동안에는 PlayerController.SetMovementLocked 로 이동/회전/사격을 막는다
// (WeaponController.SetArmed 와 같은 방식의 push 방식 - PlayerController 는 ChestUI 존재를 몰라도 됨).
// 상자를 열 때 플레이어 인벤토리(InventoryUI)도 같이 연다 - 룻팅 게임에서 상자+내 인벤토리를 같이 보여주는 것.
public class ChestUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [Tooltip("상자를 열 때 같이 열 플레이어 인벤토리 UI (비워두면 상자만 연다)")]
    [SerializeField] private InventoryUI inventoryUI;

    private PlayerInputHandler input;
    private PlayerController player;

    public bool IsOpen
    {
        get { return panelRoot != null && panelRoot.activeSelf; }
    }

    private void Awake()
    {
        input = FindAnyObjectByType<PlayerInputHandler>();
        player = FindAnyObjectByType<PlayerController>();

        if (inventoryUI == null)
        {
            inventoryUI = FindAnyObjectByType<InventoryUI>();
        }

        Close();
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.CancelPressed += HandleCancel;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.CancelPressed -= HandleCancel;
        }
    }

    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (inventoryUI != null)
        {
            inventoryUI.Open();
        }

        if (player != null)
        {
            player.SetMovementLocked(true);
        }
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (inventoryUI != null)
        {
            inventoryUI.Close();
        }

        if (player != null)
        {
            player.SetMovementLocked(false);
        }
    }

    private void HandleCancel()
    {
        if (IsOpen)
        {
            Close();
        }
    }
}
