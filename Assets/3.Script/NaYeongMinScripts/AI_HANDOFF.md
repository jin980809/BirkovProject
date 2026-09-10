# AI 인수인계 — Birdkov Action / NaYeongMin 파트

> 이 파일 하나만 읽고 작업을 이어받을 수 있게 만든 상태 문서다.
> 작업 스텝이 끝날 때마다 담당 AI가 아래 13. 변경 이력에 한 줄 추가하고
> 바뀐 섹션을 갱신한다. 갱신하지 않은 채 다음 AI에게 넘기지 말 것.
>
> 최종 갱신: 2026-09-10 (드롭 CSV 런타임 연결 밑작업. EditMode 55개 통과)

---

## 1. 절대 규칙 (위반 금지)

- **커밋·푸시 금지.** git 명령을 실행하지 않는다. 사용자가 직접 한다.
- **브랜치 `NaYeongMin` 외 사용 금지.**
- **팀원 파일 / `Packages/` / `ProjectSettings/` 수정 금지.**
- **사용자 승인 없이 코드·씬 수정 금지.** 지시받은 범위만 작업한다.
- **기획서에 없는 값을 임의로 만들지 않는다.** 모르면 확정 요청 목록에 올린다.
- **한국어 이름·수치 누락은 임의 작성 금지, 보고 대상이다.**
- 참고 엑셀 2종(드랍테이블.xlsx, Item_id_displayName_Korean.xlsx)은 **원본 수정 금지**.

## 2. 프로젝트 좌표

| 항목 | 값 |
|---|---|
| Unity 프로젝트 | `BirkovProject` (Unity 6000.2.8f1, URP) |
| 브랜치 | `NaYeongMin` |
| 내 씬 | `Assets/1.Scene/NaYeongMin.unity` (Camera + Light + InventoryTestBench + EventSystem) |
| 내 스크립트 루트 | `Assets/3.Script/NaYeongMinScripts/` |
| 내 CSV 루트 | `Assets/DataTeble/NaYeongMinCsvData/` (폴더명 오타 Teble 은 기존 참조 때문에 유지) |
| 내 프리팹 | `Assets/2.Model/Prefabs/NaYeongMin/` |
| 아이콘 에셋 | `Assets/4.Sprite/Images/_Icons/` (로우폴리 팩, 교체 예정 임시값) |
| 어셈블리 | `Birdkov.NaYeongMin` / `Birdkov.NaYeongMin.Tests` |
| 네임스페이스 | `Birdkov.NaYeongMin.InventorySystem` / `.Rng` / `.SaveSystem` / `.Tests` |

## 3. 코딩 규칙

- 변수·필드 `camelCase` / 클래스·메서드·프로퍼티 `PascalCase`
- **최소 코드.** 과설계 금지. 표준 라이브러리로 되면 의존성 추가하지 않는다.
- 최소 결합. 씬·플레이어·전투·UI를 직접 참조하지 않는다.
- 팀 연동은 서비스 클래스의 공개 메서드로만 노출하고, 연결 방법은 README에 적는다.
- 재사용 UI는 Prefab화.

## 4. 담당 범위

**포함** — 그리드 인벤토리 / 장비 슬롯 / 허브 창고 / CSV 아이템 데이터 /
상자·적 사망 RNG 드롭 / 재료 아이템 파밍 / JSON 저장·불러오기·손상 복구 /
플레이어 사망 시 인벤토리 전체 비우기 / 드롭 오브젝트 풀링

**제외** — UI 디자인, E 상호작용 판정, 적 AI·사망 애니메이션, 스킬,
HUD, 사운드, 맵 배치, 특수 아이템 효과, 총기 발사·전투 로직

## 5. 현재 구현 상태

담당 8항목 기준 진척 82%. 완료 5 / 부분 2 / 미착수 1 / 차단 0.

| # | 항목 | 진척 | 비고 |
|---|---|---|---|
| 01 | 그리드 인벤토리 | 100% | 가방 25칸, 전 아이템 1칸 고정 |
| 02 | 장비 슬롯 | 100% | 무기 2 + 방어구 2. 빈 슬롯 전제 착용 |
| 03 | 허브 창고 | 100% | 120칸, 상호작용 게이트 |
| 04 | CSV 아이템 데이터 | 72% | 21종 등록. 총기·탄약 8종 대기 |
| 05 | 상자·적 RNG 드롭 | 85% | 51행. 무기·탄약 32행 대기 |
| 06 | 재료 아이템 파밍 | 0% | 채집. 다음 작업 큐 1순위 |
| 07 | JSON 저장·복구 | 100% | saveVersion 3 |
| 08 | 사망 시 전체 비우기 | 100% | 창고는 유지 |

### InventorySystem/
| 파일 | 역할 |
|---|---|
| `ItemData.cs` | 아이템 스탯 구조체 + ItemType / EquipmentSlotType / ItemRarity enum |
| `ItemCatalog.cs` | `IItemCatalog` 인터페이스 + id→ItemData 딕셔너리. 중복 id 예외 |
| `ItemCsvLoader.cs` | ItemData.csv 파싱 (따옴표·BOM 처리) |
| `ItemDatabase.cs` | MonoBehaviour. TextAsset CSV를 인스펙터에서 연결 |
| `GridSlotData.cs` | 슬롯 1칸 (itemId, amount) |
| `GridContainerData.cs` | width x height 그리드 컨테이너 |
| `InventorySettings.cs` | 모든 크기 상수 |
| `InventoryResult.cs` | 결과 enum + `InventoryMoveResult` 구조체 |
| `InventoryService.cs` | 추가·이동·제거·스택·분할·교환 (컨테이너 무관 순수 로직) |
| `EquipmentSlots.cs` | 장비 슬롯 인덱스 상수 + 수용 규칙 |
| `PlayerInventoryData.cs` | 가방 5x5 + 장비 4칸 + 아이템 퀵슬롯 매핑 int[3] |
| `PlayerInventoryService.cs` | 착용·해제·파괴 + 퀵슬롯 API + `ClearOnDeath` |
| `LootContainerSize.cs` | 상자 크기 프리셋 enum + resolver |
| `LootContainerData.cs` | 가변 크기 전리품 컨테이너 |
| `WarehouseService.cs` | 허브 창고 + 상호작용 게이트 |

### Rng/
| 파일 | 역할 |
|---|---|
| `RandomSource.cs` | `IRandomSource` + `SystemRandomSource(seed)` |
| `DropSourceType.cs` | Box / BasicEnemy / HeavyEnemy / RangedEnemy |
| `DropTableEntry.cs` | 드롭 1행 |
| `DropTableCsvLoader.cs` | DropTable.csv 파싱 + `FindMissingItemIds` 무결성 검사 |
| `DropRoller.cs` | 독립 추첨. 컨테이너 칸 수 초과분 파기 |
| `LootDropObject.cs` | 노란 오브제 MonoBehaviour |
| `LootDropPool.cs` | 30개 사전 생성 풀 |

### SaveSystem/
| 파일 | 역할 |
|---|---|
| `PlayerSaveData.cs` | saveVersion + 인벤토리 + 창고 |
| `SaveDataValidator.cs` | 로드 시 구조 검증 |
| `JsonSaveSystem.cs` | tmp -> backup -> 교체 안전 저장, 손상 시 backup 복구 |

### Tests/Editor/ — 55개 전부 통과 (2026-09-10 재실행)
`DropRollerTests` / `DropTableCsvLoaderTests` / `EquipmentSlotTests` / `InventoryServiceTests` /
`ItemCsvLoaderTests` / `ItemQuickSlotTests` / `JsonSaveSystemTests` /
`LootContainerSizeTests` / `LootDropPoolTests` / `WarehouseServiceTests`

## 6. 확정된 크기 상수

```
가방          5 x 5  = 25칸   (기획서 6.3)
장비 슬롯     4칸            0 주무기 / 1 보조무기 / 2 머리 / 3 몸
무기 퀵슬롯   2개            장비 슬롯 0,1 의 링크. 데이터 없음
아이템 퀵슬롯 3개            가방 인덱스 매핑
전리품        8칸            (기획서 10.3)
창고         10 x 12 = 120칸 (덕코프 원작 만렙 참조, 잠정)
상자         2x4(8) / 3x3(9) / 3x5(15) / 4x1(4)
드롭 풀       30개
스택         일반 5 / 무기·특수 1
아이템 점유   전부 1칸 고정
```

## 7. 팀 연동 API

```csharp
// 아이템
ItemCsvLoader.Parse(csvText)                    // -> List<ItemData>
new ItemCatalog(items)                          // -> IItemCatalog

// 인벤토리 (UI는 이것만 호출)
playerInventoryService.AddToInventory(playerData, itemId, amount)   // 항상 가방으로만
playerInventoryService.Move(playerData, srcType, srcIdx, dstType, dstIdx, amount)
playerInventoryService.ClearOnDeath(playerData)

// 장비 슬롯
playerInventoryService.EquipFromInventory(playerData, invIdx, equipSlotIdx) // 빈 슬롯일 때만
playerInventoryService.UnequipToInventory(playerData, equipSlotIdx, invIdx)
playerInventoryService.DestroyEquipped(playerData, equipSlotIdx)    // 내구도 0
playerInventoryService.TryGetEquipped(playerData, equipSlotIdx, out item)

// 무기 퀵슬롯 (장비 슬롯 0,1 의 링크. 지정 절차 없음)
playerInventoryService.TryGetWeaponQuickSlot(playerData, quickIdx, out equipIdx, out item)

// 아이템 퀵슬롯 (가방 인덱스 매핑)
playerInventoryService.AssignItemQuickSlot(playerData, quickIdx, invIdx)
playerInventoryService.ClearItemQuickSlot(playerData, quickIdx)
playerInventoryService.TryGetItemQuickSlot(playerData, quickIdx, out invIdx, out item)
playerInventoryService.SanitizeItemQuickSlots(playerData)  // Add/Move 안에서 자동 호출

// 창고 (거점 오브젝트 상호작용 담당이 Open/Close 호출)
warehouseService.Open() / Close()
warehouseService.Store(playerData, invIdx, whIdx, amount)
warehouseService.Withdraw(playerData, whIdx, invIdx, amount)
warehouseService.MoveWithin(srcIdx, dstIdx, amount)
// Open() 전에는 전부 InventoryResult.DestinationRejected 반환

// 드롭 (상자 담당 / 적 담당이 호출)
DropTableCsvLoader.Parse(csvText)
DropTableCsvLoader.FindMissingItemIds(entries, catalog) // 시작 시 1회, 빈 목록이어야 정상
new DropRoller(randomSource).Roll(entries, sourceType, sizePreset)

// 풀링
lootDropPool.Rent()   // 고갈 시 null 반환. 호출 측에서 null 체크 필수
lootDropObject.NotifyContentsChanged()  // 비면 자동 반환

// 저장 (게임 흐름 담당이 호출)
jsonSaveSystem.Save(slotName, playerSaveData)
jsonSaveSystem.TryLoad(slotName, out data, out recoveredFromBackup)
```

## 8. 데이터 파일

| 파일 | 내용 |
|---|---|
| `ItemData.csv` | 아이템 21종. 헤더 30열 |
| `ItemDataNotes.txt` | ID 체계, 아이콘 정책, 확인 필요 사항 |
| `DropTable.csv` | 드롭 51행. sourceType,itemId,finalDropChance,minAmount,maxAmount,comment |
| `DropTableNotes.txt` | 출처, 제외 항목, 확인 필요 사항 |

**아이템 ID 체계** (기존 엑셀 번호를 복사하지 않고 프로젝트용으로 구성)
```
20001        화폐 (지푸라기)
21001~21003  체력 회복
22001~22003  수분 회복
23001~23003  허기 회복
24002~24004  특수 소모형 (24001 긴급 탈출 버튼은 폐기됨)
25001        화약 (상점 전용)
26001~26003  판매용 (깃털/돌/거울 조각)
27001~27004  채집 버섯 (빨강/파랑/노랑/검정)
```
ID는 저장 파일에 쓰이므로 이름·아이콘이 바뀌어도 **절대 변경하지 않는다.**

## 9. 확정된 기획 사항

- **긴급 탈출 버튼 폐기.** 탈출은 X키 자유귀환 하나. 기획서에 남은 언급은 무시한다.
- **근접 공격·주먹 폐기.** 무기 내구도 0이면 무기 없음 상태. 대체 아이템을 넣지 않는다.
- **지푸라기(20001)는 상점 매매 제외.** 화폐 전용.
- **초기 지급 장비**: 기관권총 1 + 기관권총 총알 2상자(20알 x 2) + 회복약 3.
- 빨간 버섯 설명의 기관소총은 오타 -> **기관권총**.
- **아이템 그리드 점유는 전부 1칸 고정** (A-2). 다칸 점유는 프로토타입 이후 과제.
- **퀵슬롯 2종** (A-3, A-1): 무기 퀵슬롯 2칸은 장비 무기 슬롯의 링크,
  아이템 퀵슬롯 3칸은 가방 인덱스 매핑. 둘 다 아이템을 담지 않는다.
- **장비 슬롯 4칸** (A-1): 무기 슬롯 2칸 + 방어구 슬롯 2칸(머리 1, 몸통 1). 무기 슬롯 5칸은 폐기.
  **대상 슬롯이 비어 있다는 전제에서만 안착한다.** 차 있으면 거부하며 교환하지 않는다.
  바꿔 끼우려면 UnequipToInventory 로 먼저 빼야 한다.
  무기 두 칸이 모두 비면 어느 칸에 놓든 주 무기(0번)로 들어간다.
  무기와 방어구 모두 획득 시 가방으로만 들어간다. 자동 착용은 없다.
  보호구와 아이템은 전리품·가방 사이를 자유롭게 오가며 어디에도 링크되지 않는다.
- 저장 포맷 **saveVersion 3**. 이전 버전 세이브는 로드하지 않는다. 마이그레이션 없음.

## 10. 확정 대기 (막혀 있는 것)

| 코드 | 내용 | 영향 |
|---|---|---|
| B-1 | 총기 4종 스펙 (탄창/재장전/연사/탄퍼짐/산탄/초기 내구도) | CSV 등록 불가 |
| B-2 | 탄약 인벤 단위 (박스 vs 알) | CSV 등록 불가 |
| B-3 | 창고 칸 수 확정 (현재 120 잠정) | 상수 1개 |
| B-4 | 상자 크기별 맵 배치 계획 | 배치 담당 협의 |
| B-5 | 채집 오브젝트 동작 규칙 | 채집 구현 |
| B-6 | 총알 제작 기능 담당·범위 | 미착수 |
| B-7 | 자동 저장 시점, 슬롯 수 | 호출 연결 |
| C-4 | 판매용 3종 구매 가능 여부 | 상점 담당 |
| C-9 | 깃털·돌·거울 조각 드롭 확률 | 드롭 테이블 3종 |
| — | 보호구 아이템 목록과 효과 | 장비 슬롯은 완성. 넣을 아이템이 CSV에 아직 없음 |

**이 항목들에 해당하는 작업은 착수하지 않는다.** 확정 회신이 오면 그때 진행한다.

**차단(A 등급) 항목은 현재 0건이다.** 위 목록은 전부 예정·확인 등급이라
다음 작업 큐를 그대로 진행하면 된다.

## 11. 다음 작업 큐

1. **채집(재료 파밍)** — 버섯 4종 획득 경로. 데이터·서비스만 (E 판정은 제외 범위)
2. **드롭 테이블 런타임 바인딩** — DropTableDatabase 컴포넌트·테스트 완료. 실제 오브젝트/프리팹 연결은 남음
3. **인벤토리 테스트 UI 검증** — InventoryTest/InventoryTestBench가 NaYeongMin 씬에 존재. 전체 수동 조작 검증은 남음
4. **초기 지급 장비 API** — 총기·탄약 CSV 등록 후
5. (확정 후) 총기 4종 + 탄약 4종 CSV, 드롭 테이블 8행 x 4소스 추가

## 12. 검증 방법

작업 후 반드시 실행:

1. Unity 리프레시 + 컴파일 -> 콘솔 **에러 0** 확인
   (URP Camera 경고, Profiler 버퍼 경고는 무해. 무시)
2. EditMode 테스트 `Birdkov.NaYeongMin.Tests` -> **전부 통과**
3. CSV를 바꿨으면 참조 무결성 확인:
   `DropTableCsvLoader.FindMissingItemIds(entries, catalog)` 가 빈 목록인지

## 13. 변경 이력

| 날짜 | 작업 | 결과 |
|---|---|---|
| 09-09 | 인벤토리·RNG·저장 기반 구축, CSV 14종 | 테스트 23개 |
| 09-10 | 상자 가변 크기 4종, 허브 창고 + 게이트, 신규 아이템 8종 | 테스트 29개 |
| 09-10 | 창고 120칸(원작 참조), 드롭 테이블 CSV + 로더 | 무결성 0건 |
| 09-10 | 기획 회신 반영: 긴급탈출버튼 삭제, 오타 수정 | 아이템 21 / 드롭 51 |
| 09-10 | A-2 1칸 고정, A-3 퀵슬롯 매핑 전환 | saveVersion 2 |
| 09-10 | A-1 장비 슬롯 4칸 + 무기 퀵슬롯 링크 | saveVersion 3 |
| 09-10 | 외부 요인으로 유실된 파일 전량 복구 | 테스트 49개 |
| 09-10 | A-1 추가사항: 장비 착용은 빈 슬롯 전제, 교환 제거 | 테스트 51개 |
| 09-10 | DropTableDatabase 및 연결 README, 테스트 4개 추가. 씬/UI/CSV 내용 변경 없음 | EditMode 55/55 통과 |

## 14. 유실 사고 기록 (2026-09-10)

작업 중 다음 파일이 외부 요인(git 작업으로 추정)으로 삭제되었고 전부 재작성했다.
- 스크립트: LootContainerSize / WarehouseService / DropTableCsvLoader
- 테스트: WarehouseService / LootContainerSize / DropTableCsvLoader / 퀵슬롯
- 데이터: DropTable.csv, 노트 2종, ItemData.csv 내용 전량(헤더만 남아 있었음)
- 문서: README 후반부, 이 파일
- **InventoryDemo 폴더 전체는 복구하지 않았다.** 원본을 그대로 되살릴 수 없고
  장비 슬롯 구조로 어차피 다시 짜야 해서, 다음 작업 큐 3번으로 넘겼다.

재발 방지: 작업 구간이 끝날 때마다 사용자에게 커밋을 요청할 것.

## 15. 드롭 CSV 연결 밑작업 (2026-09-10)

- `Rng/DropTableDatabase.cs`: TextAsset 연결 + Load(IItemCatalog) + Entries 제공.
- CSV 미연결/카탈로그 누락/미등록 아이템 참조 시 실패하고 Entries를 null로 초기화.
- Awake 자동 실행 없음. ItemDatabase.Load() 후 Load(itemDatabase.Catalog)를 호출한다.
- 자세한 연결 절차: `Rng/DropTableDatabase_README.txt`.
- 신규 테스트: `Tests/Editor/DropTableDatabaseTests.cs` 4개. 전체 55개 통과.
- 이번에는 기존 씬·테스트 UI·아이템/드롭 CSV를 수정하지 않음. 검증을 위해 Play 종료.
- 채집 규칙, 총기/탄약 수치, 실제 상호작용 연결은 미착수 유지.
- 기존 82%는 이전 산정값이며 UI/팀 통합까지 검증 완료했다는 의미가 아님.
