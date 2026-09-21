using Birdkov.NaYeongMin.Rng;
using UnityEngine;

namespace Birdkov.NaYeongMin.Integration
{
    // 통합 씬 전리품 검증 전용. 팀원 적 AI와 분리한다.
    [RequireComponent(typeof(EnemyLootReceiver))]
    public sealed class DropDeathTestTarget : MonoBehaviour, IDamageable
    {
        public void TakeDamage(float amount)
        {
            if (!Application.isPlaying || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f) return;
            Defeat();
        }

        [ContextMenu("Defeat Test Target")]
        public void Defeat()
        {
            if (!Application.isPlaying) return;
            var receiver = GetComponent<EnemyLootReceiver>();
            if (receiver.DeathHandled || !receiver.NotifyDeath()) return;
            gameObject.SetActive(false);
        }
    }
}
