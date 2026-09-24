using Birdkov.NaYeongMin.InventoryTest;
using UnityEngine;

namespace Birdkov.NaYeongMin.Integration
{
    // 기획서 6.7 - 피격 1회당 장착 중인 방어구 부위가 모두 닳는다.
    // 팀원 PlayerVitals 에 피격 전용 이벤트가 없어 HealthChanged 로 감지한다.
    // 허기 0 자연 피해는 프레임당 값이 매우 작아 minHitDamage 로 걸러진다.
    public sealed class ArmorDurabilityBridge : MonoBehaviour
    {
        public PlayerVitals playerVitals;
        public InventoryTestBench inventoryBench;
        [Min(0.01f)] public float minHitDamage = 0.5f;

        private float lastHealth = -1f;

        private void Awake()
        {
            if (playerVitals == null) playerVitals = GetComponentInParent<PlayerVitals>();
            if (playerVitals == null) playerVitals = FindAnyObjectByType<PlayerVitals>();
            if (inventoryBench == null) inventoryBench = FindAnyObjectByType<InventoryTestBench>();
        }

        private void OnEnable()
        {
            if (playerVitals == null) return;
            lastHealth = playerVitals.Health;
            playerVitals.HealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (playerVitals != null) playerVitals.HealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            float drop = lastHealth - current;
            lastHealth = current;
            if (drop < minHitDamage) return;
            if (inventoryBench == null || !inventoryBench.IsReady) return;
            inventoryBench.ApplyArmorHit();
        }
    }
}
