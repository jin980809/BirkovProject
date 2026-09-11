드롭 CSV 런타임 연결 밑작업 (2026-09-10)

1. 향후 담당 오브젝트/프리팹에 DropTableDatabase 컴포넌트 추가.
2. Drop Table Csv에 Assets/DataTeble/NaYeongMinCsvData/DropTable.csv 연결.
3. ItemDatabase.Load() 완료 후 dropTableDatabase.Load(itemDatabase.Catalog) 호출.
4. 기존 DropRoller.Roll(dropTableDatabase.Entries, sourceType, sizePreset) 사용.

자동 Awake 로드 없음: 아이템 카탈로그 초기화 순서를 호출 측에서 보장.
미연결 CSV/미등록 아이템 ID는 예외 발생. 실패 시 Entries는 null.
Load 성공 전 추첨 금지. Entries의 행 데이터는 읽기 전용으로 취급.
씬/UI/적 사망/상호작용 연결은 이번 밑작업에 포함하지 않음.
기존 테스트 UI의 직접 CSV 로드는 그대로 유지.

[2026-09-11 추가] LootRuntime 으로 연결 완료

더 이상 ItemDatabase / DropTableDatabase / LootDropPool 을 따로 배선하지 않아도 된다.
LootRuntime 이 세 컴포넌트를 한 오브젝트에 묶고 초기화 순서까지 보장한다.

프리팹 : Assets/2.Model/Prefabs/NaYeongMin/LootRuntime.prefab
         ItemDatabase(ItemData.csv) + DropTableDatabase(DropTable.csv)
         + LootDropPool(LootDropObject.prefab) + LootRuntime 이 모두 연결된 상태다.

쓰는 법
  1. 씬에 LootRuntime.prefab 을 배치한다. 맵당 하나면 된다.
  2. 적 사망이 확정된 시점에 호출한다.
       LootDropObject obj = lootRuntime.Spawn(사망위치, 회전, 적유형);
       if (obj == null) { 풀 고갈. 이번 사망은 오브제를 남기지 않는다. }
     적 유형은 DropSourceType.BasicEnemy / HeavyEnemy / RangedEnemy.
  3. 상자는 오브제가 필요 없으면 결과만 받는다.
       LootContainerData loot = lootRuntime.Roll(DropSourceType.Box, LootContainerSize.Box3x3);
  4. 전리품을 다 비우면 LootDropObject 가 스스로 풀에 돌아간다.
     상호작용 담당은 아이템을 옮긴 뒤 NotifyContentsChanged 만 호출하면 된다.

주의
  - Spawn 과 Roll 은 IsReady 가 false 면 각각 null 을 돌려준다. 반환값을 반드시 확인한다.
  - Initialize 는 Awake 에서 자동 호출되며 여러 번 불러도 안전하다.
  - useFixedSeed 를 켜면 seed 값으로 재현 가능한 추첨이 된다. 검증용이며 기본은 꺼짐.
  - 풀 기본 30개. 고갈 시 자동 확장하지 않는다. 늘리려면 LootDropPool.initialSize 를 수정한다.

검증 기록 (2026-09-11)
  Initialize 성공, 4개 소스 x 4개 상자 크기 추첨 정상, 빈 상자도 정상 결과.
  Spawn 33회 요청 -> 30개 성공, 3개 null. 풀 고갈 처리 확인.
