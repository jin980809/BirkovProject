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
