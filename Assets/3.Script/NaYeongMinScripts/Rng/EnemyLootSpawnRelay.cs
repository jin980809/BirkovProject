using UnityEngine;

namespace Birdkov.NaYeongMin.Rng
{
    // 팀원 EnemyController의 사망 Instantiate 진입점을 기존 전리품 풀로 연결.
    public sealed class EnemyLootSpawnRelay : MonoBehaviour
    {
        [SerializeField] private DropSourceType sourceType = DropSourceType.BasicEnemy;

        private void Start()
        {
            LootRuntime runtime = null;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                runtime = root.GetComponentInChildren<LootRuntime>();
                if (runtime != null) break;
            }
            if (runtime != null && runtime.IsReady &&
                (sourceType == DropSourceType.BasicEnemy || sourceType == DropSourceType.HeavyEnemy || sourceType == DropSourceType.RangedEnemy))
                runtime.Spawn(transform.position, transform.rotation, sourceType);
            else
                Debug.LogWarning("적 사망 드롭: 같은 씬의 LootRuntime/적 종류 확인 필요.", this);
            Destroy(gameObject);
        }
    }
}
