플레이어 사망 오브젝트 — 2026-09-21

개인 테스트 씬: NaYeongMin
테스트 오브젝트: PlayerDeath_Test (-2, 0, 2). F로 열기/다시 F로 닫기.
샘플: 기관권총(마모20, 사용탄7), 탄약2박스(개봉 잔탄9), 빨간버섯5.

프리팹: Assets/2.Model/Prefabs/NaYeongMin/PlayerDeath/
- PlayerDeathObject.prefab: 사망 위치에 생기는 임시 묘비 모델, Interaction 레이어/Collider.
- PlayerDeathPanel.prefab: HONETi 오른쪽 분실물 UI. 5열 × 6행, 30칸.
  슬롯은 열 때 생성. 일반 적 전리품 8칸과 별개.

스크립트 역할
- InventoryWorldKind.PlayerDeath: 기존 enum 뒤에 추가하여 이전 직렬화 값 유지.
- PlayerDeathContainer: 아이템 보관, Capture/SetItems/CopyItems/ContentsChanged.
- PlayerDeathInteractable: 팀원 IInteractable 기반 F 입력, 거리 초과 닫기.
  WorldContainerInteractable을 중복으로 붙이지 말 것. 기존 팀원 스위치는 새 kind를 모름.
- PlayerDeathSpawner: PlayerVitals.Died 구독, 사망 위치에 생성 후 보유 아이템 비우기.
  playerVitals/inventoryBench/deathPrefab 연결 필요. 생성 실패 시 보유 아이템은 삭제하지 않음.
- InventoryTestBench.PlayerDeath: 별도 UI 열기. 기존 획득/드래그/상세창 사용.

JSON 담당 팀원 연결
- JSON 입출력/씬 간 유지/부활/재접속 복원은 구현하지 않았음.
- 생성: spawner.Spawned 이벤트에서 corpse.transform.position 및 corpse.CopyItems()를 읽어 기록.
- 회수 변경: corpse.ContentsChanged 이벤트에서 CopyItems()를 읽어 저장 갱신.
- 복원: spawner.SpawnFromItems(position, savedSlots). 복원도 Spawned 이벤트 발생.
- SetItems는 30칸 초과를 거부하며 원본 상태 유지. 입력/반환 슬롯은 깊은 복사.
- 가방25 + 장착4 = 최대29칸 사용. UI는 요청대로 30칸 유지. 퀵슬롯은 중복 복사하지 않음.
- 지푸라기는 사망해도 플레이어 보유 수치 유지. 사망 오브젝트 및 복원 입력에서 20001 제외.
- 창고는 사망 이전 대상에서 제외. 전체 초기화 ClearAll과 사망 처리 ClearOnDeath는 별개.
- 보관된 잔탄/마모를 유지. 재개방 시 RNG/아이템 재지급 없음.
- 저장 담당자가 사망 시 인벤토리를 먼저 비우면 안 됨. Spawned 시점 기록 후 정리할 것.
- 다른 사망 드롭/삭제 처리와 중복 등록하지 말 것.
- 빈 사망 오브젝트는 유지. 자동 삭제/지난 사망 오브젝트 수 제한은 미정.

검증: EditMode 131/131 통과. PlayMode API로 사망 생성/위치/21개 슬롯 이전,
원래 가방 비우기, 30칸 UI, 회수/재개방/잔탄·마모 보존, F 닫기 확인.
팀 통합 Test 씬·팀원 Player 코드·CSV·JSON 저장 로직 수정 없음.
