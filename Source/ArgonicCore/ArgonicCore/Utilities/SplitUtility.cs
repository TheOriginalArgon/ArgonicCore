using System.Configuration;

namespace ArgonicCore.Utilities
{
    public static class SplitUtility
    {
        private static int RoundToNearest5(int value)
        {
            int roundedValue = (value + 2) / 5 * 5;
            return roundedValue == 0 ? 5 : roundedValue;
        }

        public static int Split(string splitMode, int amount, out int extracted)
        {
            if (splitMode == "small")
            {
                extracted = RoundToNearest5(amount / 3);
                if (extracted > amount) extracted = amount;
                return amount - extracted;
            }
            if (splitMode == "big")
            {
                extracted = RoundToNearest5(amount / 2);
                if (extracted > amount) extracted = amount;
                return amount - extracted;
            }
            if (splitMode == "replace")
            {
                extracted = amount;
                return 0;
            }
            extracted = 0;
            return amount;
        }
    }
}
