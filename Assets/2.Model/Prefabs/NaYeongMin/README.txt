NaYeongMin 프리팹 (2026-09-10)

InventoryTestBench.prefab
- 검증용 UI Canvas. ItemData/DropTable CSV와 아이콘 16개 연결 포함.
- 씬에 하나 배치하고 Play. UI 슬롯/버튼은 Start에서 생성하므로 편집 모드에서는 비어 있음.
- 클릭/드래그 입력에는 씬에 EventSystem이 하나 필요. 기존 EventSystem이 있으면 추가하지 말 것.
- NaYeongMin 씬의 기존 Canvas는 이 프리팹 인스턴스로 연결됨.
- 테스트용 임시 무기/방어구와 별도 테스트 저장 경로 사용. 본 게임 UI 아님.
- font 미지정 시 기존 코드가 OS 한글 폰트를 사용. 다른 환경에서는 한글 Font 에셋 지정 권장.

LootDropObject.prefab (기존)
- 전리품 데이터 보관용. 외형/충돌/상호작용 연결은 별도.

LootDropPool.prefab (기존)
- LootDropObject 참조 연결, 초기 풀 30개.
- Rent()가 null이면 풀 고갈. 호출 측에서 처리.

ItemDatabase/DropTableDatabase는 아직 실제 사용 오브젝트가 없어 이번 프리팹화에서 제외.
씬 공용 Camera/Light/EventSystem은 중복 배치를 피하려고 묶지 않음.
CSV/아이콘 원본 경로는 이동하지 않음. 프리팹과 .meta 파일을 함께 커밋할 것.

[2026-09-11] LootRuntime.prefab 추가
드롭 시스템 단일 진입점. ItemDatabase + DropTableDatabase + LootDropPool + LootRuntime 이
한 오브젝트에 묶여 있고 CSV 와 LootDropObject.prefab 이 이미 연결되어 있다.
씬에 하나만 배치하면 되고 호출 측은 Spawn 또는 Roll 만 쓰면 된다.
상세는 Assets/3.Script/NaYeongMinScripts/Rng/DropTableDatabase_README.txt 참고.
