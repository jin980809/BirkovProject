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
        [Tooltip("팀원 Player의 입력/회복/상자 연결 사용. 이 브리지는 풀 전리품과 사망/거리 처리를 담당합니다.")]
        [SerializeField] private bool useTeamPlayerConnections;
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
            if (!useTeamPlayerConnections)
            {
                inventoryBench.BindRecoveryTarget(this);
                playerInput.WeaponSelected += SelectWeapon;
                playerInput.QuickSlotUsed += UseQuickSlot;
                playerInput.InventoryToggled += ToggleInventory;
            }
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
                if (!useTeamPlayerConnections) inventoryBench.BindRecoveryTarget(null);
                inventoryBench.useStandaloneKeyboard = previousStandaloneKeyboard;
            }
            openedDrop = null;
            lastStats = null;
            subscribed = false;
        }

        private void Update()
        {
            if (!IsConnected || playerVitals == null) return;
            Transform anchor = inventoryBench.ExternalAnchor;
            if (inventoryBench.IsExternalOpen && (anchor == null || !anchor.gameObject.activeInHierarchy ||
                Vector3.Distance(playerVitals.transform.position, anchor.position) > interactionDistance))
            {
                inventoryBench.CloseCurrent();
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
            if (inventoryBench.IsExternalOpen)
            {
                inventoryBench.CloseCurrent();
                openedDrop = null;
                return;
            }
            if (useTeamPlayerConnections)
            {
                PlayerController player = playerVitals.GetComponent<PlayerController>();
                if (player != null && (player.IsControlLocked || player.IsUsingItem || player.IsReloading)) return;
                PlayerInteraction interaction = playerVitals.GetComponent<PlayerInteraction>();
                if (interaction != null && interaction.HasTarget) return;
            }
            LootDropObject nearest = null;
            InventoryWorldContainer nearestContainer = null;
            float nearestDistance = interactionDistance;
            foreach (Collider hit in Physics.OverlapSphere(playerVitals.transform.position, interactionDistance))
            {
                InventoryWorldContainer container = hit.GetComponentInParent<InventoryWorldContainer>();
                if (container != null && container.isActiveAndEnabled)
                {
                    if (useTeamPlayerConnections) continue;
                    float containerDistance = Vector3.Distance(playerVitals.transform.position, container.transform.position);
                    if (containerDistance <= nearestDistance)
                    {
                        nearestContainer = container;
                        nearest = null;
                        nearestDistance = containerDistance;
                    }
                    continue;
                }
                LootDropObject drop = hit.GetComponentInParent<LootDropObject>();
                if (drop == null || !drop.gameObject.activeInHierarchy) continue;
                float distance = Vector3.Distance(playerVitals.transform.position, drop.transform.position);
                if (distance <= nearestDistance) { nearest = drop; nearestContainer = null; nearestDistance = distance; }
            }
            if (nearestContainer != null)
            {
                nearestContainer.Open(inventoryBench);
                return;
            }
            if (nearest == null) return;
            openedDrop = nearest;
            inventoryBench.OpenLoot(nearest);
        }
    }
}
