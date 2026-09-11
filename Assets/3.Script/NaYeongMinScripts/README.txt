NaYeongMin 담당 시스템 연결 지침

1. ItemDatabase 컴포넌트의 itemDataCsv에 Assets/DataTeble/NaYeongMinCsvData/ItemData.csv를 연결한다.
2. UI는 PlayerInventoryService만 호출한다. 가방 25칸, 장비 4칸, 아이템 퀵슬롯 3칸(가방 인덱스 매핑)을 PlayerInventoryData가 생성한다. 상세는 15·16항.
3. 장비 슬롯은 0 주 무기 / 1 보조 무기 / 2 머리 / 3 몸통이다. 무기 퀵슬롯 2칸은 0·1을 그대로 비추는 링크다. 착용 전 무기와 방어구는 가방에 보관한다. 상세는 15·16항.
4. 적 사망 또는 상자 개봉 시 LootRuntime.Spawn 또는 Roll을 호출한다. 결과 칸 수는 컨테이너 크기를 따르며 초과 당첨분은 폐기된다. 상세는 12·13항.
5. Assets/2.Model/Prefabs/NaYeongMin/LootRuntime.prefab 하나만 씬에 올리면 CSV 로드와 풀 초기화까지 끝난다. 풀은 LootDropObject.prefab을 30개 선생성한다. 전리품이 비면 NotifyContentsChanged를 호출해 풀로 반환한다. 외형·콜라이더는 아트/상호작용 연결 시 추가한다.
6. 플레이어 사망 이벤트에서 PlayerInventoryService.ClearOnDeath를 호출한다. 창고 데이터는 전달하지 않으므로 유지된다.
7. JsonSaveSystem.Save/TryLoad를 사용한다. 주 파일 손상 시 .backup.json을 읽어 복구한다.
8. 씬, 플레이어, 전투, UI와 직접 참조하지 않도록 작성됨. 연결 어댑터는 각 담당자 머지 후 추가한다.
9. 아이템 ID 체계는 Assets/DataTeble/NaYeongMinCsvData/ItemDataNotes.txt에 확정 기록되어 있다. 1xxxx 장비류(10 총기 / 11 탄약 / 12 방탄복 / 13 헬멧), 2xxxx 소모품과 재화. 탄약 ID는 무기 ID + 1000이다. ID는 저장 파일에 쓰이므로 변경하지 않는다. CSV에는 비무기 21종이 등록되어 있고 총기·탄약·방어구는 수치 확정 대기라 미등록이다.
10. 드롭 풀은 기본 30개이며 고갈 시 Rent가 null을 반환한다. 호출 측은 null을 확인해야 한다. 자동 확장·기존 전리품 강제 회수는 하지 않는다. 추후 initialSize 설정을 늘려 확장할 수 있다.

[2026-09-10 갱신] 창고 / 상자 가변 크기 / 드롭 테이블 / 장비 슬롯

11. 허브 창고: WarehouseService 를 사용한다.
    생성   : new WarehouseService(itemCatalog, saveData.warehouseData)
    열기   : 거점 창고 오브젝트의 상호작용(E) 담당이 Open() 호출. 창 닫을 때 Close() 호출.
    이동   : Store(가방 -> 창고), Withdraw(창고 -> 가방), MoveWithin(창고 내부 정렬)
    게이트 : Open() 전에는 세 메서드 모두 DestinationRejected 를 반환한다.
    UI     : 좌측 Warehouse, 우측 PlayerInventoryData.inventory 배치.
    크기   : InventorySettings.WarehouseWidth x WarehouseHeight = 10 x 12 = 120칸.
             기획서에 규정이 없어 원작(Escape from Duckov) 만렙 용량 참조로 잠정 확정.
    사망   : ClearOnDeath 는 창고를 건드리지 않는다. 창고 보관분은 유지된다.

12. 상자 크기: LootContainerSize 프리셋. Box2x4(8) / Box3x3(9) / Box3x5(15) / Box4x1(4).
    DropRoller.Roll(entries, sourceType, sizePreset) 로 크기를 지정한다.
    적 사망 오브제는 인자를 생략하면 Box2x4(8칸)이며 기획서 전리품 8칸 규칙과 같다.
    컨테이너 칸 수를 넘긴 당첨분은 생성하지 않고 파기한다.
    LootDropObject.SetLoot 은 원본의 크기 프리셋을 그대로 따라간다.
    UI 파밍 창은 LootContainerData.loot 의 width/height 를 읽어 가변 그리드로 그린다.

13. 드롭 테이블 CSV: Assets/DataTeble/NaYeongMinCsvData/DropTable.csv (51행)
    로드 : DropTableCsvLoader.Parse(csvText) -> List<DropTableEntry>
    검증 : DropTableCsvLoader.FindMissingItemIds(entries, itemCatalog) 가 빈 목록이어야 한다.
           게임 시작 시 1회 호출해 참조 무결성을 확인할 것.
    시드 : SystemRandomSource(seed) 로 고정 시드를 주입하면 결과가 재현된다.
    상세는 DropTableNotes.txt 참고.

14. CSV 아이템 21종. 기획서 2026-09-09판 기준 비무기 전량.
    20001 화폐 / 21001~21003 체력 / 22001~22003 수분 / 23001~23003 허기 /
    24002~24004 특수 / 25001 화약 / 26001~26003 판매용 / 27001~27004 채집 버섯.

15. 장비 슬롯 4칸 (A-1 확정) - 무기 슬롯 5칸은 폐기되었다
    구성 : EquipmentSlots.PrimaryWeapon(0) / SecondaryWeapon(1) / Helmet(2) / Armor(3)
    보관 : 착용 전 무기와 보호구는 모두 가방(25칸)에 둔다. 별도 보관 슬롯이 없다.
    착용 : playerInventoryService.EquipFromInventory(playerData, inventoryIndex, equipmentSlotIndex)
           - 대상 장비 슬롯이 비어 있다는 전제에서만 안착한다.
             슬롯이 차 있으면 DestinationRejected 이며 교환하지 않는다.
             바꿔 끼우려면 UnequipToInventory 로 먼저 빼야 한다.
           - 무기 슬롯 두 칸이 모두 비어 있으면 어느 칸에 놓든 주 무기(0번)로 들어간다.
           - 슬롯 종류가 맞지 않으면 DestinationRejected.
           - 무기와 방어구 모두 획득 시에는 가방으로만 들어간다. 자동 착용은 없다.
    해제 : UnequipToInventory(playerData, equipmentSlotIndex, inventoryIndex)
    파괴 : DestroyEquipped(playerData, equipmentSlotIndex)
           내구도 0으로 파괴된 장비를 슬롯에서 없앤다. 대체 아이템을 넣지 않는다.
    자동 착용은 하지 않는다. 획득은 항상 가방으로만 들어간다.

16. 퀵슬롯 - 두 종류다
    무기 퀵슬롯 2칸 (Alpha 1,2) : 장비 무기 슬롯 0,1 을 그대로 비추는 링크다.
        별도 데이터도 지정 절차도 없다. 장비 슬롯이 바뀌면 자동 반영된다.
        조회 : TryGetWeaponQuickSlot(playerData, weaponQuickIndex, out equipSlotIndex, out item)
    아이템 퀵슬롯 3칸 (Alpha 3,4,5) : 가방 슬롯 인덱스를 가리키는 매핑이다.
        아이템은 가방에 그대로 있고 퀵슬롯은 참조일 뿐이다.
        데이터 : PlayerInventoryData.itemQuickSlotIndices (int[3], -1 은 미지정)
        지정 : AssignItemQuickSlot(playerData, quickIndex, inventoryIndex)
               소모형과 특수 소모형만 지정된다. 같은 가방 칸을 다른 퀵슬롯에 다시 걸면
               이전 퀵슬롯은 자동 해제된다.
        해제 : ClearItemQuickSlot(playerData, quickIndex)
        조회 : TryGetItemQuickSlot(playerData, quickIndex, out inventoryIndex, out item)
        정리 : SanitizeItemQuickSlots(playerData)
               AddToInventory 와 Move 안에서 자동 호출된다.
               아이템이 소모되어 사라지면 그 퀵슬롯은 즉시 비워진다.
    PlayerContainerType 은 Inventory / Equipment 두 값뿐이다.

17. 저장 포맷 saveVersion 3
    2: 퀵슬롯이 컨테이너에서 가방 인덱스 매핑으로 변경.
    3: 무기 슬롯 5칸이 장비 슬롯 4칸으로 변경.
    이전 버전 세이브는 로드하지 않는다. 마이그레이션은 만들지 않았다.

18. 확정된 기획 사항
    - 긴급 탈출 버튼 폐기. 탈출은 X키 자유귀환 하나. 기획서에 남은 언급은 무시한다.
    - 근접 공격과 주먹 폐기. 무기 내구도 0이면 [무기 없음] 상태.
    - 지푸라기(20001)는 상점 매매 목록에서 제외한다.
    - 초기 지급 장비: 기관권총 1 + 기관권총 총알 2상자(20알 x 2) + 회복약 3.
    - 아이템 그리드 점유는 전부 1칸 고정. 다칸 점유는 프로토타입 이후 과제.

19. 아직 대기 중인 확정 사항
    - 총기 4종 스펙, 탄약 인벤토리 단위
    - 깃털/돌/거울 조각의 드롭 확률 (10.1에는 개수가 있으나 10.3 확률표에 없음)
    - 채집 동작, 총알 제작, 저장 시점, 창고 칸 수 확정
    - 보호구 아이템 목록과 효과 (장비 슬롯은 만들었으나 넣을 아이템이 아직 없음)

20. 아이콘: 로우폴리 아이콘 에셋을 실험적으로 사용 중이며 추후 교체될 수 있다.
    교체 시 CSV 의 iconKey 열만 갱신하면 되고 코드 수정은 없다.
    미확보 항목은 임의 대체하지 않았다. 상세는 ItemDataNotes.txt 참고.

[2026-09-11 갱신] 아이템 데이터 필드 정리와 ID 체계 확정

23. ItemData 필드 정리
    제거 : attacksPerSecond  -> fireRate 와 중복이었고, 로더의
           GetAny("attacksPerSecond", "fireRate") 폴백이 값을 조용히 복제하고 있었다.
           공격속도는 fireRate 단일 필드로 통일한다. 단위는 발/초이며
           연사 간격이 필요하면 1f / fireRate 로 계산한다.
    제거 : ItemType.Other    -> 코드와 CSV 양쪽에서 쓰이지 않았다.
    유지 : weight, rarity, defense 는 현재 읽는 코드가 없지만 유지하기로 확정했다.
    CSV 열 29개.

24. 확정 대기(B-1) 상태로 남겨 둔 필드
    attackDamage / range / fireRate / reloadSpeed / magazineSize / projectileSpeed /
    automatic / pelletCount / maxSpread / maxDurability / durabilityCostPerHit /
    repairAmountPerCurrency
    전부 기획에 수치를 요청해 둔 항목이다. 확정되면 CSV 열만 채우면 되고 코드 수정은 없다.
    기획서 10.2 의 공격속도 열 이름(초/횟수)과 해설(1초당 발사 횟수)이 서로 역수라
    단위 확정이 함께 필요하다.

25. 아이템 ID 체계 (확정)
    1xxxx 장비류
      10001~10004 총기 4종   10001 기관권총 / 10002 샷건 / 10003 돌격소총 / 10004 스나이퍼
      11001~11004 탄약 4종   무기 ID + 1000. 10002 샷건 -> 11002 샷건 총알
      12001~12009 방탄복     몸통. equipmentSlotType = Armor
      13001~13009 헬멧       머리. equipmentSlotType = Helmet
    2xxxx 소모품과 재화
      20001 화폐 / 21001~21003 체력 / 22001~22003 수분 / 23001~23003 허기 /
      24002~24004 특수 / 25001 화약 / 26001~26003 판매용 / 27001~27004 채집 버섯
    버섯 하위 자리가 총기 인덱스와 같다. 27001 빨강 -> 10001 기관권총 / 11001 총알.
    제작 레시피(버섯 5 + 화약 5 = 1박스)도 27000+n -> 11000+n 으로 유도된다.
    ID 는 저장 파일에 쓰이므로 이름이나 아이콘이 바뀌어도 변경하지 않는다.
    상세는 ItemDataNotes.txt 참고.

26. 테스트 벤치 임시 아이템
    990001 임시 기관권총 -> 10001 / 990002 임시 샷건 -> 10002
    990101 임시 헬멧     -> 13001 / 990102 임시 방탄복 -> 12001
    InventoryTestBench 가 메모리로만 추가하며 CSV 에는 없다.
    실제 아이템이 등록되면 정식 ID 로 교체한다.
