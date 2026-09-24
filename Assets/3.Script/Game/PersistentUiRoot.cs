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
    private static PersistentUiRoot instance;

    private CanvasGroup group;

    // 로딩 씬을 보여주는 동안에는 유지 중인 UI(인벤토리/HUD)를 숨긴다. GameSession 이 호출한다.
    public static void SetVisible(bool visible)
    {
        if (instance != null)
        {
            instance.ApplyVisible(visible);
        }
    }

    private void ApplyVisible(bool visible)
    {
        if (group == null && !TryGetComponent(out group))
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }

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
