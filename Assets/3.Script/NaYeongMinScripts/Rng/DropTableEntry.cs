using System;

namespace Birdkov.NaYeongMin.Rng
{
    [Serializable]
    public class DropTableEntry
    {
        public DropSourceType sourceType;
        public string presetName;
        public int groupIndex;
        public int itemId = -1;
        public float groupDropChance;
        public float itemSelectionChance;
        public float finalDropChance;
        public int minAmount = 1;
        public int maxAmount = 1;
    }
}
