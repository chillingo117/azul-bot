using System.Collections.Generic;

namespace AzulLambda
{
    public static class Constants
    {
        public static readonly Color[][] DefaultMosaicColors = new Color[][]
        {
            new Color[] { Color.Blue, Color.Yellow, Color.Red, Color.Black, Color.White },
            new Color[] { Color.White, Color.Blue, Color.Yellow, Color.Red, Color.Black },
            new Color[] { Color.Black, Color.White, Color.Blue, Color.Yellow, Color.Red },
            new Color[] { Color.Red, Color.Black, Color.White, Color.Blue, Color.Yellow },
            new Color[] { Color.Yellow, Color.Red, Color.Black, Color.White, Color.Blue }
        };

        public static readonly int[] FloorLinePenalties = { -1, -1, -2, -2, -2, -3, -3 };
    }
}