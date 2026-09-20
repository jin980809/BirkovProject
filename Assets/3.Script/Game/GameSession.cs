using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬을 넘어서 유지되는 게임 진행 관리자. 지금은 씬 전환만 담당한다.
// 나중에 레이드 결과(탈출/사망), 자동 저장 같은 것도 여기로 모으면 된다.
//
// 전환 순서: 페이드 아웃 → 로딩 씬 → (목적지 씬을 뒤에서 읽어들이며 진행률 표시) → 페이드 아웃 → 목적지 씬 → 페이드 인
// 로딩 화면의 그림/문구는 로딩 씬에서 직접 꾸미고, 진행률은 LoadingSceneController 가 이 스크립트의 값을 읽어 표시한다.
//
// 씬에 미리 배치할 필요가 없다. 처음 필요해질 때 Instance 가 스스로 만든다.
public class GameSession : MonoBehaviour
{
    [Tooltip("로딩 화면으로 쓸 씬 이름. Build Settings 에 등록돼 있어야 한다")]
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [Tooltip("화면이 어두워지는 시간(초)")]
    [SerializeField] private float fadeOutDuration = 0.35f;
    [Tooltip("화면이 다시 밝아지는 시간(초)")]
    [SerializeField] private float fadeInDuration = 0.35f;
    [Tooltip("로딩 화면을 최소한 이 시간만큼은 보여준다 (너무 빨리 지나가는 것 방지)")]
    [SerializeField] private float minimumLoadingTime = 1f;

    private static GameSession instance;

    private SceneTransitionOverlay overlay;

    public bool IsLoading { get; private set; }

    public string LoadingSceneName
    {
        get { return loadingSceneName; }
    }

    // 로딩 씬이 표시할 값들
    public string PendingSceneName { get; private set; } = string.Empty;
    public float LoadProgress { get; private set; }

    public static GameSession Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject root = new GameObject("GameSession");
                instance = root.AddComponent<GameSession>();
                DontDestroyOnLoad(root);
            }

            return instance;
        }
    }

    // 게임이 처음 켜질 때(첫 씬이 로드되기 전) 미리 만들어 둔다. 그래야 시작 화면도 검은 상태에서 밝아진다.
    // 에디터에서 아무 씬이나 Play 해도 동일하게 동작한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameSession session = Instance;
        session.EnsureOverlay();
        session.overlay.SetAlpha(1f); // 첫 프레임부터 검은 화면으로 덮어둔다
    }

    private void Awake()
    {
        // 씬에 직접 배치한 경우를 대비한 중복 정리 (먼저 있던 쪽을 남긴다)
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
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

    // 씬 전환(LoadSceneRoutine)은 자기가 페이드를 처리한다. 그 외의 경로로 씬이 시작된 경우
    // (게임 실행 직후, 에디터에서 그 씬을 바로 Play, 다른 코드가 씬을 바꾼 경우) 여기서 밝혀준다.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsLoading)
        {
            StartCoroutine(FadeInFromBlack());
        }
    }

    private IEnumerator FadeInFromBlack()
    {
        EnsureOverlay();
        overlay.SetAlpha(1f);

        yield return null; // 씬의 Awake/Start 가 한 번 돈 뒤에 밝히기 시작한다

        yield return overlay.FadeTo(0f, fadeInDuration);
    }

    private void EnsureOverlay()
    {
        if (overlay == null)
        {
            overlay = SceneTransitionOverlay.Create();
        }
    }

    public void LoadScene(string sceneName)
    {
        if (IsLoading)
        {
            return; // 이미 전환 중이면 무시한다 (상호작용을 여러 번 완료해도 한 번만 넘어간다)
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("GameSession: 이동할 씬 이름이 비어 있습니다.", this);
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        IsLoading = true;
        PendingSceneName = sceneName;
        LoadProgress = 0f;

        EnsureOverlay();

        yield return overlay.FadeTo(1f, fadeOutDuration);

        // 인벤토리/HUD 는 씬을 넘어 유지되므로 로딩 화면 위에 그대로 떠 있는다. 로딩 동안에는 숨긴다.
        PersistentUiRoot.SetVisible(false);

        // 1) 로딩 씬으로 이동
        AsyncOperation loadingSceneLoad = SceneManager.LoadSceneAsync(loadingSceneName);
        if (loadingSceneLoad == null)
        {
            Debug.LogError("GameSession: 로딩 씬 '" + loadingSceneName + "' 을 불러오지 못했습니다. " +
                           "Build Settings 등록과 이름을 확인하세요.", this);
            yield return FinishWithFailure();
            yield break;
        }

        while (!loadingSceneLoad.isDone)
        {
            yield return null;
        }

        yield return overlay.FadeTo(0f, fadeInDuration);

        // 2) 로딩 씬을 보여주는 동안 목적지 씬을 읽어들인다
        float loadingStartTime = Time.unscaledTime;

        AsyncOperation targetLoad = SceneManager.LoadSceneAsync(sceneName);
        if (targetLoad == null)
        {
            Debug.LogError("GameSession: '" + sceneName + "' 씬을 불러오지 못했습니다. Build Settings 에 등록했는지 확인하세요.", this);
            yield return FinishWithFailure();
            yield break;
        }

        targetLoad.allowSceneActivation = false;

        // allowSceneActivation 이 false 인 동안 progress 는 0.9 에서 멈춘다. 그 구간을 1로 환산해서 보여준다.
        while (targetLoad.progress < 0.9f)
        {
            LoadProgress = targetLoad.progress / 0.9f;
            yield return null;
        }

        LoadProgress = 1f;

        while (Time.unscaledTime - loadingStartTime < minimumLoadingTime)
        {
            yield return null;
        }

        // 3) 목적지 씬으로 교체
        yield return overlay.FadeTo(1f, fadeOutDuration);

        targetLoad.allowSceneActivation = true;
        while (!targetLoad.isDone)
        {
            yield return null;
        }

        // 새 씬의 Awake/Start 가 한 번 돈 뒤에 밝히기 시작한다 (UI 재연결 등이 끝난 화면을 보여주기 위해)
        yield return null;

        PersistentUiRoot.SetVisible(true);
        yield return overlay.FadeTo(0f, fadeInDuration);

        IsLoading = false;
        PendingSceneName = string.Empty;
        LoadProgress = 0f;
    }

    private IEnumerator FinishWithFailure()
    {
        PersistentUiRoot.SetVisible(true);
        yield return overlay.FadeTo(0f, fadeInDuration);

        IsLoading = false;
        PendingSceneName = string.Empty;
        LoadProgress = 0f;
    }
}
