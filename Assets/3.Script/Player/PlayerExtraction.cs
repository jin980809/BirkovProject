using UnityEngine;

// B 키를 누르고 있으면 귀환(탈출) 게이지가 차고, 다 차면 로비로 씬을 전환한다.
//  - 누르고 있는 동안만 찬다. 떼면 0 으로 돌아간다.
//  - 제자리에서만 가능하다. 이동 입력이 들어오면 취소된다.
//  - 게이지가 도는 동안 총소리급 소음을 주기적으로 낸다 - 주변 적이 몰려온다.
//  - 진행률은 기존 상호작용 게이지(InteractionPromptUI)에 함께 표시한다.
//
// 귀환 중에는 사격 / 구르기 / 상호작용 / 무기 교체가 막힌다 (PlayerController 가 IsExtracting 을 확인).
// 시야 회전은 그대로 된다.
[RequireComponent(typeof(PlayerController))]
public class PlayerExtraction : MonoBehaviour
{
    [Tooltip("귀환할 씬 이름. Build Settings 에 등록돼 있어야 한다")]
    [SerializeField] private string targetSceneName = "LobbyScene";
    [Tooltip("게이지가 다 차는 데 걸리는 시간(초)")]
    [SerializeField] private float extractDuration = 6f;

    [Header("소음")]
    [Tooltip("귀환 중 소음 반경. 총소리와 같은 종류로 전달된다 (적 감지 게이지 +50)")]
    [SerializeField] private float noiseRadius = 25f;
    [Tooltip("소음을 내는 주기(초)")]
    [SerializeField] private float noiseInterval = 1f;
    [Tooltip("귀환을 시작하는 순간에도 한 번 소음을 낸다")]
    [SerializeField] private bool emitNoiseOnStart = true;

    private const int GunshotNoiseType = 2; // PlayerNoise 의 소음 종류 - 0 걷기 / 1 달리기·구르기 / 2 총소리

    private PlayerController player;
    private PlayerInputHandler input;
    private PlayerVitals vitals;
    private PlayerNoise noise;

    private float extractTimer;
    private float nextNoiseTime;

    public bool IsExtracting { get; private set; }

    // UI 가 읽는 0~1 진행률
    public float ExtractProgress01
    {
        get
        {
            float progress = 0f;
            if (extractDuration > 0f)
            {
                progress = Mathf.Clamp01(extractTimer / extractDuration);
            }

            return progress;
        }
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        TryGetComponent(out input);
        TryGetComponent(out vitals);
        TryGetComponent(out noise);
    }

    private void Update()
    {
        if (CanContinue())
        {
            TickExtraction();
        }
        else
        {
            CancelExtraction();
        }
    }

    // 누르고 있고, 제자리에 서 있고, 다른 행동 중이 아닐 때만 진행된다
    private bool CanContinue()
    {
        bool canContinue = false;

        if (input != null && input.ExtractHeld && !GameSession.Instance.IsLoading)
        {
            bool isAlive = vitals == null || !vitals.IsDead;
            bool isStandingStill = input.MoveInput.sqrMagnitude <= 0.01f;
            bool isFree = !player.IsControlLocked && !player.IsUsingItem && !player.IsReloading && !player.IsDodging;

            canContinue = isAlive && isStandingStill && isFree;
        }

        return canContinue;
    }

    private void TickExtraction()
    {
        if (!IsExtracting)
        {
            IsExtracting = true;
            extractTimer = 0f;
            nextNoiseTime = Time.time;

            if (!emitNoiseOnStart)
            {
                nextNoiseTime = Time.time + noiseInterval;
            }
        }

        EmitNoiseIfDue();

        extractTimer += Time.deltaTime;
        if (extractTimer >= extractDuration)
        {
            CompleteExtraction();
        }
    }

    // 귀환 중에는 계속 소리가 난다 - 적이 위치를 알고 몰려오게 하기 위한 것이라 반경을 크게 잡는다
    private void EmitNoiseIfDue()
    {
        if (noise != null && Time.time >= nextNoiseTime)
        {
            nextNoiseTime = Time.time + Mathf.Max(0.1f, noiseInterval);
            noise.EmitNoise(noiseRadius, GunshotNoiseType);
        }
    }

    private void CancelExtraction()
    {
        if (IsExtracting)
        {
            IsExtracting = false;
            extractTimer = 0f;
        }
    }

    private void CompleteExtraction()
    {
        IsExtracting = false;
        extractTimer = 0f;

        GameSession.Instance.LoadScene(targetSceneName);
    }
}
