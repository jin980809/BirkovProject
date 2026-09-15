# HONETi UI 배치 인수인계 — 2026-09-15

## 완료한 비코드 작업
- `Assets/4.Sprite/NaYeongMinUi/InventoryUiTheme.asset`: HONETi 스프라이트 연결. 정사각/가로 슬롯 모두 `universal_panel_2.png` 사용, 어두운 청회색 테마.
- 같은 폴더 `ItemIconTable.asset`: 기존 씬 InventoryTestBench.icons 30건 그대로 복사.
- `Assets/2.Model/Prefabs/NaYeongMin/Ui/ItemSlot.prefab`: 배경·아이콘·수량·캡션 연결.
- 같은 폴더 `InventoryUiRoot.prefab`: 공통 창. 왼쪽 장비/가방, 오른쪽 외부 컨테이너, 하단 퀵슬롯. 가방 25칸, 장비 4칸, 퀵슬롯 2+3칸.
- ExternalPanel에 ScrollRect/RectMask2D/ContentSizeFitter 구성. 10x12 창고 수용.
- `Assets/DataTeble/NaYeongMinDropSettings/Chest_{Weapon,Ammo,Armor,Med,Food,Normal}.asset`: 각각 3x5, 4x2, 3x3, 2x4, 2x4, 2x4. 허용 목록은 모두 비어 있음. 확률/아이템을 임의 확정하지 않음.
- NaYeongMin 씬에 새 프리팹 인스턴스 비활성 배치. 씬 ItemDatabase와 실제 Player Transform 참조 연결.
- 기존 InventoryTestBench와 기존 입력 연결 유지. 새 화면 전환 완료 전에는 Build Legacy Ui를 끄지 않음.
- HONETi 원본, 기존 코드, CSV, 팀원 프리팹 수정 없음. 커밋/푸시 없음.

## 코드 수정이 필요한 차단 사항 — Claude 담당
1. MonoImporter.GetAllRuntimeMonoScripts()/MonoScript.GetClass() 조회에서 다음 클래스에 대응하는 MonoScript가 없음:
   - ItemTooltipView, DragGhostView: 현재 ItemSlotView.cs에 함께 정의.
   - StorageContainer, LootContainer, MapChestContainer: 현재 ContainerSources.cs에 함께 정의.
   각 MonoBehaviour를 같은 이름의 독립 .cs 파일로 분리하고 namespace/API를 유지할 것. 임의 GUID나 다른 스크립트의 참조로 연결하지 말 것.
2. PlayerInventoryBridge는 아직 InventoryTestBench만 참조. 새 컨트롤러의 ToggleExternal/OpenInventory/CloseCurrent 및 퀵슬롯·회복·사망 경로로 전환 필요.
3. InventoryUiController.Awake는 ItemDatabase.Catalog가 먼저 로드되었다고 가정. 초기화 순서를 보장하는 내 쪽 부트스트랩 필요. Script Execution Order 임의 변경으로 숨기지 말 것.
4. StorageContainer.BindWarehouse, RecoveryTarget, 팀원 WeaponInventoryBridge.SetPlayerInventoryData, 실제 JSON 로드/저장 호출 연결 필요. 같은 SaveData 사용. 팀원 코드 수정 금지.
5. Tooltip/DragGhost는 새 프리팹의 `Tooltip_PENDING_COMPONENT`, `DragGhost_PENDING_COMPONENT`에 시각 요소만 준비. 클래스 분리 후 컴포넌트와 필드 연결 필요. 자식들은 표시 Root 아래에 있어야 함께 숨겨짐.
6. LootContainer 및 MapChestContainer는 클래스 분리 전이므로 월드 오브젝트/원본 드롭 프리팹에 부착하지 않음. 적 사망의 개별 허용목록 연결도 별도 확인 필요.

## 이어서 할 배치 작업 — Codex 담당
- 코드 수정 인수인계 확인 후 Tooltip/DragGhost/월드 컨테이너 컴포넌트 부착.
- Inspector로 새 브리지/부트스트랩 연결. 이후 기존 테스트 UI 비활성화 및 새 루트 활성화.
- 사용자 지정 itemId/확률로 상자별 Entries 구성. 빈 목록은 실제로 아무것도 생성하지 않음.
- 공통 컨트롤러 하나가 UI 4종 모드를 제공하는 구조이며, 중복 SaveData를 만드는 별도 컨트롤러 4개를 생성하지 않음.

## 검증 범위
- 임시 편집 모드 미리보기에서 Initialize/OpenInventory와 가방 25칸, 외부 120칸 생성 및 아이콘 표시 확인.
- 미리보기용 테스트 데이터/오브젝트/카메라는 저장 전 제거. 실제 저장 데이터 변경 없음.
- 씬 validate: Missing Script 0, broken prefab 0. 마지막 콘솔 오류/경고 0.
- 실제 F 입력, 창고 이동, 전리품 풀 반환, 맵 상자 재개방, 툴팁/드래그 동작은 아직 미검증. UI 4종이 바로 사용 가능한 상태라고 보고하지 말 것.
- 최종 통합테스트는 팀원 머지 이후.
