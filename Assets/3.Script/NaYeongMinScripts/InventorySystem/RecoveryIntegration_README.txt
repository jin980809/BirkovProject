회복 아이템 팀 연동 (2026-09-11)

변수/필드 camelCase. 클래스/메서드/프로퍼티 PascalCase.
실제 플레이어 체력/스태미나/허기/수분 시스템은 NaYeongMin 담당 아님.

연결 지점
- 플레이어 담당 또는 별도 어댑터가 IRecoveryTarget 구현.
- TryApplyRecovery(healthRecovery, hungerRecovery, waterRecovery)는 동기 호출.
- 실제 회복 적용 시 true, 최대치/사망/사용불가 등 적용하지 않았으면 false.
- false 반환 시 플레이어 상태 변경 금지. 예외 발생 전 부분 적용도 금지.
- 콜백 내부에서 인벤토리 변경/아이템 사용 재호출 금지.
- playerService.UseRecoveryItem(playerData, bagIndex, target) 호출.
- 성공 시 인벤토리 서비스가 1개 차감하고 퀵슬롯 링크 정리. 호출 측 추가 차감 금지.
- 퀵슬롯은 TryGetItemQuickSlot로 bagIndex 확인 후 동일 API 호출.

회복량 계약
- 현재 CSV 값 그대로 절대량 전달. 비율로 자동 해석하지 않음.
- 6/12/27은 최대치 30일 때 20/40/90%에 해당하는 기존 데이터.
- 실제 최대치 변동에 대응할 비율 데이터 계약은 팀 합의 후 변경할 것.
- 스태미나 회복/특수 효과는 추가하지 않음. Consumable 타입만 처리.

InventoryTestBench의 IRecoveryTarget 구현과 최대치 30/현재치 10은 임시 검증용.
서비스에는 플레이어 클래스/씬/싱글턴/최대치 의존성이 없음.
외부 어셈블리는 Birdkov.NaYeongMin 참조. 코어에서 팀원 어셈블리를 역참조하지 말 것.
CSV/JSON 포맷, 기존 메서드, 씬/프리팹 직렬화 필드는 변경하지 않음.

AI_HANDOFF.md는 이번 작업 시작 시 프로젝트에서 찾지 못함. 임의 복원하지 않음.

검증: 2026-09-11 EditMode 전체 67/67 통과 (신규 회복 테스트 12개 포함).
신규 C# 컴파일 오류 없음. 기존 Main Camera URP 추가 데이터 경고는 별도 미해결.
실제 팀원 플레이어 연동/수동 UI 조작 검증은 아직 진행하지 않음.
