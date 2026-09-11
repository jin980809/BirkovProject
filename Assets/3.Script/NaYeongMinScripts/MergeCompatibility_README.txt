main 머지 후 호환성 점검 (2026-09-11)

수정 범위: NaYeongMinScripts 내 테스트 UI 및 Editor 구성 메뉴만.
- useStandaloneKeyboard: 단독 테스트 기본 true. 팀원 입력과 함께 사용할 때 false.
  숫자키/E 직접 처리를 끄며 마우스 조작은 유지. 플레이어 사격 입력 차단까지 해주지는 않음.
- 테스트 구성 메뉴: Play 중 실행 차단, 대상 씬 내부에서만 벤치 검색,
  프리팹 인스턴스 변경 기록, NaYeongMin 씬만 저장. 열린 팀원 씬 일괄 저장 제거.

팀 연결 전 합의/연결 필요 (자동 연결하지 않음)
1. PlayerVitals: MaxHealth/MaxHunger/MaxWater 기본값 100. 테스트 벤치는 30.
   CSV 6/12/27은 절대 회복량. 20/40/90%로 바꾸려면 데이터 계약 확정 필요.
   Heal / RestoreHunger / RestoreWater 호출을 IRecoveryTarget 어댑터에서 처리.
   IsDead 및 실제 회복 가능 여부를 먼저 확인. 사망/최대치이면 false로 차감 방지.
2. PlayerInputHandler: WeaponSelected(0~1), QuickSlotUsed(0~2),
   InventoryToggled, InteractPressed 이벤트 제공. 통합 입력은 한 경로만 사용.
   퀵슬롯은 가방 인덱스로 해석 후 UseRecoveryItem 호출. 테스트 임시 수치에 연결하지 말 것.
   UI 클릭 시 사격 차단/마우스 입력 모드 전환은 플레이어 담당과 협의 필요.
3. PlayerVitals.Died -> 실제 플레이어 데이터의 ClearOnDeath 호출 연결 필요.
   테스트 KillPlayer는 별도 임시 데이터만 비움. 창고 데이터는 유지.
4. PlayerController.SetArmed / 무기 선택: 장비 데이터와 연결 필요.
   장비 방어력 계산은 PlayerVitals.TakeDamage에 TODO만 있으며 자동 적용되지 않음.
5. 어셈블리: 내 코어는 Birdkov.NaYeongMin, 팀원 Player는 Assembly-CSharp.
   내 asmdef에서 PlayerVitals를 직접 참조하면 안 됨. 인터페이스/외부 어댑터로 연결.
   연결 때문에 팀원 폴더에 asmdef를 추가하거나 기존 코어 asmdef를 제거하지 말 것.
6. EnemyController는 비어 있음. 적 사망 드롭 호출은 아직 연결할 구현이 없음.

현재 activeInputHandler=2 (Both). 기존 StandaloneInputModule과 팀원 Input System 공존 가능.
통합 씬 EventSystem 중복 배치 금지. New Input만 사용하도록 바뀌면 테스트 입력도 별도 전환 필요.
테스트 저장은 NaYeongMinTestBench 하위 경로. 실제 플레이어 저장으로 사용하지 말 것.

기존 경고: EnemyData의 CreateAssetMenu 대상 형식, Player BitMask 중복 등록,
Main Camera URP 추가 데이터 누락. 팀원 코드/프로젝트 설정은 수정하지 않음.

검증 결과: 수정 후 EditMode 67/67 통과. 신규 C# 컴파일 오류 미발견.
팀원 스크립트/메타 파일은 작업 전후 SHA256 동일 확인.
이번 테스트는 코어 자동 테스트이며, 신규 입력 옵션/구성 메뉴의 수동 UI 검증과
실제 플레이어-인벤토리 통합 테스트를 대체하지 않음.
