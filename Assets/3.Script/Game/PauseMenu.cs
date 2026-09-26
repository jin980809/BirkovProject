using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;
using UnityEngine.UI;

// ESC 일시정지 메뉴 (게임으로 돌아가기 / 사운드 설정 / 메인메뉴로 나가기).
//
// 언제 열리나: ESC 로 해제할 것이 아무것도 없을 때만 열린다. ESC 는 이미 인벤토리·상자 닫기,
// 아이템 사용 취소, 상호작용 취소, 재장전 취소가 함께 쓰고 있어서, 그런 상태가 하나라도 켜져 있으면
// 그쪽이 ESC 를 쓰도록 두고 메뉴는 열지 않는다 (한 번 더 누르면 열린다).
// 메뉴가 열려 있는 동안 ESC 는 메뉴를 닫는다.
//
// 멈춤: 열려 있는 동안 Time.timeScale = 0 이라 제한시간(GameManager)·적·헐기가 모두 멈추고,
// 플레이어 조작도 SetMovementLocked 로 막는다.
//
// 세팅: 씬을 넘어 유지되는 UI(PersistentUiRoot) 안에 두고 Menu Root / 버튼 3개 / 사운드 패널을 연결한다.
// 사운드 패널도 같은 PersistentUiRoot 안에 둔 OptionUI 오브젝트를 연결하면 모든 씬에서 그대로 쓰인다.
// 플레이어는 씬마다 새로 생기므로 씬이 로드될 때마다 다시 찾는다 (ISceneRebindable).
public class PauseMenu : MonoBehaviour, ISceneRebindable
{
    [Header("UI")]
    [Tooltip("ESC 를 누르면 켜질 메뉴 오브젝트")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button mainMenuButton;

    [Tooltip("사운드 설정 패널(OptionUI 가 붙은 오브젝트). 닫기는 그쪽 닫기 버튼이 한다")]
    [SerializeField] private GameObject soundPanel;

    [Header("이동")]
    [Tooltip("메인메뉴 버튼으로 갈 씬. Build Settings 에 등록돼 있어야 한다")]
    [SerializeField] private string startSceneName = "StartScene";

    private PlayerInputHandler input;
    private PlayerController player;
    private WeaponController weapon;
    private InventoryTestBench inventoryBench;
    private ChestUI chestUI;
    private DeathPanelUI deathPanel;
    private CrosshairUI crosshair;

    private bool isShown;

    // 지난 프레임에 ESC 로 해제할 것이 있었는지. ESC 는 여러 곳이 같이 구독하고 있고 누가 먼저 받을지
    // 정해져 있지 않다 - 인벤토리 쪽이 먼저 받아 닫아버리면 이쪽에서 "지금 열린 게 없다"고 잘못 보고
    // 메뉴까지 같이 열릴 수 있다. 그래서 현재 상태가 아니라 이 스냅샷(프레임 시작 시점의 상태)으로 판단한다.
    private bool cancellableWasActive;

    public bool IsShown
    {
        get { return isShown; }
    }

    private void Awake()
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Resume);
        }

        if (soundButton != null)
        {
            soundButton.onClick.AddListener(OpenSoundPanel);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }

    private void Start()
    {
        RebindSceneReferences();
    }

    // 씬이 바뀌면 그 씬의 플레이어/인벤토리를 다시 잡고, 열려 있던 메뉴는 닫는다.
    public void RebindSceneReferences()
    {
        Unsubscribe();

        input = FindAnyObjectByType<PlayerInputHandler>();
        player = FindAnyObjectByType<PlayerController>();
        weapon = FindAnyObjectByType<WeaponController>();
        inventoryBench = (PersistentUiRoot.Find<InventoryTestBench>() ?? FindAnyObjectByType<InventoryTestBench>());
        chestUI = (PersistentUiRoot.Find<ChestUI>() ?? FindAnyObjectByType<ChestUI>(FindObjectsInactive.Include));
        deathPanel = (PersistentUiRoot.Find<DeathPanelUI>() ?? FindAnyObjectByType<DeathPanelUI>(FindObjectsInactive.Include));
        crosshair = (PersistentUiRoot.Find<CrosshairUI>() ?? FindAnyObjectByType<CrosshairUI>(FindObjectsInactive.Include));

        Subscribe();
        CloseMenu(false); // 씬을 넘어오며 멈춰 있는 상태가 남지 않게 한다

        if (mainMenuButton != null)
        {
            mainMenuButton.interactable = true; // 지난 씬에서 눌러 잠긴 것을 되돌린다
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
        CloseMenu(false); // 이 UI 가 꺼질 때 게임이 멈춘 채로 남지 않게 한다
    }

    private void Subscribe()
    {
        if (input != null)
        {
            input.CancelPressed += HandleCancel;
        }
    }

    private void Unsubscribe()
    {
        if (input != null)
        {
            input.CancelPressed -= HandleCancel;
        }
    }

    // 입력 콜백보다 뒤에 도는 LateUpdate 에서 상태를 찍어둔다. 다음 프레임의 ESC 는 이 값으로 판단한다.
    private void LateUpdate()
    {
        cancellableWasActive = IsCancellableActive();
    }

    private void HandleCancel()
    {
        if (isShown)
        {
            Resume();
        }
        else if (!cancellableWasActive && CanOpenMenu())
        {
            OpenMenu();
        }
    }

    // ESC 로 해제되는 기능이 하나라도 켜져 있으면 true. 이번 ESC 는 그쪽이 쓴다.
    private bool IsCancellableActive()
    {
        if (player != null && (player.IsUsingItem || player.IsInteracting))
        {
            return true; // 아이템 사용 / 상호작용 취소
        }

        if (weapon != null && weapon.IsReloading)
        {
            return true; // 재장전 취소
        }

        if (inventoryBench != null && inventoryBench.IsOpen)
        {
            return true; // 인벤토리 / 창고 / 상점 / 전리품 닫기
        }

        if (chestUI != null && chestUI.IsOpen)
        {
            return true;
        }

        return false;
    }

    // ESC 를 쓰는 다른 기능이 하나도 켜져 있지 않을 때만 true.
    // (그런 기능이 켜져 있으면 이번 ESC 는 그쪽이 쓰고, 다음 ESC 에 메뉴가 열린다)
    private bool CanOpenMenu()
    {
        if (GameSession.Instance.IsLoading)
        {
            return false; // 씬 전환 중
        }

        if (deathPanel != null && deathPanel.IsShown)
        {
            return false; // 사망 패널이 떠 있으면 그쪽 버튼으로만 나간다
        }

        if (player == null || player.IsDead)
        {
            return false;
        }

        // 지금 이 순간에도 해제할 것이 켜져 있으면 열지 않는다 (스냅샷과 함께 이중으로 막는다)
        return !IsCancellableActive();
    }

    private void OpenMenu()
    {
        isShown = true;

        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
        }

        HideSoundPanel(); // 지난번에 열어둔 사운드 설정이 같이 뜨지 않게 한다

        Time.timeScale = 0f;

        if (player != null)
        {
            player.SetMovementLocked(true);
        }

        SetCrosshairVisible(false);
        Cursor.visible = true;
    }

    private void Resume()
    {
        CloseMenu(true);
    }

    // restoreControl: 플레이어 조작을 다시 풀어줄지. 씬이 바뀌는 경우에는 그 씬의 플레이어가
    // 따로 있으므로 이전 플레이어를 건드리지 않는다.
    private void CloseMenu(bool restoreControl)
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        HideSoundPanel();

        Time.timeScale = 1f;

        // 메뉴가 떠 있는 동안에도 Tab 으로 인벤토리를 열 수 있다 (입력은 timeScale 과 무관하게 들어온다).
        // 그 상태로 메뉴만 닫으면서 잠금을 풀고 크로스헤어를 켜면, 인벤토리가 열려 있는데 커서가 사라져
        // 아이템을 클릭할 수 없게 된다. 그래서 "지금 UI 가 열려 있는지"를 보고 되돌린다.
        bool uiStillOpen = IsCancellableActive();

        if (restoreControl && player != null)
        {
            player.SetMovementLocked(uiStillOpen);
        }

        if (isShown && restoreControl)
        {
            SetCrosshairVisible(!uiStillOpen);
        }

        isShown = false;
    }

    private void OpenSoundPanel()
    {
        if (soundPanel != null)
        {
            soundPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("PauseMenu: Sound Panel 이 연결되지 않아 사운드 설정을 열 수 없습니다.", this);
        }
    }

    private void HideSoundPanel()
    {
        if (soundPanel != null)
        {
            soundPanel.SetActive(false);
        }
    }

    private void GoToMainMenu()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.interactable = false; // 여러 번 눌러도 한 번만 넘어간다
        }

        // 씬을 넘기기 전에 반드시 시간을 돌려놓는다 - 멈춘 상태로 넘어가면 페이드도 로딩도 돌지 않는다
        CloseMenu(true);

        // 로비에서 나가는 경우에는 저장한다 (창고 정리한 것이 날아가지 않게).
        // 전투 중이면 SaveNow 가 스스로 건너뛴다 - 그 판의 획득물은 귀환해야 남는다.
        SaveCoordinator.Instance.SaveNow();

        GameSession.Instance.LoadScene(startSceneName);
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (crosshair != null)
        {
            crosshair.SetCrosshairActive(visible);
        }
    }
}
