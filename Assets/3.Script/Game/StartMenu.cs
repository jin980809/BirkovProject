using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// 시작 씬의 버튼들을 담당한다.
//  - 새로하기: 저장 파일(인벤토리/창고/돈 + 사망 분실물 기록)을 지우고 로비로 간다. 아무것도 없는 상태로 시작한다.
//  - 이어하기: 저장 파일을 그대로 두고 로비로 간다. 실제 불러오기는 SaveCoordinator 가 로비에서 한 번만 한다.
//              저장 파일이 없으면 이 버튼은 눌리지 않는다.
//  - 사운드 설정: 인스펙터에 넣어둔 오브젝트(OptionUI 가 붙은 Option)를 켠다. 닫기는 그쪽 닫기 버튼이 한다.
//  - 가이드: 인스펙터에 넣어둔 가이드 패널을 켠다. 켜는 것만 하고, 닫기는 그 패널 쪽에서 한다.
//  - 종료: 게임을 끝낸다. 에디터에서는 플레이 모드를 멈춘다.
//
// 씬 전환은 GameSession 이 담당한다 (페이드 아웃 → 로딩 씬 → 로비 → 페이드 인).
// 사운드 설정값은 PlayerPrefs 에 따로 저장되므로 새로하기로 지우지 않는다.
//
// 세팅: 시작 씬의 StartUI(또는 아무 오브젝트)에 붙이고 버튼과 패널 오브젝트를 연결한다.
// 버튼의 OnClick 은 비워둬도 된다 - 여기서 코드로 연결한다. 안 쓰는 칸은 비워두면 그 버튼만 동작하지 않는다.
public class StartMenu : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button guideButton;
    [SerializeField] private Button quitButton;

    [Header("패널 (버튼을 누르면 켜질 오브젝트. 시작할 때는 꺼 둔다)")]
    [Tooltip("사운드 설정 패널 - OptionUI 가 붙은 Option")]
    [SerializeField] private GameObject soundPanel;
    [Tooltip("가이드 패널")]
    [SerializeField] private GameObject guidePanel;

    [Header("이동")]
    [Tooltip("새로하기/이어하기로 갈 씬. Build Settings 에 등록돼 있어야 한다")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    // 저장 파일 위치 - 인벤토리 벤치(JsonSaveSystem)와 분실물 기록(DeathBoxStorage)이 쓰는 폴더/이름과 같아야 한다
    private const string SaveFolderName = "NaYeongMinTestBench";
    private const string InventorySlotName = "testbench";
    private const string DeathBoxFileName = "deathbox.json";

    private string SaveFolder
    {
        get { return Path.Combine(Application.persistentDataPath, SaveFolderName); }
    }

    private string InventorySavePath
    {
        get { return Path.Combine(SaveFolder, InventorySlotName + ".json"); }
    }

    private void Awake()
    {
        HidePanel(soundPanel);
        HidePanel(guidePanel);
    }

    private void OnEnable()
    {
        AddListener(newGameButton, StartNewGame);
        AddListener(continueButton, ContinueGame);
        AddListener(soundButton, OpenSoundPanel);
        AddListener(guideButton, OpenGuidePanel);
        AddListener(quitButton, QuitGame);

        RefreshContinueButton();
    }

    private void OnDisable()
    {
        // 인스펙터에서 연결한 OnClick 은 그대로 남는다 - 여기서 코드로 추가한 것만 지운다
        RemoveListeners(newGameButton);
        RemoveListeners(continueButton);
        RemoveListeners(soundButton);
        RemoveListeners(guideButton);
        RemoveListeners(quitButton);
    }

    // 저장 파일이 없으면 이어하기를 누를 수 없게 한다
    private void RefreshContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.interactable = HasSaveFile();
        }
    }

    private bool HasSaveFile()
    {
        bool exists = false;

        try
        {
            exists = File.Exists(InventorySavePath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("StartMenu: 저장 파일 확인 실패 - " + exception.Message, this);
        }

        return exists;
    }

    private void StartNewGame()
    {
        DeleteSaveFiles();

        // 저장 파일만 지워서는 부족하다. ESC 메뉴로 시작 화면에 돌아온 경우에는 인벤토리 데이터를 들고 있는
        // UI 캔버스가 씬을 넘어 그대로 살아 있어서, 지운 파일과 무관하게 이전 아이템이 따라간다.
        // 그 캔버스를 버려서 로비가 새 데이터로 시작하게 한다.
        PersistentUiRoot.Discard();

        GoToLobby();
    }

    private void ContinueGame()
    {
        GoToLobby();
    }

    // 인벤토리 저장(본체 + 백업 + 임시)과 사망 분실물 기록을 모두 지운다.
    // 사운드 설정(PlayerPrefs)은 건드리지 않는다.
    private void DeleteSaveFiles()
    {
        DeleteFile(InventorySavePath);
        DeleteFile(Path.Combine(SaveFolder, InventorySlotName + ".backup.json"));
        DeleteFile(Path.Combine(SaveFolder, InventorySlotName + ".tmp"));
        DeleteFile(Path.Combine(SaveFolder, DeathBoxFileName));
        DeleteFile(Path.Combine(SaveFolder, DeathBoxFileName + ".tmp"));
    }

    private void DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("StartMenu: 저장 파일 삭제 실패 (" + path + ") - " + exception.Message, this);
        }
    }

    private void GoToLobby()
    {
        SetButtonsInteractable(false); // 전환 중에 다른 버튼을 또 누르지 못하게 한다
        GameSession.Instance.LoadScene(lobbySceneName);
    }

    private void OpenSoundPanel()
    {
        ShowPanel(soundPanel, "Sound Panel");
    }

    private void OpenGuidePanel()
    {
        ShowPanel(guidePanel, "Guide Panel");
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터에서는 Application.Quit 이 통하지 않는다
#else
        Application.Quit();
#endif
    }

    private void ShowPanel(GameObject panel, string fieldName)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("StartMenu: " + fieldName + " 이 연결되지 않아 열 수 없습니다.", this);
        }
    }

    private static void HidePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        SetInteractable(newGameButton, interactable);
        SetInteractable(continueButton, interactable && HasSaveFile());
        SetInteractable(soundButton, interactable);
        SetInteractable(guideButton, interactable);
        SetInteractable(quitButton, interactable);
    }

    private static void SetInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private static void AddListener(Button button, Action action)
    {
        if (button != null)
        {
            button.onClick.AddListener(() => action());
        }
    }

    private static void RemoveListeners(Button button)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}
