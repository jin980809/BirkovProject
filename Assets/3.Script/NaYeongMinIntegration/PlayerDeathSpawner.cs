using System;
using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

namespace Birdkov.NaYeongMin.Integration
{
    public sealed class PlayerDeathSpawner : MonoBehaviour
    {
        public PlayerVitals playerVitals;
        public InventoryTestBench inventoryBench;
        public PlayerDeathContainer deathPrefab;
        public PlayerDeathContainer LastSpawned { get; private set; }
        // 위치 및 아이템 JSON 기록은 팀원이 이 이벤트/API에 연결한다.
        public event Action<PlayerDeathContainer> Spawned;
        private bool handledDeath;

        // 비워 두면 씬에서 찾는다. 플레이어에 붙이지 않고 단독 프리팹으로 둬도 된다.
        private void Awake()
        {
            if (playerVitals == null) playerVitals = FindAnyObjectByType<PlayerVitals>();
            if (inventoryBench == null) inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }

        private void OnEnable() { if (playerVitals != null) playerVitals.Died += HandleDeath; }
        private void OnDisable() { if (playerVitals != null) playerVitals.Died -= HandleDeath; }
        private void Update() { if (playerVitals != null && !playerVitals.IsDead) handledDeath = false; }

        public PlayerDeathContainer SpawnFromItems(Vector3 position, IEnumerable<GridSlotData> items)
        {
            if (deathPrefab == null || items == null || float.IsNaN(position.sqrMagnitude) || float.IsInfinity(position.sqrMagnitude)) return null;
            var corpse = Instantiate(deathPrefab, position, Quaternion.identity);
            if (!corpse.SetItems(items)) { Destroy(corpse.gameObject); return null; }
            var interaction = corpse.GetComponent<PlayerDeathInteractable>();
            if (interaction != null) interaction.inventoryBench = inventoryBench;
            LastSpawned = corpse;
            Spawned?.Invoke(corpse);
            return corpse;
        }

        private void HandleDeath()
        {
            if (handledDeath || inventoryBench == null || !inventoryBench.IsReady) return;
            var player = inventoryBench.PlayerData;
            var items = new List<GridSlotData>(player.inventory.slots);
            items.AddRange(player.equipmentSlots.slots);
            if (SpawnFromItems(playerVitals.transform.position, items) == null)
            {
                Debug.LogError("사망 오브젝트 생성 실패. 아이템은 삭제하지 않았습니다.", this);
                return;
            }
            handledDeath = true;
            inventoryBench.KillPlayer();
            inventoryBench.CloseCurrent();
            var weapon = playerVitals.GetComponent<WeaponController>();
            if (weapon != null) weapon.RefreshEquippedWeapon();
        }
    }
}
