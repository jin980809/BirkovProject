namespace Birdkov.NaYeongMin.InventorySystem
{
    // 파밍 컨테이너 크기 프리셋. 기획 확정 크기만 정의한다.
    // 적 사망 노란 오브제는 Box2x4(8칸)를 사용해 기획서 전리품 8칸 규칙과 일치시킨다.
    public enum LootContainerSize
    {
        Box2x4,
        Box3x3,
        Box3x5,
        Box4x1
    }

    public static class LootContainerSizes
    {
        public const LootContainerSize CorpseSize = LootContainerSize.Box2x4;

        public static void GetSize(LootContainerSize sizePreset, out int width, out int height)
        {
            switch (sizePreset)
            {
                case LootContainerSize.Box3x3:
                    width = 3;
                    height = 3;
                    break;
                case LootContainerSize.Box3x5:
                    width = 3;
                    height = 5;
                    break;
                case LootContainerSize.Box4x1:
                    width = 4;
                    height = 1;
                    break;
                default:
                    width = 2;
                    height = 4;
                    break;
            }
        }

        public static int GetSlotCount(LootContainerSize sizePreset)
        {
            GetSize(sizePreset, out int width, out int height);
            return width * height;
        }
    }
}
