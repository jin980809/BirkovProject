NaYeongMin 담당 시스템 연결 지침

1. ItemDatabase 컴포넌트의 itemDataCsv에 Assets/DataTeble/NaYeongMinCsvData/ItemData.csv를 연결한다.
2. UI는 PlayerInventoryService만 호출한다. 가방 25칸, 무기 5칸, 퀵슬롯 3칸은 PlayerInventoryData가 생성한다.
3. 무기 슬롯 0~1은 장착, 2~4는 보관이다. 현재 데이터 계층은 5칸 모두 무기만 허용한다. 실제 장착 효과는 전투 시스템 연결 시 0~1만 사용한다.
4. 적/상자 사망 또는 개봉 시 DropRoller.Roll을 호출한다. 결과는 최대 8칸이며 이후 성공분은 폐기된다.
5. Assets/2.Model/Prefabs/NaYeongMin/LootDropPool.prefab은 LootDropObject.prefab과 연결되어 있고 30개를 선생성한다. 전리품이 비면 NotifyContentsChanged를 호출해 풀로 반환한다. 외형·콜라이더는 아트/상호작용 연결 시 추가한다.
6. 플레이어 사망 이벤트에서 PlayerInventoryService.ClearOnDeath를 호출한다. 창고 데이터는 전달하지 않으므로 유지된다.
7. JsonSaveSystem.Save/TryLoad를 사용한다. 주 파일 손상 시 .backup.json을 읽어 복구한다.
8. 씬, 플레이어, 전투, UI와 직접 참조하지 않도록 작성됨. 연결 어댑터는 각 담당자 머지 후 추가한다.
9. 기존 아이템 ID 엑셀은 분류·명칭 참고자료로 응용한다. ID를 그대로 복사하지 않고 프로젝트용 ID로 구성한다. 현재 CSV는 헤더만 있으며 실제 아이템 행은 아직 입력하지 않았다.
10. 드롭 풀은 기본 30개이며 고갈 시 Rent가 null을 반환한다. 호출 측은 null을 확인해야 한다. 자동 확장·기존 전리품 강제 회수는 하지 않는다. 추후 initialSize 설정을 늘려 확장할 수 있다.
