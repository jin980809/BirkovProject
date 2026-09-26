using UnityEngine;
using UnityEngine.SceneManagement;

// UI 캔버스(인벤토리 벤치 + HUD)를 씬 전환 후에도 살려둔다.
// 인벤토리 데이터(가방·장비·퀵슬롯·창고·화폐)가 InventoryTestBench 안에 들어 있어서, 이 캔버스가 살아 있으면
// 데이터도 그대로 따라간다.
//
//  - 처음 로드된 캔버스만 남기고, 다음 씬에 들어 있는 같은 역할의 캔버스는 스스로 지운다 (UI 두 벌 방지).
//  - 씬이 바뀌면 자식 UI 들에게 "새 씬의 플레이어를 다시 찾아라"라고 알린다 (ISceneRebindable).
//
// 세팅: 로비/전투 씬 양쪽의 UI 캔버스 최상위 오브젝트에 이 컴포넌트를 붙인다.
public class PersistentUiRoot : MonoBehaviour
{
    [Tooltip("이 이름의 씬에서는 유지 중인 UI(인벤토리/HUD)를 숨긴다. 시작 화면에는 게임 UI 가 보이면 안 된다")]
    [SerializeField] private string hiddenSceneName = "StartScene";

    private static PersistentUiRoot instance;

    private CanvasGroup group;

    // 로딩 중인지 (GameSession 이 정한다)와, 지금 씬이 UI 를 숨기는 씬인지를 따로 들고 있다가 함께 반영한다.
    // 그래야 씬 전환이 끝나고 GameSession 이 다시 보이라고 해도 시작 씬에서는 계속 숨어 있는다.
    private bool visibleRequested = true;
    private bool hiddenForScene;

    // 로딩 씬을 보여주는 동안에는 유지 중인 UI(인벤토리/HUD)를 숨긴다. GameSession 이 호출한다.
    public static void SetVisible(bool visible)
    {
        if (instance != null)
        {
            instance.visibleRequested = visible;
            instance.ApplyVisibility();
        }
    }

    // 씬을 넘어 살아남은 UI 캔버스 안에서 컴포넌트를 찾는다 (꺼져 있는 것도 포함, 없으면 null).
    //
    // 왜 필요한가: 새 씬에 들어 있던 중복 캔버스는 Awake 에서 스스로 비활성화 + Destroy 되지만,
    // Destroy 는 프레임 끝에야 실제로 반영된다. 그래서 같은 프레임의 Start() 에서
    // FindAnyObjectByType(FindObjectsInactive.Include) 를 쓰면 그 "곧 사라질" 중복을 잡을 수 있고,
    // 그 참조는 다음 프레임에 죽어서 아무 동작도 하지 않는다 (크로스헤어가 안 꺼지는 등).
    // 유지되는 UI(인벤토리 벤치 / 크로스헤어 / 사망 패널 등)는 이 함수로 찾는다.
    public static T Find<T>() where T : Component
    {
        if (instance == null)
        {
            return null;
        }

        return instance.GetComponentInChildren<T>(true);
    }

    // 새로하기처럼 지금까지의 진행을 완전히 버려야 할 때 부른다.
    // 이 캔버스가 인벤토리 데이터(가방·장비·창고·돈)를 들고 있어서, 지우면 다음 씬의 캔버스가
    // 새 데이터로 시작한다. 저장 파일까지 지운 상태라면 완전히 빈 상태로 시작된다.
    // 정리(구독 해제 등)는 이 오브젝트의 OnDestroy 가 한다.
    public static void Discard()
    {
        if (instance != null)
        {
            // Destroy 는 이번 프레임 끝에야 실제로 지워지므로, 그 전에 다른 스크립트가 찾지 못하게 먼저 끈다
            instance.gameObject.SetActive(false);
            Destroy(instance.gameObject);
        }
    }

    private void ApplyVisibility()
    {
        if (group == null && !TryGetComponent(out group))
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }

        bool visible = visibleRequested && !hiddenForScene;

        if (visible)
        {
            group.alpha = 1f;
        }
        else
        {
            group.alpha = 0f;
        }

        group.blocksRaycasts = visible;
        group.interactable = visible;

        ApplyGameplayCursor(visible);
    }

    // 크로스헤어는 켜져 있는 동안 OS 커서를 숨긴다(CrosshairUI). 그래서 캔버스를 투명하게만 하면
    // 시작 화면에서 마우스 커서가 보이지 않는다 - 게임 UI 를 숨기는 동안에는 크로스헤어도 같이 끈다.
    // 로비/전투로 돌아오면 다시 켠다 (인벤토리가 열려 있으면 PlayerInventoryToggle 이 곧 다시 맞춘다).
    private void ApplyGameplayCursor(bool gameplayVisible)
    {
        CrosshairUI crosshair = Find<CrosshairUI>();

        if (crosshair != null)
        {
            crosshair.SetCrosshairActive(gameplayVisible);
        }
        else if (!gameplayVisible)
        {
            Cursor.visible = true;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // 새 씬에 들어 있던 UI 캔버스다. 유지 중인 쪽이 이미 있으므로 이쪽을 지운다.
            // Destroy 는 이번 프레임 끝에야 실제로 지워지므로, 그 전에 다른 스크립트가
            // FindAnyObjectByType 으로 이 중복 벤치/HUD 를 잡아버린다 (곧 파괴돼서 참조가 죽는다).
            // 그래서 먼저 비활성화한다 - FindAnyObjectByType 은 비활성 오브젝트를 건너뛴다.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (transform.parent != null)
        {
            transform.SetParent(null, true); // DontDestroyOnLoad 는 최상위 오브젝트에만 적용된다
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 시작 씬에서는 게임 UI(인벤토리/HUD)를 숨긴다. 로비/전투로 돌아오면 다시 보인다.
        // 지우지는 않는다 - 이 캔버스가 인벤토리 데이터를 들고 있기 때문이다.
        hiddenForScene = scene.name == hiddenSceneName;
        ApplyVisibility();

        // 로딩 씬에는 플레이어가 없다. 거기서 다시 찾으면 전부 null 이 되고 경고만 쌓이므로 건너뛴다.
        if (scene.name != GameSession.Instance.LoadingSceneName)
        {
            RebindAll();
        }
    }

    // 자식 UI 들이 새 씬의 플레이어/무기 등을 다시 찾게 한다
    public void RebindAll()
    {
        ISceneRebindable[] targets = GetComponentsInChildren<ISceneRebindable>(true);
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i].RebindSceneReferences();
        }
    }
}
