namespace ArgonicCore.Utilities
{
    public static class SplitUtility
    {
        public static int Split(string splitMode, int amount, out int extracted)
        {
            if (splitMode == "small")
            {
                switch (amount)
                {
                    case 15:
                        extracted = 5;
                        return 10;
                    case 20:
                        extracted = 5;
                        return 15;
                    case 25:
                        extracted = 10;
                        return 15;
                    case 30:
                        extracted = 10;
                        return 20;
                    case 35:
                        extracted = 15;
                        return 20;
                    case 40:
                        extracted = 15;
                        return 25;
                    case 60:
                        extracted = 20;
                        return 40;
                    case 80:
                        extracted = 30;
                        return 50;
                    default:
                        extracted = amount / 3;
                        if (extracted <= 0) { extracted = 1; }
                        return amount - extracted;
                }
            }
            if (splitMode == "big")
            {
                switch (amount)
                {
                    case 15:
                        extracted = 10;
                        return 5;
                    case 20:
                        extracted = 15;
                        return 5;
                    case 25:
                        extracted = 15;
                        return 10;
                    case 30:
                        extracted = 20;
                        return 10;
                    case 35:
                        extracted = 20;
                        return 15;
                    case 40:
                        extracted = 25;
                        return 15;
                    case 60:
                        extracted = 40;
                        return 20;
                    case 80:
                        extracted = 50;
                        return 30;
                    default:
                        extracted = amount / 2;
                        if (extracted <= 0) { extracted = 1; }
                        return amount - extracted;
                }
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
