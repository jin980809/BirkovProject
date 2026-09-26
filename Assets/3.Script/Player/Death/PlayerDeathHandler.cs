using System.Collections;
using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

// 플레이어가 죽었을 때 사망 패널(플레이 시간 + 로비 버튼)을 띄운다.
//
// 아이템 손실과 분실물(사망 오브젝트) 생성은 NaYeongMin 쪽 PlayerDeathSpawner 가 처리하고,
// 그 분실물을 씬을 나갔다 와도 유지하는 건 PlayerDeathPersistence 가 처리한다.
// 여기서는 화면 쪽만 맡는다 - 열려 있던 인벤토리/상자 UI 를 닫고 패널을 띄운다.
// (입력은 PlayerController.IsDead 가 막는다.)
//
// 세팅: 플레이어(PlayerVitals 와 같은 오브젝트)에 붙인다.
[RequireComponent(typeof(PlayerVitals))]
public class PlayerDeathHandler : MonoBehaviour
{
    [Tooltip("인벤토리 벤치. 비우면 씬에서 찾는다 (죽을 때 열려 있던 UI 를 닫는 데만 쓴다)")]
    [SerializeField] private InventoryTestBench inventoryBench;
    [Tooltip("사망 패널. 비우면 씬에서 찾는다 (꺼져 있어도 찾는다)")]
    [SerializeField] private DeathPanelUI deathPanel;

    // 사망 처리(아이템 비우기 + 묘비 생성)가 끝날 때까지 기다리는 프레임 수
    private const int SaveDelayFrames = 2;

    private PlayerVitals vitals;
    private bool handled;

    private void Awake()
    {
        vitals = GetComponent<PlayerVitals>();
    }

    private void OnEnable()
    {
        if (vitals != null)
        {
            vitals.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (vitals != null)
        {
            vitals.Died -= HandleDied;
        }
    }

    private void HandleDied()
    {
        if (handled)
        {
            return;
        }

        handled = true;

        // TODO: 사망 애니메이션 (나중에 연출을 넣을 자리)

        CloseOpenInventory();
        ShowPanel();
        StartCoroutine(SaveAfterDeath());
    }

    // 죽은 직후에 바로 저장한다. 안 하면 사망 패널에서 로비로 돌아가지 않고 게임을 끄는 경우
    // 저장 파일에는 죽기 전 인벤토리가 그대로 남아서, 아이템을 잃지 않은 채 묘비까지 남는다.
    //
    // 몇 프레임 기다리는 이유: PlayerVitals.Died 는 여러 스크립트가 같이 구독하고 있고 호출 순서가
    // 정해져 있지 않다. NaYeongMin 의 PlayerDeathSpawner 가 아이템을 비우고 묘비를 만든 뒤에
    // 저장해야 하므로, 그 처리가 끝날 시간을 준다.
    private IEnumerator SaveAfterDeath()
    {
        for (int i = 0; i < SaveDelayFrames; i++)
        {
            yield return null;
        }

        if (inventoryBench != null && inventoryBench.IsReady)
        {
            inventoryBench.SaveGame();
        }
    }

    private void CloseOpenInventory()
    {
        if (inventoryBench == null)
        {
            inventoryBench = (PersistentUiRoot.Find<InventoryTestBench>() ?? FindAnyObjectByType<InventoryTestBench>());
        }

        if (inventoryBench != null)
        {
            inventoryBench.CloseCurrent();
        }
    }

    private void ShowPanel()
    {
        if (deathPanel == null)
        {
            deathPanel = (PersistentUiRoot.Find<DeathPanelUI>() ?? FindAnyObjectByType<DeathPanelUI>(FindObjectsInactive.Include));
        }

        if (deathPanel != null)
        {
            // 이번 판의 시간: 이 전투 씬이 시작된 뒤부터 지금까지
            deathPanel.Show(Time.timeSinceLevelLoad);
        }
        else
        {
            Debug.LogWarning("PlayerDeathHandler: DeathPanelUI 가 씬에 없어 사망 패널을 띄우지 못했습니다.", this);
        }
    }
}
