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
    }

    private void CloseOpenInventory()
    {
        if (inventoryBench == null)
        {
            inventoryBench = FindAnyObjectByType<InventoryTestBench>();
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
            deathPanel = FindAnyObjectByType<DeathPanelUI>(FindObjectsInactive.Include);
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
