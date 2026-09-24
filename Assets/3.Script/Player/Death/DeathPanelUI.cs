using UnityEngine;
using UnityEngine.UI;

// 사망 패널 (플레이 시간 + 로비로 가는 버튼). 패널의 모양은 직접 만들고, 이 컴포넌트는 값을 채우고 버튼만 연결한다.
//
// 세팅:
//  - 항상 켜져 있는 오브젝트(예: 캔버스)에 붙이고, Panel Root 에 실제 패널 오브젝트를 연결한다.
//    Panel Root 를 비우면 이 컴포넌트가 붙은 오브젝트 자체를 패널로 본다 (그 경우 꺼둔 채로 두면 된다).
//  - Play Time Text 에 플레이 시간을 보여줄 Text, Lobby Button 에 로비로 가는 버튼을 연결한다.
//  - PlayerDeathHandler 가 플레이어가 죽으면 이 패널을 띄운다.
public class DeathPanelUI : MonoBehaviour, ISceneRebindable
{
    [Tooltip("죽었을 때 켜질 패널. 비우면 이 컴포넌트가 붙은 오브젝트를 켠다")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text playTimeText;
    [SerializeField] private Button lobbyButton;
    [Tooltip("{0} 자리에 시간이 들어간다 (예: 12:34)")]
    [SerializeField] private string playTimeFormat = "플레이 시간 {0}";
    [Tooltip("버튼을 누르면 이동할 로비 씬 이름. Build Settings 에 등록돼 있어야 한다")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    private CrosshairUI crosshair;
    private bool isShown;

    public bool IsShown
    {
        get { return isShown; }
    }

    private void Awake()
    {
        if (panelRoot != null && panelRoot != gameObject)
        {
            panelRoot.SetActive(false);
        }

        if (lobbyButton != null)
        {
            lobbyButton.onClick.AddListener(GoToLobby);
        }
    }

    // 씬이 바뀌면(로비로 돌아가거나 다시 전투로 들어가면) 패널을 닫는다.
    // 이 UI 가 붙은 캔버스는 씬을 넘어서 유지되므로(PersistentUiRoot), 끄지 않으면 다음 씬까지 떠 있는다.
    public void RebindSceneReferences()
    {
        crosshair = FindAnyObjectByType<CrosshairUI>(FindObjectsInactive.Include);
        Hide();
    }

    public void Hide()
    {
        if (!isShown)
        {
            return;
        }

        isShown = false;

        GameObject target = panelRoot != null ? panelRoot : gameObject;
        target.SetActive(false);

        if (lobbyButton != null)
        {
            lobbyButton.interactable = true; // 다음 사망에 대비해 다시 누를 수 있게 되돌린다
        }

        // 크로스헤어/커서를 원래대로. 인벤토리가 열려 있으면 PlayerInventoryToggle 이 다음 프레임에 다시 맞춘다.
        if (crosshair != null)
        {
            crosshair.SetCrosshairActive(true);
        }
    }

    // 플레이어가 죽었을 때 PlayerDeathHandler 가 부른다. playSeconds 는 이번 판(전투 씬 진입 이후)의 시간.
    public void Show(float playSeconds)
    {
        isShown = true;

        if (playTimeText != null)
        {
            playTimeText.text = string.Format(playTimeFormat, FormatTime(playSeconds));
        }

        GameObject target = panelRoot != null ? panelRoot : gameObject;
        target.SetActive(true);

        if (crosshair == null)
        {
            crosshair = FindAnyObjectByType<CrosshairUI>(FindObjectsInactive.Include);
        }
    }

    // 버튼을 누를 수 있게 마우스 커서를 계속 보이게 해 둔다. 인벤토리를 닫는 등으로 다른 시스템이
    // 크로스헤어를 다시 켜도 여기서 다시 끈다.
    private void Update()
    {
        if (!isShown)
        {
            return;
        }

        if (crosshair != null && crosshair.gameObject.activeSelf)
        {
            crosshair.SetCrosshairActive(false);
        }

        Cursor.visible = true;
    }

    private void GoToLobby()
    {
        if (lobbyButton != null)
        {
            lobbyButton.interactable = false; // 여러 번 눌러도 한 번만 넘어간다
        }

        GameSession.Instance.LoadScene(lobbySceneName);
    }

    private string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int hours = total / 3600;
        int minutes = (total % 3600) / 60;
        int secs = total % 60;

        if (hours > 0)
        {
            return string.Format("{0}:{1:00}:{2:00}", hours, minutes, secs);
        }

        return string.Format("{0:00}:{1:00}", minutes, secs);
    }
}
