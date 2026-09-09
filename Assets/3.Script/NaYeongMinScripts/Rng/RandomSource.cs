using System;

namespace Birdkov.NaYeongMin.Rng
{
    public interface IRandomSource
    {
        double NextUnit();
        int NextInclusive(int minimum, int maximum);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;

        public SystemRandomSource()
        {
            random = new Random();
        }

        public SystemRandomSource(int seed)
        {
            random = new Random(seed);
        }

        public double NextUnit()
        {
            return random.NextDouble();
        }

        public int NextInclusive(int minimum, int maximum)
        {
            if (maximum < minimum)
            {
                (minimum, maximum) = (maximum, minimum);
            }

            return random.Next(minimum, maximum + 1);
        }
    }
}
