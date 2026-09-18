using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

namespace Birdkov.NaYeongMin.Integration
{
    public sealed class WeaponDurabilityBridge : MonoBehaviour
    {
        public WeaponController weaponController;
        public WeaponInventoryBridge weaponInventory;
        public InventoryTestBench inventoryBench;
        private GridSlotData pendingBroken;
        private PlayerInventoryData linkedData;

        // 테스트벤치의 초기화/불러오기는 PlayerInventoryData 를 새 객체로 갈아 끼우는데,
        // 팀원 링크는 최초 1회만 연결한다. 그대로 두면 무기 브리지가 옛 가방을 봐서
        // 재장전 탄약이 0 으로 보이고 마모도 옛 슬롯에 쌓인다. 공개 API 로만 다시 잇는다.
        private void Update()
        {
            if (inventoryBench == null || !inventoryBench.IsReady || weaponInventory == null) return;
            PlayerInventoryData current = inventoryBench.PlayerData;
            if (current == null || ReferenceEquals(current, linkedData)) return;
            linkedData = current;
            weaponInventory.SetPlayerInventoryData(current);
            if (weaponController != null) weaponController.RefreshEquippedWeapon();
        }

        private void OnEnable() { if (weaponController != null) weaponController.ShotFired += HandleShot; }
        private void OnDisable() { if (weaponController != null) weaponController.ShotFired -= HandleShot; FinishShot(); }

        // 팀원의 공개 API 만 쓴다. WeaponInventoryBridge.GetWeaponSlotData 가 돌려주는 객체는
        // WeaponController 가 장착에 쓰는 바로 그 슬롯이다. 선택 번호는 InventoryTestBenchLink 가
        // 실제 장착과 같은 조건(PlayerController.CanSwapWeapon)으로 갱신한다.
        private GridSlotData FindEquippedSlot()
        {
            int equippedItemId = weaponController.EquippedWeaponItemId;
            GridSlotData slot = weaponInventory.GetWeaponSlotData(inventoryBench.SelectedWeaponIndex);
            if (slot != null && !slot.IsEmpty() && slot.itemId == equippedItemId) return slot;

            // 두 무기 칸이 같은 총기일 때만 여기로 온다. 마모량은 어느 쪽이든 같은 규칙이다.
            for (int index = 0; index < 2; index++)
            {
                slot = weaponInventory.GetWeaponSlotData(index);
                if (slot != null && !slot.IsEmpty() && slot.itemId == equippedItemId) return slot;
            }

            return null;
        }

        private void HandleShot()
        {
            if (inventoryBench == null || !inventoryBench.IsReady || weaponInventory == null || weaponController == null) return;
            GridSlotData slot = FindEquippedSlot();
            if (slot == null) return;
            if (WeaponDurability.ApplyShot(slot))
            {
                pendingBroken = slot;
                weaponController.StopFiring();
            }
        }

        private void LateUpdate() { FinishShot(); }

        private void FinishShot()
        {
            // ShotFired 는 투사체 생성 이전 이벤트. 마지막 정상 발사 이후에 제거한다.
            if (pendingBroken == null) return;
            pendingBroken.Clear();
            pendingBroken = null;
            if (weaponController != null) weaponController.RefreshEquippedWeapon();
            if (inventoryBench != null) inventoryBench.Refresh();
        }
    }
}
