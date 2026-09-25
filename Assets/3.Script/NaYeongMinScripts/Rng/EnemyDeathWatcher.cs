using UnityEngine;

// 팀원 적 코드는 사망 이벤트 없이 끝난다(예전 Destroy, 지금 SetActive(false)). 그 코드를 고치지 않고
// 사라지는 시점만 잡아 전리품 지급을 트리거한다. 사망이 아닌 경우는 걸러낸다.
// - Awake 의 출현 확률로 꺼진 적: Start 전이라 무시
// - 스포너가 부모 영역을 끈 경우: 자기 자신은 켜져 있어(activeSelf) 무시
// - 씬 언로드, 플레이 종료: 무시
namespace Birdkov.NaYeongMin.Rng
{
    [RequireComponent(typeof(EnemyLootReceiver))]
    public sealed class EnemyDeathWatcher : MonoBehaviour
    {
        private static bool quitting;

        private EnemyLootReceiver receiver;
        private bool started;

        private void Awake()
        {
            receiver = GetComponent<EnemyLootReceiver>();
            quitting = false;
            Application.quitting += MarkQuitting;
        }

        private void Start()
        {
            started = true;
        }

        // 팀원 EnemyController 는 체력이 0 아래가 되면 자기 자신을 끈다.
        private void OnDisable()
        {
            if (started && !gameObject.activeSelf && Application.isPlaying && !quitting && receiver != null && gameObject.scene.isLoaded)
            {
                receiver.NotifyDeath();
            }
        }

        private void OnDestroy()
        {
            Application.quitting -= MarkQuitting;

            // 플레이 중 살아 있는 씬에서 사라졌을 때만 사망으로 본다.
            if (!Application.isPlaying || quitting || receiver == null || !gameObject.scene.isLoaded)
            {
                return;
            }

            receiver.NotifyDeath();
        }

        private static void MarkQuitting()
        {
            quitting = true;
        }
    }
}
