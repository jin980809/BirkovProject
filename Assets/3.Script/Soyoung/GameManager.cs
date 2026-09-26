using UnityEngine;
using TMPro;

// 전투맵 제한시간. 이 컴포넌트가 있는 씬에서 바로 카운트다운이 시작되고, 시간이 다 되면
// 플레이어를 조건 없이 사망시킨다 (BattleScene 에만 둔다 - 로비에는 넣지 않는다).
//
// 시간 흐름: Time.deltaTime 으로 줄이므로 ESC 일시정지처럼 Time.timeScale 을 0 으로 만드는 동안에는
// 저절로 멈춘다. 인벤토리/창고를 열어둔 동안에는 timeScale 을 건드리지 않으므로 계속 흐른다.
//
// 사망 처리: PlayerVitals.Kill() 을 부른다 - 방어구 감쇄를 거치지 않고 체력을 0 으로 만들며
// Died 이벤트도 정상적으로 발생하므로, 사망 패널 / 전리품 드랍 / 로비 복귀는 기존 흐름이 그대로 처리한다.
public class GameManager : MonoBehaviour
{
    [Header("제한시간")]
    [Tooltip("제한시간(분). 다 지나면 플레이어가 즉시 사망한다")]
    [SerializeField] private float timeLimitMinutes = 12f;

    [Tooltip("남은 시간을 mm:ss 로 표시할 텍스트. 비워두면 표시 없이 시간만 잰다")]
    [SerializeField] private TMP_Text timeLimitText;

    [Tooltip("제한시간이 끝났을 때 사망시킬 플레이어. 비워두면 씬에서 자동으로 찾는다")]
    [SerializeField] private PlayerVitals playerVitals;

    private float remainingTime;
    private bool timedOut;
    private int shownSeconds = -1; // 초가 바뀔 때만 문자열을 새로 만들기 위해 기억한다

    public float RemainingTime
    {
        get { return remainingTime; }
    }

    public bool TimedOut
    {
        get { return timedOut; }
    }

    // Awake 가 아니라 Start 에서 찾는다 - 씬 전환으로 들어온 경우 중복 오브젝트 정리가 Awake 에서
    // 일어나므로, 그 뒤인 Start 에서 찾으면 살아남은 플레이어가 잡힌다.
    private void Start()
    {
        remainingTime = Mathf.Max(0f, timeLimitMinutes * 60f);
        EnsurePlayerVitals();
        UpdateTimeText();
    }

    private void Update()
    {
        if (!timedOut && !IsPlayerDead())
        {
            TickTimeLimit();
        }
    }

    private void TickTimeLimit()
    {
        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        UpdateTimeText();

        if (remainingTime <= 0f)
        {
            HandleTimeOut();
        }
    }

    private void HandleTimeOut()
    {
        timedOut = true;
        EnsurePlayerVitals();

        if (playerVitals != null)
        {
            playerVitals.Kill();
        }
        else
        {
            Debug.LogWarning("GameManager: PlayerVitals 를 찾지 못해 제한시간 사망 처리를 하지 못했습니다.", this);
        }
    }

    // 제한시간이 끝나기 전에 죽은 경우에는 더 세지 않는다 (사망 패널이 떠 있는 동안 시간이 흐르지 않게)
    private bool IsPlayerDead()
    {
        return playerVitals != null && playerVitals.IsDead;
    }

    private void EnsurePlayerVitals()
    {
        if (playerVitals == null)
        {
            playerVitals = FindAnyObjectByType<PlayerVitals>();
        }
    }

    private void UpdateTimeText()
    {
        if (timeLimitText != null)
        {
            int totalSeconds = Mathf.CeilToInt(remainingTime);

            if (totalSeconds != shownSeconds)
            {
                shownSeconds = totalSeconds;

                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                timeLimitText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
            }
        }
    }
}
