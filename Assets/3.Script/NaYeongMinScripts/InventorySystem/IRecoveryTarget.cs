namespace Birdkov.NaYeongMin.InventorySystem
{
    public interface IRecoveryTarget
    {
        // 절대 회복량. 실제 회복 적용 시에만 true. false이면 상태를 변경하지 않는다.
        // 동기 호출이며 인벤토리 변경/재진입은 금지. 최대치 판정은 구현 측 담당.
        bool TryApplyRecovery(float healthRecovery, float hungerRecovery, float waterRecovery);
    }
}
