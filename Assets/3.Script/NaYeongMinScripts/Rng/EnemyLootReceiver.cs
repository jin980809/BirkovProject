using UnityEngine;

namespace Birdkov.NaYeongMin.Rng
{
    // 적 담당의 사망 확정 시 NotifyDeath 호출. Disable/Destroy를 사망으로 추정하지 않는다.
    public sealed class EnemyLootReceiver : MonoBehaviour
    {
        [SerializeField] private LootRuntime lootRuntime;
        [SerializeField] private DropSourceType sourceType = DropSourceType.BasicEnemy;
        private bool deathHandled;

        public bool DeathHandled => deathHandled;
        public bool DropSpawned { get; private set; }

        public bool NotifyDeath()
        {
            if (deathHandled) return false;
            if (lootRuntime == null || !lootRuntime.IsReady || sourceType == DropSourceType.Box)
            {
                Debug.LogWarning("적 드롭 런타임/적 종류 연결을 확인할 것.", this);
                return false;
            }
            deathHandled = true;
            DropSpawned = lootRuntime.Spawn(transform.position, transform.rotation, sourceType) != null;
            return DropSpawned;
        }

        // 적 풀 재사용 시 새 생명 시작 시점에만 호출한다.
        public void ResetForSpawn()
        {
            deathHandled = false;
            DropSpawned = false;
        }
    }
}
