namespace OmicIntegrator.Helpers
{
    public static class CsvSplitter
    {
        public static string[] Split
            (string Row, char separator = ',', char StringLiteral = '"')
        {
            List<string> rtr = new();

            int currentFrom = 0;
            bool InString = false;
            bool WithLiterals = false;

            void AddField(int endPosition)
            {
                var from = currentFrom + (WithLiterals ? 1 : 0);
                var length = endPosition - currentFrom - (WithLiterals ? 2 : 0);

                if (length <= 0)
                    rtr.Add(string.Empty);
                else
                    rtr.Add(Row.Substring(from, length));
            }

            for (int x = 0; x < Row.Length; x++)
            {
                var character = Row[x];

                if (!InString)
                {
                    if (character == separator)
                    {
                        AddField(x);
                        currentFrom = x + 1;
                        WithLiterals = false;
                    }
                    else if (character == StringLiteral)
                    {
                        InString = true;
                        WithLiterals = true;
                    }
                }
                else
                {
                    if (character == StringLiteral)
                    {
                        InString = false;
                    }
                }
            }

            AddField(Row.Length);

            return rtr.ToArray();
        }
    }
}
