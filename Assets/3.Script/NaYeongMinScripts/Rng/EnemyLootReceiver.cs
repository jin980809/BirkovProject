using System.Collections.Generic;
using System.Text.RegularExpressions;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

namespace Birdkov.NaYeongMin.Rng
{
    // 적 담당의 사망 확정 시 NotifyDeath 호출. Disable/Destroy를 사망으로 추정하지 않는다.
    public sealed class EnemyLootReceiver : MonoBehaviour
    {
        // 팀원 EnemyArmor 가 OnEnable 에서 HeadGear/Belly 밑에 붙이는 모델 이름. 1LvHelmet(Clone) ~ 3LvArmor(Clone)
        private static readonly Regex WornPattern = new Regex(@"^([1-3])Lv(Helmet|Armor)\(Clone\)");

        [Tooltip("비워 두면 씬의 LootRuntime 을 자동으로 찾는다.")]
        [SerializeField] private LootRuntime lootRuntime;
        [SerializeField] private DropSourceType sourceType = DropSourceType.BasicEnemy;
        [Tooltip("스폰 때 입고 나온 헬멧·조끼를 전리품 앞칸에 넣는다.")]
        [SerializeField] private bool dropWornArmor = true;

        private readonly List<int> wornItemIds = new List<int>();
        private bool deathHandled;

        public bool DeathHandled => deathHandled;
        public bool DropSpawned { get; private set; }
        public DropSourceType SourceType => sourceType;
        public IReadOnlyList<int> WornItemIds => wornItemIds;

        private void Start()
        {
            CaptureWornArmor();
        }

        // 헬멧 1~3 -> 13001~13003, 조끼 1~3 -> 12001~12003 (ItemData.csv)
        public void CaptureWornArmor()
        {
            wornItemIds.Clear();
            if (!dropWornArmor) return;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                Match match = WornPattern.Match(child.name);
                if (!match.Success) continue;
                int tier = match.Groups[1].Value[0] - '0';
                wornItemIds.Add((match.Groups[2].Value == "Helmet" ? 13000 : 12000) + tier);
            }
        }

        public bool NotifyDeath()
        {
            if (deathHandled) return false;
            if (lootRuntime == null) lootRuntime = FindAnyObjectByType<LootRuntime>();
            if (lootRuntime == null || !lootRuntime.IsReady || sourceType == DropSourceType.Box)
            {
                Debug.LogWarning("적 드롭 런타임/적 종류 연결을 확인할 것.", this);
                return false;
            }
            deathHandled = true;
            DropSpawned = lootRuntime.Spawn(transform.position, transform.rotation, sourceType,
                LootContainerSize.Box2x4, wornItemIds) != null;
            return DropSpawned;
        }

        // 적 풀 재사용 시 새 생명 시작 시점에만 호출한다.
        public void ResetForSpawn()
        {
            deathHandled = false;
            DropSpawned = false;
            CaptureWornArmor();
        }
    }
}
