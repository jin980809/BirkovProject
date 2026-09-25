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

[2026-09-19 - 칸 선택 표시 / 좌클릭 상세 / 우클릭 메뉴]
- 가방 칸을 좌클릭해도 소모품이 바로 쓰이지 않는다. 칸이 선택되고 화면 가운데에 상세 창이 뜬다.
- 선택된 칸은 colors.slotSelected 색으로 표시된다. 상점에서 판매/수리 대상을 고른 칸도 같은 색이다.
- 우클릭하면 마우스 자리에 메뉴가 뜬다. 사용 / 버리기.
  사용은 가방의 소모품·특수 아이템에만 보이고, 버리기는 가방과 창고에만 보인다.
- 새 파일: InventoryTestBench.ItemMenu.cs. TestSlotView 에 우클릭 전달만 추가했다.
- 하이어라키: Root/ItemDetailPanel(TitleBar/Title, Icon, Body, Button_닫기), Root/ContextMenu(Button_사용, Button_버리기).
  이름을 그대로 두면 ui 참조를 비워 둬도 Bind 때 자동으로 찾는다. 위치/크기/색은 인스펙터에서 바꾸면 된다.

[검증 2026-09-19]
- EditMode 128/128 통과.
- Play: 가방 좌클릭 -> 수량 그대로, 상세 창 열림(제목/아이콘/무게/종류/설명), 선택 칸 녹색.
- Play: 우클릭 -> 재료는 사용 버튼 숨김, 버리기만 표시. 버리기 누르니 칸이 비워졌다.
- Play: 회복약 우클릭 -> 사용, 체력 40 -> 46, 수량 3 -> 2.
- Play: 상점 열고 가방 칸 클릭 -> 같은 녹색으로 표시되고 상세에 '선택: 회복약' 이 뜬다.

[2026-09-19 2차 - 아이템 이동은 더블클릭]
- 창고 / 전리품 / 맵 상자 칸: 한 번 클릭 = 선택 + 상세, 두 번 클릭 = 가방으로 가져오기.
- 가방 칸: 한 번 클릭 = 선택 + 상세, 두 번 클릭 = 열려 있는 창고나 상자로 보내기.
  보낼 곳 우선순위는 창고 > 전리품/맵 상자. 둘 다 닫혀 있으면 아무 일도 없고 안내만 뜬다.
- 상점/제작대를 열면 창고가 같이 열리므로 그 상태에서도 더블클릭으로 창고와 주고받는다.
  상점이 열려 있을 때 가방 한 번 클릭은 그대로 판매/수리 대상 선택이다.
- 드래그로 옮기는 기존 방식은 그대로 둔다.

[검증 2026-09-19 2차]
- EditMode 128/128 통과.
- Play(상점 열린 상태): 창고 한 번 클릭 -> 수량 1 그대로, 선택 녹색, 상세 열림.
- Play: 창고 두 번 클릭 -> 창고 1 -> 0, 가방 사용 칸 15 -> 16.
- Play: 가방 한 번 클릭 -> 수량 5 그대로. 두 번 클릭 -> 5 -> 0, '5개 보냄'.

[2026-09-19 3차 - 상자 칸 누적 버그 / 우클릭 메뉴 위치]
- 상자를 열 때마다 전리품 칸이 지워지지 않고 쌓였다. 맵 상자 9 -> 18 -> 27, 전리품 8 -> 16 -> 24.
  쌓인 칸이 새 칸을 덮어서 상자를 열어도 내용이 안 보이는 것처럼 됐다.
  원인: BuildLootGrid 가 views 목록만 보고 지웠고 Destroy 가 한 프레임 늦어 그리드 자식이 남았다.
  수정: ClearLootCells 로 LootGrid 와 MapChestGrid 의 자식을 직접 떼어내고 지운다.
- 우클릭 메뉴가 엉뚱한 자리에 떠서 안 보였다. Root 피벗이 가운데(0.5, 0.5)인데
  메뉴 앵커는 좌상단(0, 1)이라 좌표가 800, 450 만큼 어긋났다.
  수정: ScreenPointToWorldPointInRectangle 로 월드 좌표를 그대로 넣고 화면 안으로 clamp 한다.

[검증 2026-09-19 3차]
- EditMode 128/128 통과.
- Play: 맵 상자 / 전리품을 3번씩 번갈아 열어도 MapChestGrid 9, LootGrid 8 로 고정. 반대쪽은 0.
- Play: 창고(Storage) 도 정상 열림.
- Play: 우클릭 -> 메뉴가 마우스 위치에 뜨고 사용/버리기 표시, 상세창과 선택 녹색도 같이 동작.

[2026-09-19 4차 - 우클릭 메뉴 빈 박스 / 상자 오인]
- 상자 칸을 우클릭하면 사용도 버리기도 숨겨져서 검은 빈 박스만 떴다.
  수정: 전리품/맵 상자 칸에서도 버리기를 쓸 수 있게 하고, 보일 버튼이 하나도 없으면 메뉴를 띄우지 않는다.
  ContextMenu 에 VerticalLayoutGroup + ContentSizeFitter 를 붙여 버튼 수에 맞게 박스가 줄어든다.
  버튼 두 개면 84, 하나면 46. 버튼 위치는 코드가 아니라 레이아웃이 잡는다.
- 상자 안 아이템을 버리면 NotifyLootChanged 로 상자 상태도 갱신한다.

[상자로 보이지만 상자가 아닌 것]
- 씬의 큰 노란/회색 박스 Cube (1) ~ Cube (4) 는 Layer 7 Obstacle 이다. 엄폐물이라 F 로 열리지 않는다.
- 실제 상호작용 대상은 Layer 10 Interaction 인 6개뿐이다.
  StorageInteraction(-3, 0, 0) / LootInteraction(3, 0, 0) / MapChestInteraction(3, 0.1, 3)
  TemporaryShopNPC(-3.83, 0.6, -3) / TemporaryCraftingStation(3, 0.6, -3) / 적 드랍 LootDropObject
- 세 상자 모두 코드로 열어 확인했고 정상 동작한다. 엄폐물을 상자로 쓰려면
  그 오브젝트에 InventoryWorldContainer + WorldContainerInteractable 을 붙이고 레이어를 Interaction 으로 바꿔야 한다.

[검증 2026-09-19 4차]
- EditMode 128/128 통과.
- Play: 상자 칸 우클릭 -> 버리기만 표시, 박스 높이 46. 버리기 누르니 23001 -> 비었음.
- Play: 상자 빈 칸 우클릭 -> 메뉴가 뜨지 않는다.
- Play: 가방 소모품 우클릭 -> 사용 + 버리기, 박스 높이 84.

[2026-09-19 5차 - 우클릭 사용 확대 / 창고 자동 스크롤]
- 우클릭 사용 버튼을 가방뿐 아니라 창고와 상자 칸에서도 쓸 수 있게 했다.
  가방 밖에서 누르면 한 개만 가방으로 옮긴 뒤 그 자리에서 사용한다. 가방이 꽉 차면 안내만 뜬다.
- StorageInteraction(보관상자)은 kind=Storage, dropSettings 가 비어 있는 허브 창고다. 전리품이 들어 있지 않다.
  창고는 오른쪽 세로 목록의 맨 아래에 있어서 열어도 화면에 안 보였다.
  수정: 창고를 열면 ScrollToWarehouse 로 스크롤을 창고 위치까지 내린다. 상점/제작대로 열 때도 같다.
- 상자 안 전리품을 보려면 kind 가 Loot 이나 MapChest 인 상자를 열어야 한다.
  LootInteraction(3,0,0) 은 Test_Loot_SelectedItems, MapChestInteraction(3,0.1,3) 은 Test_MapChest_SelectedItems 를 쓴다.

[미검증]
- 위 두 가지는 코드 수정까지만 마쳤고 Play 검증은 못 했다.
  에디터가 스크립트 컴파일 중 상태에서 멈춰 Play 진입이 되지 않았다.
  Unity 창을 한 번 클릭해 포커스를 준 뒤 컴파일이 끝나면 다시 확인해야 한다.

[2026-09-19 6차 - 창고 단독 열기 / 버튼 글자]
- 창고만 열면(F, 보관상자) 전리품 상자와 같은 자리인 오른쪽 WarehouseSoloScroll 에 뜬다.
  이때 왼쪽 세로 목록에는 장비와 가방만 남는다(Content 높이 656).
- 상점이나 제작대를 열면 창고가 왼쪽 세로 목록에 통합된다(Content 높이 2758). 오른쪽에는 상점/제작 패널이 뜬다.
- 같은 WarehousePanel 을 SetParent 로 옮긴다. 슬롯 120칸과 데이터, 참조는 그대로다.
  MoveWarehouse(true) 가 오른쪽 단독, MoveWarehouse(false) 가 왼쪽 통합이다.
  ui.warehouseSoloScroll / warehouseSoloContent / inventoryColumn 은 비워 둬도 이름으로 자동 연결된다.
- 사용 / 제작 / 구매 버튼 글자를 흰색 볼드로 바꿨다. 밝은 녹색 배경에서 잘 보인다.

[검증 2026-09-19 6차]
- EditMode 128/128 통과.
- Play: 보관상자 -> 우측 SoloScroll, 좌측 656. 상점 -> 좌측 통합 2758, Solo 꺼짐. 제작대도 같다.
- Play: 닫으면 둘 다 꺼진다.

[2026-09-19 최종 일괄 점검]
- EditMode 128/128 통과. 콘솔 에러/경고 0.
- InventoryTestBench 의 인스펙터 참조 NULL 0건.
- 남은 NULL 2건은 의도된 것이다.
  EnemyBulletPool.partcleSystem 은 팀원 원본에서도 비어 있다.
  StorageInteraction.dropSettings 는 허브 창고라 전리품 설정이 없다.
- Play 통합 확인: 보관상자/전리품/맵상자/상점/제작대 전부 열림.
  창고 위치는 단독일 때 우측, 상점·제작대일 때 좌측 통합으로 정상 전환.
  전리품 1클릭 선택(수량 유지) / 2클릭 획득, 구매 200->195G, 수리 1G 로 90->140,
  제작 완료, 저장->초기화->불러오기 후 내구도 손상 77 보존.
- PlayerInventoryBridge.cs 는 어느 씬/프리팹에서도 쓰이지 않는다. 삭제 후보로 남겨 둔다.
- ProjectSettings 는 손대지 않는 것이 규칙이라 Play 검증 때 켜졌던 runInBackground 를 0 으로 되돌렸다.
  git status 에 ProjectSettings 3개가 M 으로 보이지만 diff 는 비어 있다.
  커밋 전에 git checkout -- ProjectSettings 로 정리하면 된다.

[내구도 수리 분리 - 2026-09-20]
- InventoryWorldKind 에 Repair 추가. InventoryWorldContainer.kind 를 Repair 로 두면 F 로 수리대가 열린다.
- 상점(ShopPanel)에서 '수리 1G' 버튼 제거. 수리는 RepairPanel 전용.
- 하이어라키: InventoryUI 프리팹 Root/RepairPanel (TitleBar / Label(보유 G) / Detail(DetailIcon+Label) / Button_수리 1G).
- 코드 참조가 비어 있으면 이름(RepairPanel)으로 자동 참조한다. 인스펙터 InventoryTestBench > ui > 수리대 에서 교체 가능.
- 사용법: 수리대 열기 -> 가방/장비 칸 클릭으로 총기 선택 -> '수리 1G' 클릭. 1G 당 회복량은 ItemData.csv 의 repairAmountPerCurrency.
- 검증: EditMode 128/128 통과. 플레이 모드에서 기관권총 내구도 60/100 -> 100/100, 지푸라기 200 -> 199 확인.

[방어구 내구도 - 2026-09-21]
- 임시 규칙(전 등급 통일): 최대 100 / 피격당 2 / 수리비 3G 완전 수리. 0 이 되면 그 부위만 소멸.
  수치는 InventoryTestBench 인스펙터 '방어구 내구도 임시 규칙'(armorDurability)에서 바로 고친다. 코드 수정 불필요.
- 1회 피격당 장착 중인 헬멧과 조끼가 각각 차감된다(기획서 6.7, 히트박스 구분 없음).
- 신규 ArmorDurabilityBridge (NaYeongMinIntegration). PlayerVitals.HealthChanged 구독.
  체력 감소폭이 minHitDamage(기본 0.5) 이상일 때만 ApplyArmorHit. 허기 0 자연 피해는 프레임당 값이 작아 걸러진다.
  팀원 코드 수정 0. NaYeongMin.unity / Test.unity 의 Player 에 부착, playerVitals·inventoryBench 자동 연결.
- 장비/가방/창고 칸에 방어구 내구도 숫자 표시 추가(무기와 같은 자리).
- 수리대에서 방어구 선택 시 버튼 글자가 '완전 수리 3G' 로 바뀐다. 무기는 기존 '수리 1G'.
- 검증: EditMode 145/145 통과(ArmorDurabilityTests 13개 신규).
  Play: 피격 1회 -> 헬멧·조끼 각각 100->98, 2회 -> 96/100 표시. 잔여 2 에서 피격 -> 헬멧 소멸(itemId -1).
  Play: 최고급 조끼 60/100 -> '완전 수리 3G' -> 100/100, 지푸라기 5 -> 2.

[방어력 계산식 - 확정 2026-09-21]
- 방어력은 팀원(김표진)이 설정해 둔 값과 방식을 그대로 쓴다. 내가 바꾸지 않는다.
  PlayerArmorBridge.GetDamageMultiplier() 의 비율 곱연산, ItemData.csv 의 defense 10/20/30 유지.
  실측: 공격력 5, 튼튼헬멧+최고급조끼 -> 2.8 피해.
- 기획서 2차 수정(2026-09-10) 6.7 / 10.2.1 에는 정수 차감식(MAX(1, 공격력 - 총 방어력),
  구형 1 / 튼튼 2 / 최고급 3)으로 적혀 있으나 적용하지 않는다. 기획서 6.7 안에도
  '곱연산 / 부위당 50% 캡' 불릿과 6.2 '최고급 둘 다 60% 경감' 문장이 함께 있어 비율식과 모순되지 않는다.
- 내 작업 범위는 방어구 '내구도' 뿐이다. 내구도는 방어력 수치와 독립이다.
- 출처: 비율식과 defense 10/20/30 은 커밋 e763cd2e (2026-09-17, 김표진, '방어구 수치 모델 적용') 에서 들어왔다.
  그 이전까지 CSV defense 는 기획서와 같은 1/2/3 이었다. 내 커밋에서 defense 열을 바꾼 적은 없다.

[무기 최대 내구도 갱신 - 2026-09-21]
- 기획서 2차 수정(2026-09-10) 5.2 표대로 최대 내구도를 올렸다.
    기관권총 10001  100 -> 500
    샷건     10002  120 -> 600
    돌격소총 10003  180 -> 900
    스나이퍼 10004  160 -> 800
- 표의 '사격 당 소모' 칸 괄호값 (1) 은 '1발 발사당' 이라는 뜻이다. 소모량은 2 / 3 / 3 / 8 로 변경 없음.
- 1G 당 회복 내구도 100 / 60 / 50 / 40 도 표와 같아 변경 없음. 상점 가격도 그대로다.
- 고친 곳: ItemData.csv 의 maxDurability 열 4칸, WeaponDurability 폴백 상수, StationTests TestCase 4건.
  WeaponDurability 는 CSV 를 먼저 읽으므로 수치 정본은 CSV 한 곳이다. 폴백은 CSV 가 없을 때만 쓴다.
- 검증: EditMode 145/145 통과. Play: Maximum 500/600/900/800, 돌격소총 1발 마모 3, 칸 표시 897/900.

[확인 필요 - 수리비가 기획서 표와 5배 차이]
- 최대 내구도만 5배가 되고 1G 당 회복량은 그대로라 완전 수리 비용이 5배가 된다.
  Play 실측(잔여 1 -> 만땅): 10001 5G / 10002 10G / 10003 18G / 10004 20G.
  기획서 5.2 의 '수리비 최고 수치' 열은 1G / 2G / 3G / 4G 다. 정확히 5배 차이.
- 1차 기획서에서는 최대 내구도와 1G 당 회복량이 맞아떨어져 1/2/3/4G 가 나왔다.
  2차 수정에서 최대 내구도만 올리고 1G 당 회복량을 안 고친 것으로 보인다.
- 어느 쪽이 정본인지 확정 전까지 1G 당 회복량은 표 그대로 100/60/50/40 을 유지한다. 임의 수정하지 않음.

[CSV - 방어력 열은 손대지 않음]
- ItemData.csv 의 defense 열(10/20/30)은 그대로 둔다. 이번 작업에서 바꾼 것은 maxDurability 열뿐이다.

[프리팹 재구성 - 2026-09-22]
- 리베이스 과정에서 Test.unity 의 내 오브젝트들이 프리팹 연결을 잃고 씬에 직접 박혀 있었다. 전부 다시 프리팹으로 묶었다.
- InventoryUI.prefab 을 현재 Test.unity 의 Root 내용으로 갱신하고 씬 오브젝트를 그 프리팹 인스턴스로 다시 연결했다.
  프리팹이 09-18 버전이라 빠져 있던 RepairPanel / WarehouseSoloScroll / PlayerDeathPanel /
  EquipmentPanel 의 StrawCurrencyIcon·StatsText 가 이제 프리팹에 들어 있다.
- 새로 만든 프리팹: Chest, LootPickupTest, RandomChest_Food_Test, RandomChest_Ammo_Test,
  RandomEnemyDeath_Test, PlayerDeath/PlayerDeath_Test.
  갱신한 프리팹: TemporaryShopNPC, TemporaryCraftingStation.
- 팀원 오브젝트는 건드리지 않았다. Player / Enemy / EnemyPool / Main Camera / VirtualCamera /
  CameraTarget / Ground / Object / BulletPooling / EventSystem / KTS_Imported /
  PreviousActors_Backup, 그리고 벤치 자식인 Crosshair / Interaction 은 그대로 둔다.
- SaveAsPrefabAssetAndConnect 를 써서 씬 오브젝트를 그대로 유지한 채 연결했다.
  벤치 ui 참조 39건 + 배열 원소 136건, TestSlotView 162개가 작업 전후로 동일하다.
  각 상호작용 오브젝트의 씬 참조(dropSettings / inventoryBench / prompt / player)도 전후 동일하다.
  씬 참조는 프리팹 에셋 쪽에서는 None 이 되고 씬 인스턴스에만 남는다. 정상이다.
- NaYeongMin.unity 는 지시대로 손대지 않았다. 다만 그 씬도 InventoryUI.prefab 을 참조하므로
  갱신된 UI 를 자동으로 받게 된다. 그 씬을 열면 벤치의 ui 참조를 한 번 점검해야 한다.
- 참고: 런타임에 팀원 PlayerInventoryToggle 이 Root/QuickPanel 을 캔버스 바로 밑으로 옮긴다.
  인벤토리 창을 닫아도 퀵슬롯이 보이게 하려는 의도다. 프리팹에서 QuickPanel 은 Root 자식이어야 한다.
- 방어구 내구도는 기획 확정 전까지 인스펙터 고정값을 정본으로 쓴다. 전 등급 최대 100 / 피격당 2 / 수리비 3G.
  ItemData.csv 의 방어구 maxDurability·durabilityCostPerHit 열은 값이 확정되지 않아 사용하지 않는다.
- 검증: EditMode 153/153 통과. 콘솔 에러·경고 0. Play 에서 벤치 IsReady, 슬롯 162개,
  패널 전부 존재, 수리대 열기 정상.

[방어구 아이콘 교체 - 2026-09-23]
- 헬멧·조끼 아이콘을 실제 3D 모델로 렌더링한 이미지로 바꿨다. 예전에는 3등급 모두 방독면/배낭 아이콘 하나를 같이 썼다.
- 원본 모델은 팀원 ArmorVisual 이 쓰는 매핑과 같다. Assets/2.Model/Prefabs/Helmet Armor/ (_Enemy 폴더 아님)
    13001 구형 헬멧   <- 1LvHelmet   -> ICon/Armor/helmet-lv1.png
    13002 튼튼 헬멧   <- 2LvHelmet   -> ICon/Armor/helmet-lv2.png
    13003 최고급 헬멧 <- 3LvHelmet   -> ICon/Armor/helmet-lv3.png
    12001 구형 조끼   <- 1LvArmor    -> ICon/Armor/vest-lv1.png
    12002 튼튼 조끼   <- 2LvArmor    -> ICon/Armor/vest-lv2.png
    12003 최고급 조끼 <- 3LvArmor    -> ICon/Armor/vest-lv3.png
  경로: Assets/4.Sprite/ICONs/ICon/Armor/. 256x256, 배경 투명, 정면 왼쪽 위 3/4 시점. Sprite(Single) 임포트.
  원본 프리팹은 읽기만 했고 수정하지 않았다.
- 적용한 곳: ItemData.csv 의 iconKey 6행, Test.unity 벤치의 icons 바인딩 6건.
  InventoryTestBench.prefab 에셋의 icons 에는 방어구 항목이 원래 없어 바꿀 것이 없었다.
- 아이콘 우선순위: 벤치 인스펙터 icons 바인딩 > CSV iconKey(에디터에서만).
  그래서 icons 바인딩에 옛 아이콘이 박혀 있는 씬은 CSV 를 바꿔도 옛 아이콘이 나온다.
- 아직 옛 아이콘(방독면/배낭) 바인딩이 남아 있는 씬
    NaYeongMin.unity           내 개인 씬. 최신화 보류 지시로 손대지 않음.
    LobbyScene / BattleScene / BattleSceneBoxTest   팀원 씬. 수정 금지라 손대지 않음.
  해당 씬의 InventoryTestBench > Icons 목록에서 itemId 12001~12003, 13001~13003 의 Sprite 칸에
  위 png 6개를 끌어다 놓으면 된다.
- 검증: EditMode 165/165 통과. Play(Test.unity): 가방 6칸과 장비 머리/몸통 칸에 새 아이콘 표시 확인.

[적 드롭 - 기획서 10.3 최신 표 + 착용 방어구 드롭 2026-09-23]
- 드롭 확률 갱신
    DropTable.csv : 적 3종이 자기 무기와 그 탄약을 100% 떨어뜨리도록 7행 변경, 확률 0 이 된 12행 삭제.
                    기본 = 기관권총+총알 / 중무장 = 샷건+총알 (+스나이퍼·총알 50) / 원거리 = 돌격소총+총알 (+스나이퍼·총알 5)
    상자 SO       : Chest_Normal 에 무기 4·탄약 4·보호구 6 항목 추가, Chest_Weapon 기관권총 30->50,
                    Chest_Ammo 확률 50/50/50/50 -> 50/25/15/5. 수량은 건드리지 않았다.
    나머지 열(보호구·회복·음식·판매·특수)은 이전 값과 이미 같았다. 상세는 DataTeble/.../DropTableNotes.txt.
- 적 프리팹 연결 (KTS/Enemy, 사용자 승인으로 컴포넌트만 추가. 팀원 코드 수정 0)
    Enemy_Pistol  -> EnemyLootReceiver(BasicEnemy)  + EnemyDeathWatcher
    Enemy_Shotgun -> EnemyLootReceiver(HeavyEnemy)  + EnemyDeathWatcher
    Enemy_Rifle   -> EnemyLootReceiver(RangedEnemy) + EnemyDeathWatcher
  팀원 EnemyController 는 사망 시 Destroy 만 한다. EnemyDeathWatcher 가 그 파괴 시점을 잡아 드롭을 만든다.
  EnemyLootReceiver 의 LootRuntime 칸은 비워 둬도 씬에서 자동으로 찾는다(프리팹은 씬 참조를 못 가지므로).
- 착용 방어구 드롭
  팀원 EnemyArmor 가 스폰(OnEnable) 때 EnemyData 확률로 헬멧·조끼를 골라 HeadGear/Belly 밑에 붙인다.
  EnemyLootReceiver 가 Start 에서 그 모델 이름(1LvHelmet(Clone) ~ 3LvArmor(Clone))을 읽어
  헬멧 13001~13003 / 조끼 12001~12003 으로 바꿔 둔다. 사망 시 전리품 앞칸에 1개씩 넣는다.
  앞칸이라 8칸이 넘쳐도 착용 방어구는 버려지지 않는다. 넘치면 뒤쪽 추첨분이 버려진다(기존 규칙).
  팀원 코드를 고치지 않으려고 private 필드 대신 붙은 모델 이름을 본다. 모델 이름을 바꾸면 이 판별도 같이 바꿔야 한다.
  인스펙터 Drop Worn Armor 를 끄면 착용 방어구를 넣지 않는다.
  표의 방어구 확률 추첨은 착용분과 별개다. 같은 방어구가 2개 나올 수 있다.
- LootRuntime.Spawn 에 보장 아이템 인자(guaranteedItemIds)를 선택 인자로 추가했다. 기존 호출은 그대로 동작한다.
- Test.unity
    적 3종 배치: Enemy_Pistol(-6,6) / Enemy_Shotgun(0,8) / Enemy_Rifle(8,2). 기존 Enemy 는 그대로 둠.
    EnemyPool 의 ricocheParticlePrefab / dieParticlePrefab 가 비어 있어 적이 죽지 못했다(사망 처리 중 예외).
    팀원 Text_KTS 씬과 같은 ObstarcleHit / EnemyDie 프리팹으로 연결했다. 팀원 씬은 수정하지 않았다.
- 검증: EditMode 176/176 통과 (EnemyDropTests 11개 신규). 콘솔 에러 0.
  Play 통합(Test.unity):
    스폰 착용 캡처가 EnemyArmor 의 실제 추첨 결과와 일치 (착용 없음 포함).
    사살 -> 사망 위치에 노란 오브제 생성.
      권총   [최고급 헬멧, 튼튼 조끼] + 지푸라기5 + 기관권총 + 기관권총 총알2 + 구형 헬멧 + 구형 조끼 + 회복약4
      샷건   [튼튼 조끼] + 지푸라기19 + 샷건 + 스나이퍼 + 샷건 총알2 + 튼튼 조끼 + 회복약 + 회복키트
      라이플 [구형 헬멧, 최고급 조끼] + 지푸라기6 + 돌격소총 + 돌격소총 총알 + 튼튼 조끼
    권총 적 드롭을 F 상호작용으로 열어 전리품 8칸과 새 방어구 아이콘 표시 확인.
  손조작(실제 사격으로 사살)은 하지 않았다. TakeDamage 직접 호출로 사살했다.
- 확인 필요
    NaYeongMin.unity 의 LootRuntime 에 '원거리 적 돌격소총 총알 5%' 확률 조정이 남아 있다.
    예전 표의 깨진 칸 대응용인데 새 표(100%)와 어긋난다. 최신화 보류 중인 씬이라 손대지 않았다.
    Chest_Ammo 수량(30~60 등)과 적·일반상자 탄약 수량(1~2 박스)의 단위가 다르다.

[확인 결과 2026-09-23]
- NaYeongMin.unity 는 통합 테스트용 씬이다. 그 씬 LootRuntime 의 '원거리 적 돌격소총 총알 5%' 조정을 지웠다. 이제 CSV(100%) 그대로 쓴다.
- 탄약은 발 단위다. 탄약 아이템 1개 = 1발. 박스 단위 개념은 폐기됐다.
  Chest_Ammo 의 30~60 은 30~60발이다.
  팀원 WeaponController 재장전도 WeaponInventoryBridge.ConsumeAmmo 로 아이템 1개를 1발로 소모한다.
- 8칸이 넘쳐 뒤쪽 추첨분이 버려지는 것은 허용.

[탄약 수량 발 단위로 통일 - 2026-09-23]
- 탄약 아이템 1개 = 1발. 예전 '1박스 = 20발' 수량을 전부 x20 해서 발 수로 바꿨다.
    DropTable.csv 탄약 13행: 2개 -> 40발, 1개 -> 20발 (Box / AmmoBox / 적 3종)
      기본 적 기관권총 총알 40 / 중무장 샷건 총알 40, 스나이퍼 총알 20 / 원거리 돌격소총 총알 20, 스나이퍼 총알 20
    Chest_Normal 탄약 4항목: 40 / 40 / 20 / 20발
    Chest_Ammo 는 원래 발 단위(30~60 등)라 그대로.
- 제작대: 버섯 5 + 화약 5 -> 20발. CraftingService.CraftedAmount 가 ItemData.csv 의 magazineSize(20)를 읽는다.
- 상점: 탄약은 20발 묶음으로 사고판다 (ShopService.TradeAmount, 역시 magazineSize). 가격은 그대로 묶음당 2/3/3/5G.
  판매는 한 칸에 20발 이상 있어야 된다. 구매 때 가방이 모자라 일부만 들어가면 통째로 되돌린다.
  상점 목록에 탄약은 '이름 x20  가격G' 으로 보인다.
  묶음 크기를 바꾸려면 ItemData.csv 의 탄약 magazineSize 만 고치면 된다.
- UI 문구의 '1박스' 를 '20발' 로 바꿨다 (제작 안내·상세·완료 문구, 판매 실패 문구).
- 검증: EditMode 186/186 통과 (AmmoUnitTests 10개 신규, 제작 테스트 3건 기대값 1 -> 20).
  Play: 권총·샷건 적 드롭 탄약 40발, 라이플 적 20발. 상점 돌격소총 총알 구매 20발 / 3G. 제작 20발.
- 남은 것
    ItemData.csv 탄약 설명이 아직 '1박스 20발. 사용 시 탄약이 20발 늘어난다.' 이다. 기획 문구라 고치지 않았다.
    PlayerInventoryService 의 GetRoundsPerBox / GetAmmoRounds / ConsumeAmmoRounds 는 박스 기준 계산이다.
    게임에서는 쓰지 않고(재장전은 팀원 WeaponInventoryBridge.ConsumeAmmo) 테스트만 쓴다. 정리 후보.
    상점 판매 버튼 글자 '1개 판매' 는 탄약일 때 20발이 팔린다. 프리팹 글자는 기획팀이 바꿀 수 있다.

[회복량 버그 수정 - 2026-09-23]
- 증상: 회복약을 먹으면 체력이 +6 만 올랐다(최대 100 의 6%). 기획은 최대 체력의 20%.
- 원인(내 코드만): ItemData.csv 회복량 6/12/27 은 기획 최대치 30 기준 절대량이다(= 20/40/90%).
  그런데 PlayerInventoryService.UseRecoveryItem 이 그 값을 IRecoveryTarget.TryApplyRecovery 의 % 인자에 그대로 넘겼다.
  팀원 InventoryTestBenchLink 는 인터페이스 이름대로 % 로 받아 MaxHealth 에 곱한다. 팀원 쪽은 맞게 구현돼 있다.
- 수정: PlayerInventoryService 에 RecoveryPercent(값 x 100 / 30) 를 두고 넘기기 전에 한 번 환산한다.
  허용 범위 검사도 0~100 -> 0~30 으로 맞췄다. 아이템 상세창 효과 표기를 '체력 +20%' 형식으로 바꿨다.
  CSV 와 팀원 코드는 건드리지 않았다.
- 검증: EditMode 186/186 (회복 테스트 기대값을 절대량 6/12/27 -> 20/40/90% 로 갱신).
  Play: 체력 30 -> 회복약 50 (+20) -> 회복키트 90 (+40). 밥·물은 최대치에서 멈춤. 상세창 '체력 +90%'.

[프리팹 최신화 - 2026-09-23]
- InventoryTestBench.prefab: 씬에서만 붙어 있던 Root(중첩 InventoryUI.prefab)를 프리팹에 반영. RectTransform/Canvas/CanvasScaler/InventoryTestBench 설정도 반영.
  UI 참조 39개 연결. shopPrev/shopNext 2개는 비어 있음(의도, 상점 페이지 안 씀). 슬롯 162개.
- StorageInteraction / LootInteraction / MapChestInteraction.prefab: 씬에서 추가한 WorldContainerInteractable + Interaction 레이어 반영.
- NaYeongMin.unity: 예전 씬 전용 Root(PlayerDeathPanel 중복 포함) 삭제 -> 프리팹 Root 사용. 벤치 컴포넌트 오버라이드 되돌림(= Test.unity 와 같은 설정).
- 제외: 팀원 Crosshair / Interaction 오브젝트는 씬에만 둠. 씬 오브젝트 참조(lootRuntime, player 등)는 프리팹에 넣을 수 없어 씬 오버라이드 유지(없으면 자동 탐색).
- 검증: EditMode 186/186. Play(Test/NaYeongMin 둘 다, 코드 호출): IsReady, Root 1개, 슬롯 162, 사망패널 1개, 인벤/창고/제작대 열기·닫기 오류 0. 손조작 검증 아님.

[씬 통합 대비 점검 / 프리팹화 - 2026-09-25]
- PlayerDeathSpawner.prefab 새로 만듦(PlayerDeath 폴더). 팀원 Player.prefab 에 PlayerDeathSpawner 가 없어서, 통합 씬에서
  Player 를 프리팹으로 쓰면 사망 분실물·아이템 손실이 빠진다. 단독 오브젝트로 놓을 수 있게 코드 수정:
  Awake 에서 playerVitals/inventoryBench 자동 탐색, 생성 위치와 WeaponController 는 playerVitals 쪽 기준.
  Test/NaYeongMin 씬은 Player 의 컴포넌트를 빼고 이 프리팹으로 교체.
- EnemyPool: 두 씬의 씬 전용 EnemyPool 을 EnemyPool.prefab 인스턴스로 교체. NaYeongMin 쪽은 이펙트 참조가 비어 있었음.
  NaYeongMin 의 EnemyPool 밑에 있던 테스트 큐브(Object)는 루트로 옮겨 유지.
- NaYeongMin.unity 정리: Storage/Loot/MapChest 에 WorldContainerInteractable 이 2개씩 붙어 있던 것 1개로(씬 추가분 제거).
  MapChestInteraction 의 kind 가 PlayerDeath 로 덮여 있던 것 되돌림(MapChest). LootPickupTest 를 프리팹 인스턴스로 교체.
- 적 사망 드랍 복구: 팀원이 09-23 EnemyController 사망 처리를 Destroy -> SetActive(false) 로 바꿔(ae397727)
  EnemyDeathWatcher.OnDestroy 가 안 불려 모든 씬에서 드랍이 끊겨 있었다. 팀원 코드는 그대로 두고 EnemyDeathWatcher 에 OnDisable 추가.
  사망으로 보는 조건: Start 이후 + 자기 자신이 꺼짐(activeSelf false). 출현 확률로 Awake 에서 꺼진 적, 스포너가 부모 영역을 끈 경우,
  씬 언로드/플레이 종료는 무시. Destroy 경로도 유지(중복은 deathHandled 로 막힘).
- 검증: EditMode 186/186. Play(코드 호출): Test 적 3종 사살 -> 드랍 3개. 부모 영역 끄기 -> 드랍 없음. Start 전 끄기 -> 드랍 없음.
  플레이어 사망 -> 분실물 1개(중복 없음), 소지품 19 -> 0. NaYeongMin 도 동일, 인벤/창고 열기 정상. Battleground 활성 적 2 사살 -> 드랍 2
  (Pistol 은 팀원 출현 확률로 안 나옴). 오류 0. 손조작 검증 아님.
