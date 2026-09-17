using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

// Tab 키로 플레이어 자신의 인벤토리(가방/장비)를 단독으로 열고 닫는다.
// ESC 는 지금 열려있는 NaYeongMin UI 를 전부 닫는다 - Tab 으로 연 단독 인벤토리든,
// WorldContainerInteractable 로 연 상자/전리품/맵배치상자든 상관없다. InventoryTestBench.CloseCurrent()
// 가 화면 전체(가방 + 외부 패널)를 한 번에 닫아주기 때문에 여기 하나로 다 처리된다.
//
// 이동/회전 잠금 + 크로스헤어 전환도 여기 한 곳에서 관리한다: 뭐가 열든(Tab 이든 상자든)
// NaYeongMin 화면이 열려있는 동안은 이동을 잠그고 크로스헤어 대신 OS 커서를 보여준다 - 열림/닫힘을
// Update() 에서 폴링으로 감지해서 처리하므로, 어떤 경로로 열렸든(Tab, WorldContainerInteractable)
// 여기 하나로 다 처리된다 (WorldContainerInteractable 은 그래서 잠금을 직접 걸지 않는다).
[RequireComponent(typeof(PlayerController))]
public class PlayerInventoryToggle : MonoBehaviour
{
    [SerializeField] private InventoryTestBench inventoryBench;
    [SerializeField] private CrosshairUI crosshair;

    private PlayerController player;
    private PlayerInputHandler input;
    private bool wasOpen;
    private bool didInitialFixup;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        TryGetComponent(out input);

        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }

        if (crosshair == null)
        {
            crosshair = FindAnyObjectByType<CrosshairUI>();
        }
    }

    private void OnEnable()
    {
        if (input == null)
        {
            return;
        }

        input.InventoryToggled += HandleToggle;
        input.CancelPressed += HandleCancel;
    }

    private void OnDisable()
    {
        if (input == null)
        {
            return;
        }

        input.InventoryToggled -= HandleToggle;
        input.CancelPressed -= HandleCancel;
    }

    private void Update()
    {
        if (!didInitialFixup)
        {
            didInitialFixup = true;
            RunInitialFixup();
        }

        // 열리고 닫히는 순간을 여기서 한 번에 감지한다 - 어떤 경로로 열렸든(Tab, 상자 등) 상관없다.
        bool isOpen = inventoryBench != null && inventoryBench.IsOpen;
        if (isOpen != wasOpen)
        {
            player.SetMovementLocked(isOpen);

            if (crosshair != null)
            {
                crosshair.SetCrosshairActive(!isOpen); // 열리면 크로스헤어 끄고 OS 커서 보이게
            }
        }

        wasOpen = isOpen;
    }

    // InventoryTestBench 파일은 못 고치니, 걔가 절차적으로 만들어 둔 UI를 게임이 시작된 직후
    // (모든 Start() 가 끝난 뒤의 첫 Update()) 런타임에 손봐서 우리가 원하는 동작으로 맞춘다.
    private void RunInitialFixup()
    {
        if (inventoryBench == null)
        {
            return;
        }

        // 하단 퀵슬롯(QuickPanel)은 인벤토리 화면("Root")의 자식으로 만들어져 있어서, 화면을
        // 껐다 켰다 하면 같이 꺼졌다 켜진다. 인벤토리처럼 토글되지 않고 항상 보이게 하려면
        // Root 밖(캔버스 바로 밑)으로 옮겨야 한다. 하이어라키 경로가 그대로일 때만 동작하니,
        // NaYeongMin 쪽에서 BuildUi() 의 이름/구조를 바꾸면 이 Find 경로도 다시 확인해야 한다.
        Transform quickPanel = inventoryBench.transform.Find("Root/QuickPanel");
        if (quickPanel != null)
        {
            quickPanel.SetParent(inventoryBench.transform, false);

            // 형제 순서상 맨 앞(0번)으로 보내야 나중에 오는 다른 UI(드래그 아이콘 등)보다 먼저
            // 그려져서 뒤에 깔린다 - 안 그러면(맨 뒤로 가면) 인벤토리에서 아이템을 퀵슬롯으로
            // 드래그할 때 드래그 아이콘이 퀵슬롯 패널에 가려져 안 보인다.
            quickPanel.SetAsFirstSibling();

            quickPanel.gameObject.SetActive(true);
        }

        // InventoryTestBench.Start() 가 편의상 끝에 OpenInventory() 를 무조건 호출해서 게임
        // 시작하자마자 인벤토리가 열려 보인다. 모든 오브젝트의 Start() 는 이 첫 Update() 호출
        // 전에 이미 다 끝나 있으므로, 여기서 강제로 닫으면 화면에 보이기 전에(첫 프레임 렌더 전에)
        // 닫혀서 깜빡임 없이 처리된다. (QuickPanel 은 이미 밖으로 옮겨서 이 닫기의 영향을 안 받는다.)
        if (inventoryBench.IsOpen)
        {
            inventoryBench.CloseCurrent();
        }
    }

    private void HandleToggle()
    {
        // 아이템 사용/재장전 중에는 인벤토리를 열 수 없다 (다 쓰거나 ESC 로 취소한 뒤에)
        if (inventoryBench != null && (player == null || (!player.IsUsingItem && !player.IsReloading)))
        {
            inventoryBench.ToggleInventory(); // 잠금/크로스헤어 전환은 Update() 폴링이 알아서 처리한다
        }
    }

    private void HandleCancel()
    {
        if (inventoryBench != null && inventoryBench.IsOpen)
        {
            inventoryBench.CloseCurrent();
        }
    }
}
