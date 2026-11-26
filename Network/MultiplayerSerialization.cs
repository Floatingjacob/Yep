namespace Yep
{
    internal static class MultiplayerSerialization
    {
        internal static int[,] ToArray(string input)
        {
            string[] rows = input.Split(';', StringSplitOptions.RemoveEmptyEntries);
            int rowCount = rows.Length;
            int colCount = rows[0].Split(',').Length;

            int[,] result = new int[rowCount, colCount];

            for (int i = 0; i < rowCount; i++)
            {
                string[] cols = rows[i].Split(',');

                for (int j = 0; j < colCount; j++)
                    result[i, j] = int.Parse(cols[j]);
            }

            return result;
        }

        internal static string ToString(int[,] array)
        {
            int rowCount = array.GetLength(0);
            int colCount = array.GetLength(1);
            var rows = new string[rowCount];

            for (int i = 0; i < rowCount; i++)
            {
                var cols = new string[colCount];
                for (int j = 0; j < colCount; j++)
                {
                    cols[j] = array[i, j].ToString();
                }
                rows[i] = string.Join(",", cols);
            }

            return string.Join(";", rows);
        }

    }
}
