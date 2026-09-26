using TMPro;
using UnityEngine;

// 제한시간 표시. 실제 시간 계산과 사망 처리는 GameManager 가 하고, 여기서는 그 값을 화면에 그리기만 한다.
//
// 이 UI 는 씬을 넘어 유지되는 캔버스(PersistentUiRoot) 안에 두기 때문에, 전투맵에서 로비로 돌아와도
// 오브젝트가 살아남는다. 그래서 씬이 바뀔 때마다 그 씬에 GameManager 가 있는지 확인해서,
//  - 있으면(전투맵) 표시를 켜고 남은 시간을 갱신한다.
//  - 없으면(로비 등) 표시를 끈다.
//
// 세팅: 시간 표시 오브젝트(예: timer)에 붙이고 Text 를 연결한다. Root 를 비우면 이 오브젝트를 켜고 끈다.
// 로비·전투 양쪽 캔버스에 같은 구조로 넣어 두면 어느 씬에서 시작해도 동작한다.
public class Timer : MonoBehaviour, ISceneRebindable
{
    [Tooltip("남은 시간을 mm:ss 로 표시할 텍스트")]
    [SerializeField] private TMP_Text text;

    [Tooltip("켜고 끌 대상. 비우면 이 컴포넌트가 붙은 오브젝트를 켜고 끈다")]
    [SerializeField] private GameObject root;

    private GameManager gameManager;
    private int shownSeconds = -1; // 초가 바뀔 때만 문자열을 새로 만든다

    private void Start()
    {
        RebindSceneReferences();
    }

    // 씬이 바뀌면 그 씬의 GameManager 를 다시 찾는다 (PersistentUiRoot 가 호출한다)
    public void RebindSceneReferences()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        shownSeconds = -1;
        ApplyVisible(gameManager != null);
    }

    private void Update()
    {
        if (gameManager != null)
        {
            UpdateText(gameManager.RemainingTime);
        }
    }

    private void ApplyVisible(bool visible)
    {
        GameObject target = root;
        if (target == null)
        {
            target = gameObject;
        }

        // 이 컴포넌트가 붙은 오브젝트를 끄면 Update 가 멈춰서 다시 켤 수 없다.
        // 그래서 그 경우에는 텍스트만 끈다.
        if (target == gameObject && text != null)
        {
            text.enabled = visible;
        }
        else if (target != null)
        {
            target.SetActive(visible);
        }
    }

    private void UpdateText(float remainingTime)
    {
        if (text == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
        if (totalSeconds == shownSeconds)
        {
            return;
        }

        shownSeconds = totalSeconds;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        text.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}
