using System.IO;
using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;
using UnityEngine.SceneManagement;

// 인벤토리 저장/불러오기 시점을 정한다. 데이터 자체는 InventoryTestBench(NaYeongMin)가 들고 있고,
// 여기서는 "언제" 불러오고 저장할지만 결정한다.
//  - 불러오기: 게임을 켠 뒤 딱 한 번, 벤치가 준비되면 저장 파일이 있을 때만 불러온다.
//    씬을 옮길 때마다 불러오면 그 판에서 얻은 아이템이 디스크 값으로 되돌아가므로 절대 그러지 않는다.
//  - 저장: 로비 씬에 들어온 뒤 (귀환/사망 후 로비 진입). 사망 순간의 저장은 PlayerDeathHandler 가 한다.
//
// 씬에 미리 배치할 필요가 없다. 게임이 시작될 때 스스로 만들어지고 씬을 넘어 유지된다 (GameSession 과 같은 방식).
public class SaveCoordinator : MonoBehaviour
{
    // 이 이름의 씬이 로드되면 로비에 들어온 것으로 본다
    private const string LobbySceneName = "LobbyScene";

    // 로비 씬이 로드된 뒤 UI 재연결 등이 끝나도록 이 프레임 수만큼 기다렸다가 저장한다
    private const int SettleFramesBeforeLobbySave = 3;

    private const string SaveFileName = "testbench.json";

    private static SaveCoordinator instance;

    private InventoryTestBench bench;
    private int lobbySaveCountdown = -1;

    // 게임을 켠 뒤의 첫 불러오기가 끝났는지
    public bool InitialLoadDone { get; private set; }

    // 그 첫 불러오기에서 실제로 저장 파일을 불러왔는지 (파일이 없어서 새로 시작이면 false)
    public bool LoadedFromSave { get; private set; }

    public static SaveCoordinator Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject root = new GameObject("SaveCoordinator");
                instance = root.AddComponent<SaveCoordinator>();
                DontDestroyOnLoad(root);
            }

            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        SaveCoordinator ensure = Instance;
    }

    private void Awake()
    {
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

    // 첫 불러오기가 끝나기 전에 온 로드(게임을 로비에서 시작한 경우)는 저장하지 않는다 - 아직 불러온 데이터가 없다.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (InitialLoadDone && scene.name == LobbySceneName)
        {
            lobbySaveCountdown = SettleFramesBeforeLobbySave;
        }
    }

    private void Update()
    {
        // 씬 전환 직후에는 곧 파괴되는 중복 벤치를 잡았을 수 있다 (PersistentUiRoot 참고). 죽은 참조면 다시 찾는다.
        if (bench == null)
        {
            bench = FindAnyObjectByType<InventoryTestBench>();
        }

        if (bench == null || !bench.IsReady)
        {
            return;
        }

        if (!InitialLoadDone)
        {
            PerformInitialLoad();
            return;
        }

        if (lobbySaveCountdown > 0)
        {
            lobbySaveCountdown--;
        }
        else if (lobbySaveCountdown == 0)
        {
            lobbySaveCountdown = -1;
            bench.SaveGame();
        }
    }

    private void PerformInitialLoad()
    {
        InitialLoadDone = true;

        string path = Path.Combine(Application.persistentDataPath, "NaYeongMinTestBench", SaveFileName);
        LoadedFromSave = File.Exists(path);

        if (LoadedFromSave)
        {
            bench.LoadGame();

            // LoadGame 은 끝에 전리품 패널을 켜 두므로 화면이 열려 보이지 않게 닫는다
            bench.CloseCurrent();
        }
    }
}
