using System.Collections;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using UnityEngine;

namespace Birdkov.NaYeongMin.InventoryTest
{
    // 수동 획득 테스트 전용. 실제 드롭 확률과 팀원 시스템은 변경하지 않는다.
    public sealed class LootPickupTest : MonoBehaviour
    {
        [SerializeField] private LootRuntime lootRuntime;
        [SerializeField] private InventoryTestBench inventoryBench;
        private LootDropObject activeDrop;

        private IEnumerator Start()
        {
            if (lootRuntime == null || inventoryBench == null)
            {
                Debug.LogError("테스트 드롭의 Runtime/InventoryBench 연결 필요.", this);
                yield break;
            }
            while (!lootRuntime.IsReady || !inventoryBench.IsReady) yield return null;
            SpawnDrop();
            inventoryBench.OpenInventory();
        }

        [ContextMenu("Spawn Test Drop")]
        public void SpawnDrop()
        {
            if (!Application.isPlaying || lootRuntime == null || !lootRuntime.IsReady ||
                inventoryBench == null || !inventoryBench.IsReady) return;
            if (activeDrop != null && activeDrop.gameObject.activeInHierarchy) return;

            LootContainerData loot = new LootContainerData(LootContainerSize.Box2x4);
            InventoryService service = new InventoryService(lootRuntime.Catalog);
            int[] itemIds = { 23001, 23002, 23003 };
            foreach (int itemId in itemIds)
            {
                if (service.AddItem(loot.loot, itemId, 1).MovedAmount != 1)
                {
                    Debug.LogError("테스트 아이템 확인 필요: " + itemId, this);
                    return;
                }
            }

            activeDrop = lootRuntime.GetComponent<LootDropPool>().Rent(transform.position, Quaternion.identity, loot);
            if (activeDrop == null)
            {
                Debug.LogWarning("테스트 드롭 생성 실패: 풀에 여유 없음.", this);
                return;
            }

            // 표시와 트리거 콜라이더는 LootDropObject.prefab 에 들어 있다. 여기서 더 붙이지 않는다.
            Debug.Log("획득 테스트 준비: 노란 오브제 2m 이내에서 F로 열기/닫기 → 전리품 클릭 또는 드래그로 획득. E는 UI 열기/닫기. 전량 획득 시 오브제 회수.", this);
        }
    }
}
