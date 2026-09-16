using UnityEngine;

// 팀원 적 코드는 사망 이벤트 없이 Destroy 로 끝난다. 그 코드를 고치지 않고 파괴 시점만 잡아
// 전리품 지급을 트리거한다. 사망이 아닌 파괴(씬 언로드, 플레이 종료)는 걸러낸다.
namespace Birdkov.NaYeongMin.Rng
{
    [RequireComponent(typeof(EnemyLootReceiver))]
    public sealed class EnemyDeathWatcher : MonoBehaviour
    {
        private static bool quitting;

        private EnemyLootReceiver receiver;

        private void Awake()
        {
            receiver = GetComponent<EnemyLootReceiver>();
            quitting = false;
            Application.quitting += MarkQuitting;
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
