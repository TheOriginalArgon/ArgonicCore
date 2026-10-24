using RimWorld;
using System;
using System.Configuration;
using UnityEngine;

namespace ArgonicCore.Utilities
{
    public static class SplitUtility
    {
        private static int RoundToNearest5(int value)
        {
            int roundedValue = (value + 2) / 5 * 5;
            return roundedValue == 0 ? 5 : roundedValue;
        }

        // Obsolete. Kept for backward compatibility.
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

        public static int Split(int percentage, float extraCostFactor, int amount, out int extracted, bool round = true)
        {
            int costExtracted = amount * percentage / 100;
            if (round)
            {
                costExtracted = RoundToNearest5(costExtracted);
                extracted = RoundToNearest5((int)(costExtracted * extraCostFactor));
            }
            else
            {
                extracted = (int)(costExtracted * extraCostFactor);
            }
            return Mathf.Max(0, amount - costExtracted);
        }
    }
}
