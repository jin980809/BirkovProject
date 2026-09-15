using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.InventoryTest;
using Birdkov.NaYeongMin.Rng;
using UnityEngine;

namespace Birdkov.NaYeongMin.Integration
{
    // 기본 어셈블리에서 팀원 API를 참조한다. 팀원 원본은 수정하지 않는다.
    public sealed class PlayerInventoryBridge : MonoBehaviour, IRecoveryTarget
    {
        [SerializeField] private PlayerVitals playerVitals;
        [SerializeField] private PlayerInputHandler playerInput;
        [SerializeField] private InventoryTestBench inventoryBench;
        [SerializeField, Min(0.1f)] private float interactionDistance = 2f;
        private LootDropObject openedDrop;
        private bool subscribed;
        private bool previousStandaloneKeyboard;
        private string lastStats;

        public bool IsConnected => subscribed && inventoryBench != null && inventoryBench.IsReady;

        private void OnEnable()
        {
            if (playerVitals == null || playerInput == null || inventoryBench == null) return;
            previousStandaloneKeyboard = inventoryBench.useStandaloneKeyboard;
            inventoryBench.useStandaloneKeyboard = false;
            inventoryBench.BindRecoveryTarget(this);
            playerInput.WeaponSelected += SelectWeapon;
            playerInput.QuickSlotUsed += UseQuickSlot;
            playerInput.InventoryToggled += ToggleInventory;
            playerInput.InteractPressed += Interact;
            playerVitals.Died += HandleDeath;
            subscribed = true;
        }

        private void OnDisable()
        {
            if (!subscribed) return;
            if (playerInput != null)
            {
                playerInput.WeaponSelected -= SelectWeapon;
                playerInput.QuickSlotUsed -= UseQuickSlot;
                playerInput.InventoryToggled -= ToggleInventory;
                playerInput.InteractPressed -= Interact;
            }
            if (playerVitals != null) playerVitals.Died -= HandleDeath;
            if (inventoryBench != null)
            {
                inventoryBench.CloseLoot();
                inventoryBench.BindRecoveryTarget(null);
                inventoryBench.useStandaloneKeyboard = previousStandaloneKeyboard;
            }
            openedDrop = null;
            lastStats = null;
            subscribed = false;
        }

        private void Update()
        {
            if (!IsConnected || playerVitals == null) return;
            if (openedDrop != null && (!openedDrop.gameObject.activeInHierarchy ||
                Vector3.Distance(playerVitals.transform.position, openedDrop.transform.position) > interactionDistance))
            {
                inventoryBench.CloseLoot();
                openedDrop = null;
            }
            string stats = $"체력 {playerVitals.Health:0}/{playerVitals.MaxHealth:0}    허기 {playerVitals.Hunger:0}/{playerVitals.MaxHunger:0}    수분 {playerVitals.Water:0}/{playerVitals.MaxWater:0}";
            if (stats == lastStats) return;
            lastStats = stats;
            inventoryBench.SetVitalsDisplay(stats);
        }

        public bool TryApplyRecovery(float healthPercent, float hungerPercent, float waterPercent)
        {
            if (!isActiveAndEnabled || playerVitals == null || playerVitals.IsDead ||
                !IsValidAmount(healthPercent) || !IsValidAmount(hungerPercent) || !IsValidAmount(waterPercent)) return false;
            float health = playerVitals.Health, hunger = playerVitals.Hunger, water = playerVitals.Water;
            // 기획서 10.1: 회복량은 최대치 대비 백분율이다.
            playerVitals.Heal(playerVitals.MaxHealth * healthPercent / 100);
            playerVitals.RestoreHunger(playerVitals.MaxHunger * hungerPercent / 100);
            playerVitals.RestoreWater(playerVitals.MaxWater * waterPercent / 100);
            return playerVitals.Health > health || playerVitals.Hunger > hunger || playerVitals.Water > water;
        }

        private static bool IsValidAmount(float amount) => amount >= 0 && amount <= 100 && !float.IsNaN(amount) && !float.IsInfinity(amount);

        private void SelectWeapon(int index)
        {
            if (IsConnected && !playerVitals.IsDead) inventoryBench.SelectWeapon(index);
        }

        private void UseQuickSlot(int index)
        {
            if (IsConnected && !playerVitals.IsDead) inventoryBench.UseItemQuickSlot(index);
        }

        private void ToggleInventory()
        {
            if (IsConnected && !playerVitals.IsDead) inventoryBench.ToggleInventory();
        }

        private void HandleDeath()
        {
            if (inventoryBench != null) inventoryBench.KillPlayer();
            openedDrop = null;
        }

        private void Interact()
        {
            if (!IsConnected || playerVitals.IsDead) return;
            if (inventoryBench.IsLootOpen)
            {
                inventoryBench.CloseLoot();
                inventoryBench.ToggleInventory();
                openedDrop = null;
                return;
            }
            if (inventoryBench.InteractWithHoveredItem()) return;
            LootDropObject nearest = null;
            float nearestDistance = interactionDistance;
            foreach (Collider hit in Physics.OverlapSphere(playerVitals.transform.position, interactionDistance))
            {
                LootDropObject drop = hit.GetComponentInParent<LootDropObject>();
                if (drop == null || !drop.gameObject.activeInHierarchy) continue;
                float distance = Vector3.Distance(playerVitals.transform.position, drop.transform.position);
                if (distance <= nearestDistance) { nearest = drop; nearestDistance = distance; }
            }
            if (nearest == null) return;
            openedDrop = nearest;
            inventoryBench.OpenLoot(nearest);
        }
    }
}
