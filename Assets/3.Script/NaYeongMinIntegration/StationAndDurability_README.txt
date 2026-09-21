2026-09-18 개인 씬 상점/제작대/총기 내구도 연결

적용 씬: Assets/1.Scene/NaYeongMinScene/NaYeongMin.unity
팀원 Player 코드, ItemData.csv, Test 씬은 수정하지 않음.

TemporaryShopNPC / TemporaryCraftingStation
- Interaction 레이어(10), Collider, ServiceStationInteractable.
- inventoryBench, playerInput, player 참조 필요. crafting 체크 시 제작대.
- 팀원 IInteractable 경로로 F 열기. 다시 F / ESC 닫기, 거리 3m 초과 시 닫기.
- UI: 기존 HONETi 스킨. 상점 패널은 최초 열 때 생성.
- 상점: CSV 가격으로 1개 구매. 가방 슬롯 선택 후 1개 판매 / 총기 1G 수리.
- 버섯/화폐는 거래 제외. 특수 3종은 창고 초기 지급에서만 제외됨.
- 제작: 같은 버섯 5 + 화약 5 -> 탄약 1박스(20발). 가방 우선, 창고 부족분 소비.
- 결과는 가방에 지급. 공간 부족 시 양쪽 재료와 잔탄/내구도 상태 복구.

InventoryTestBench.seedExtendedTestStock
- NaYeongMin 씬에서만 활성화. Start/ResetAll 때 테스트 데이터 지급.
- 창고 31종: 비중첩 3개씩, 중첩 20개씩. 가방 버섯 4종/화약 5개씩, 화폐 200G.
- 지푸라기는 슬롯 대신 화폐 수치. 특수 24002/24003/24004 창고 제외.
- 기존 팀원 DebugInventorySeeder도 활성 상태여서 시작 가방이 꽉 찰 수 있음.
  구매 공간이 필요하면 먼저 창고에 보관. 저장 데이터 로드 시 강제 재지급하지 않음.

WeaponDurabilityBridge: Player 오브젝트에 연결.
- WeaponController.ShotFired 구독. 5.2 발사당 차감 확정(샷건 1회).
- 최대/차감/1G수리: 기관권총100/2/100, 샷건120/3/60, 돌격180/3/50, 스나160/8/40.
- CSV의 기존 내구도 열은 변경하지 않음. 이번 기능은 WeaponDurability 규칙 사용.
- GridSlotData.durabilityDamage에 누적 마모 저장. 기존 JSON에 필드 없으면 0(새 총기).
- ShotFired가 투사체 생성 이전이므로 파괴 처리는 LateUpdate에서 완료.
- 팀원에 현재 슬롯 공개 API가 없어 equippedWeaponSlot 필드를 읽기 전용 reflection으로 참조.
  팀원이 필드명을 변경하면 이 브리지 수정 필요. 공개 슬롯 API 추가 후 교체 권장.

검증: StationTests 포함 개인 파트 EditMode 128개 실행, 실패 보고 없음.
PlayMode API 호출: HONETi UI, 제작/구매, F 닫기, 실제 발사 및 내구도0 장착 해제 확인.
키보드/마우스로 전체 동선 통합 테스트는 별도.
기존 적 설정 오류 확인: EnemyBulletPool.bulletPrefab 누락, EnemyShot.shotEffect 미연결.
위 적 설정은 이번 작업에서 변경하지 않음.

[2026-09-18 재점검 후 수정]
- WeaponDurabilityBridge: reflection 제거. 팀원 공개 API 만 사용.
  InventoryTestBench.SelectedWeaponIndex -> WeaponInventoryBridge.GetWeaponSlotData(index),
  EquippedWeaponItemId 로 검증. 두 무기 칸이 같은 총기면 0,1 순서로 탐색.
- WeaponDurabilityBridge.Update: 테스트벤치 초기화/불러오기로 PlayerInventoryData 가 새 객체가 되면
  팀원 링크(1회성)가 갱신되지 않아 재장전 탄약이 0 으로 보이던 문제. SetPlayerInventoryData 로 다시 연결.
- InventoryTestBench.ResetAll: seedExtendedTestStock 이 켜진 씬에서는 기본 가방 지급을 건너뛴다.
  기존에는 가방이 25/25 로 꽉 차서 제작 결과와 구매 아이템이 들어갈 칸이 없었다. 지금은 빈칸 10.
- 씬: EnemyPool.bulletPrefab/particlePrefab 을 팀원 Text_KTS 씬의 현행 프리팹(KTS/EnemyBullet1, KTS/GameObject)에 연결.
  기존 참조는 삭제된 프리팹이라 MissingReferenceException 이 났다. 팀원 코드/프리팹은 수정하지 않았다.
- 씬: 아이콘 바인딩에서 스프라이트가 비어 있던 26003 항목 제거.

[내구도 기준 - 기획서 5.2 가 정본]
- 기획서: 기관권총 100/2/1G당100, 샷건 120/3/60, 돌격소총 180/3/50, 스나이퍼 160/8/40. 상점가 2/3/5/10G.
- WeaponDurability 구현값이 기획서와 일치한다. ItemData.csv 의 maxDurability/durabilityCostPerHit/
  repairAmountPerCurrency 열은 구버전(50/50/90/120, 2/3/5/8, 10/25/30/30)이라 사용하지 않는다.
  CSV price 열(2/3/5/10)만 기획서와 같아서 상점이 그대로 쓴다. CSV 는 수정하지 않았다. 팀원 확인 필요.

[검증 2026-09-18]
- EditMode 128/128 통과.
- Play: 가방 빈칸 10 확인. 제작(빨간버섯5+화약5 -> 11001 1박스) 성공. 구매(돌격소총 5G, 200->195G) 성공.
- Play: 돌격소총 30 발 발사 -> 마모 90 (발당 3, 기획서 일치).
- Play: 저장 -> 초기화 -> 불러오기 왕복에서 durabilityDamage 90, remainingRounds 7 모두 보존.
- 미검증: 실제 키보드/마우스 동선, 내구도 0 파괴 후 빈손 전환(에디터 비포커스에서 Time.time 이 멈춰
  재장전이 끝나지 않아 자동 검증 불가). 손조작 확인 필요.
- 남은 문제: Test.unity 의 EnemyPool 도 같은 삭제된 bulletPrefab 을 참조한다. 통합 씬이라 손대지 않았다.
  ItemData.csv 26001(깃털), 26003(거울 조각) iconKey 가 비어 있다. 임의로 채우지 않았다.

[2026-09-18 2차 - CSV 정본화 / 아이콘 / UI 하이어라키 이관]
- ItemData.csv 내구도 열을 기획서 5.2 로 교체.
  기관권총 100/2/1G당100, 샷건 120/3/60, 돌격소총 180/3/50, 스나이퍼 160/8/40.
  WeaponDurability 는 이제 WeaponDurability.Catalog(= ItemData.csv)를 먼저 읽는다.
  카탈로그가 없으면 기획서 5.2 기본값으로 동작한다. 수치 정본은 CSV 한 곳뿐이다.
- ItemData.csv iconKey 35행 전부 실제 경로로 교정. 폴더가 4.Sprite/ICONs/... 로 바뀌어 전부 깨져 있었다.
  비어 있던 5건 채움: 24002 알람시계=icon_hourglass_LP, 24003 위고비=icon_potion-globe_LP,
  24004 알로에즙=icon_drink-coconut_LP, 26001 깃털=icon_cotton_LP(대체 이미지), 26003 거울 조각=Glass_Shard.
  깃털 전용 이미지가 프로젝트에 없어 솜 아이콘으로 임시 대체했다. 기획팀 확인 필요.
  Glass_Shard.png 는 Default/Multiple 이라 스프라이트로 안 잡혀서 Sprite/Single 로 재임포트했다.
- InventoryTestBench 는 인스펙터 아이콘 바인딩이 없는 아이템을 CSV iconKey 경로에서 직접 읽는다(에디터 한정).
  기획팀이 CSV 의 iconKey 만 고쳐도 아이콘이 붙는다. 빌드에서는 인스펙터 바인딩만 쓴다.

[상점/제작 UI - 하이어라키 이관]
- 코드 생성(BuildShop/BuildCraftPanel)은 참조가 비어 있을 때만 쓰는 예비 경로로 남겼다.
  실제로는 씬의 Root/ShopPanel, Root/CraftPanel 을 쓴다. 전부 인스펙터에서 교체 가능하다.
- 참조: InventoryTestBench.ui 의 shopPanel / shopCurrency / shopRowBackgrounds / shopRowIcons /
  shopRowLabels / shopRowButtons / shopPrev / shopNext / shopDetailIcon / shopDetail / shopBuy /
  shopSell / shopRepairBag / shopRepairWeapon1 / shopRepairWeapon2 /
  craftRowBackgrounds / craftRowIcons / craftDetailIcon / craftDetail / craftConfirm.
- 구조(예시 이미지 기준): 주황 타이틀바 + 좌측 목록(행 = 아이콘 + 이름 + 가격/재료) + 아래 상세 박스 + 큰 실행 버튼.
  행 색은 코드가 칠한다. 가능 = 녹색, 불가 = 적갈색, 선택된 행은 밝은 색.
  기획팀이 색을 바꾸려면 InventoryTestBench.Services.cs 의 RowReady / RowBlocked 상수를 고친다.
- 상점은 한 쪽에 5행. 행 클릭 = 선택, 하단 '구 매' 버튼으로 구매한다. 이전/다음으로 쪽을 넘긴다.
- 제작은 버섯 4행. 행 클릭 = 선택, 하단 '제 작' 버튼으로 제작한다.
- 행 수를 늘리려면 씬에서 행을 복제하고 ui 배열에 추가하면 된다. 코드는 배열 길이를 그대로 따른다.

[검증 2026-09-18 2차]
- EditMode 128/128 통과.
- Play: 상점 열기 -> 행 선택 -> 구매(195->190G), 이전/다음 쪽 이동, 가방 슬롯 선택 표시,
  비매품(버섯) 판매 거부, 장착1 수리 1G -> 돌격소총 90 -> 140 (기획서 1G당 50).
- Play: 제작 열기 -> 3행 선택 -> 제작, 노란 버섯 25->20 화약 25->20 돌격소총 총알 50->51박스.
- Play: 상세 패널이 CSV 값을 표시(돌격소총 최대 180, 1G당 50).
- 아이콘: 목록/상세 모두 표시 확인. spriteMap 35건 전부 연결.
- 미검증: 실제 키보드/마우스 동선, 내구도 0 파괴 후 빈손 전환.

[2026-09-18 3차 - 레이아웃 단순화]
- ShopPanel / CraftPanel 을 화면 중앙(x 500, 600x640)으로 옮겼다.
  상점이나 제작대를 열면 오른쪽 창고도 같이 열린다. 왼쪽 장비/가방 - 가운데 상점·제작 - 오른쪽 창고.
  창고를 같이 여는 것은 InventoryTestBench.Services.cs 의 OpenWarehouseBeside 한 곳이다.
- 상점 하단 버튼 4개(판매/선택수리/장착1/장착2)를 2개(1개 판매 / 수리 1G)로 줄였다.
  상점이 열려 있을 때는 가방 칸뿐 아니라 장비 칸도 눌러서 고를 수 있다. 고른 칸 하나가 판매·수리 대상이다.
  판매는 가방 칸만 된다.
- 버튼 크기: 구매 560x48, 제작 560x64, 이전/다음 270x40, 판매/수리 270x34. 목록 행 560x52(제작 54).
- 제작 목록 행은 한 줄로 줄였다. '빨간 버섯 25 / 5   →   기관권총 총알'. 화약 수량은 아래 상세 칸에 있다.

[검증 2026-09-18 3차]
- EditMode 128/128 통과.
- Play: 상점 열기 -> 장비/가방/상점/창고 4개 패널 동시 표시 확인.
- Play: 구매 200->195G, 장비 0번 칸 클릭 후 수리 1G -> 돌격소총 90 -> 140.
- Play: 제작대 열기 -> 창고 같이 열림, 2행 제작 파란 버섯 25->20, 샷건 총알 50->51박스.

[2026-09-18 4차 - 덕코프식 레이아웃]
- 모든 UI 구조를 Assets/2.Model/Prefabs/NaYeongMin/Ui/InventoryUI.prefab 안으로 옮겼다.
  이전에는 씬 인스턴스에만 있던 오버라이드였다. 이제 프리팹 하나만 고치면 된다.
- 왼쪽(x 24, 460x640): ShopPanel / CraftPanel / LootPanel / MapChestPanel. 서로 배타적이다.
- 오른쪽(x 1116, 460x640): InventoryScroll 하나. 세로 스크롤 안에 장비 - 가방 - 창고 순으로 쌓인다.
  Content 에 VerticalLayoutGroup + ContentSizeFitter 가 있고 세 패널에 LayoutElement 가 붙어 있다.
  높이를 바꾸려면 각 패널의 LayoutElement preferredHeight 를 고친다.
- 창고 패널의 안쪽 스크롤은 없앴다. 바깥 스크롤 하나로 가방에서 창고까지 이어서 내려간다.
  창고가 켜지면 Content 높이 2758, 꺼지면 656 이다.
- 상점/제작대를 열면 창고도 같이 켜진다(OpenWarehouseBeside).
  켜고 끌 때 스크롤 길이를 다시 재려고 RebuildInventoryColumn 에서 ForceRebuildLayoutImmediate 를 부른다.
  이게 없으면 창고를 켜도 스크롤이 늘어나지 않는다.

[검증 2026-09-18 4차]
- EditMode 128/128 통과.
- Play: 인벤토리만 = Content 656, 상점 열기 = 2758, 창고가 가방 아래 -652 위치에 붙음.
- Play: 맨 아래로 스크롤하면 Warehouse_119 까지 화면 안에 들어온다.
- Play: 상점 닫으면 Content 656 으로 돌아온다.
- Play: 구매 200->195G, 다음 쪽 이동, 장비 0번 칸 선택 후 수리 1G -> 90 -> 140.
- Play: 제작대 4행 선택 -> 제작, 검은 버섯 25->20, 스나이퍼 총알 50->51박스.

[2026-09-18 5차 - 좌우 교체]
- 왼쪽(x 24): InventoryScroll. 장비 - 가방 - 창고 세로 스크롤.
- 오른쪽(x 1116): ShopPanel / CraftPanel / LootPanel / MapChestPanel. 서로 배타적.
- 위치는 InventoryUI.prefab 의 각 패널 anchoredPosition.x 값만 바꾼 것이다(24 <-> 1116).
- Play 확인: 구매 200->195G, Content 높이 2758, 창고 -652 위치 유지.

[2026-09-18 6차 - 목록 스크롤화 / 버튼 최소화]
- 상점 목록의 이전/다음 버튼을 없애고 세로 스크롤 목록으로 바꿨다.
  ShopPanel/ListScroll(RectMask2D + ScrollRect) 안의 ListContent 에 ShopRow_0 ~ 29 가 들어 있다.
  상품이 늘면 ListContent 높이를 늘리고 행을 복제한 뒤 ui.shopRow* 배열에 추가하면 된다(행 간격 56).
- 제작대도 같은 구조. CraftPanel/ListScroll/ListContent 에 CraftRow_0 ~ 3(행 간격 58).
- 남은 버튼: 상점 = 구 매 / 1개 판매 / 수리 1G 3개. 제작 = 제 작 1개. 나머지는 목록 행 자체가 버튼이다.
- 보유 화폐 문구에서 쪽 표기를 뺐다. 스크롤이라 쪽이 없다.
- ui.shopPrev / ui.shopNext 는 비워 뒀다. 코드가 null 을 건너뛴다.

[검증 2026-09-18 6차]
- EditMode 128/128 통과.
- Play: 상품 30건이 한 목록에 모두 표시(0 기관권총 ~ 29 거울 조각). 목록 content 1680 / 보이는 창 300.
- Play: 29행(거울 조각 7G) 선택 후 구매 200 -> 193G.
- Play: 제작 3행 선택 후 제작, 노란 버섯 25 -> 20, 돌격소총 총알 50 -> 51박스.
- Play: 버튼 수 = 상점 33(행 30 + 3), 제작 5(행 4 + 1).

[2026-09-18 7차 - 색까지 인스펙터로]
- InventoryTestBench 에 colors(InventoryUiColors) 묶음을 추가했다. 전부 인스펙터에서 바꾼다.
  칸 배경: slotEmpty / slotFilled / slotEquipment / slotWeaponQuick / slotSelectedWeapon / slotCraftMaterial
  목록 행: rowReady / rowBlocked / rowReadySelected / rowBlockedSelected / craftLabelReady / craftLabelBlocked
- Services.cs 의 RowReady 등 static 상수는 없앴다.

[기획팀이 코드 없이 바꿀 수 있는 것]
- 패널 위치/크기/순서, 버튼 위치·크기·글자, 폰트, 글자 크기, HONETi 스킨 스프라이트
- 목록 행 개수(행 복제 + ListContent 높이 + ui 배열에 추가), 행 간격
- 장비/가방/창고 세로 순서와 각 높이(LayoutElement preferredHeight)
- 아이템 아이콘(ItemData.csv 의 iconKey 또는 icons 바인딩)
- 위의 색 전부
- 고정 안내 문구(타이틀, 설명 라벨)

[아직 코드에 남아 있는 것]
- 값이 들어가는 문구의 형식. 예: '기관권총  2G', '빨간 버섯 25 / 5 → 기관권총 총알'.
  매 프레임 코드가 덮어쓰기 때문에 하이어라키의 글자는 무시된다.
- 전리품/맵 상자 격자. 상자 규격이 3x3, 4x2, 5x3 으로 바뀌므로 칸을 코드가 만든다(BuildLootGrid).
- 가방 25칸, 장비 4칸, 창고 120칸의 칸 수 자체. InventorySettings 의 상수다.
- 결과 메시지 문구(구매 완료, 수리 불가 등).
- 하이어라키 참조가 비었을 때 쓰는 예비 코드 생성 경로(BuildUi / BuildShop / BuildCraftPanel).

[검증 2026-09-18 7차]
- EditMode 128/128 통과.
- Play: colors.rowBlocked 를 빨강으로 바꾸고 보유 3G 로 낮추니 5G/10G 행이 바로 빨강으로 바뀌었다.

[상인/제작대 오브젝트 붙이는 법]
1. 오브젝트에 ServiceStationInteractable 을 붙인다.
2. Collider 를 붙인다(모델에 이미 있으면 그대로).
3. 레이어를 Interaction(10) 으로 바꾼다.
4. 제작대면 crafting 체크. 상점이면 끈다.
5. 프롬프트를 띄우려면 자식 오브젝트 이름을 InteractionLabel 로 두거나 prompt 칸에 직접 넣는다.
inventoryBench / playerInput / player 는 Awake 에서 씬에서 자동으로 찾는다. 비워 둬도 된다.
Collider 나 레이어가 빠지면 Awake 에서 경고 로그가 뜬다.
확인: 빈 큐브에 스크립트만 붙이고 레이어만 바꿔서 상점/제작 둘 다 열리는 것을 Play 에서 확인했다.
