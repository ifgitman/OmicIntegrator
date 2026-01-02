using LinqKit;
using MathNet.Numerics.Providers.LinearAlgebra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using OmicIntegrator.Data;
using System.Globalization;
using System.Linq.Expressions;

namespace OmicIntegrator.Helpers
{
    static class ConsoleInput
    {
        public static string? LastFileName { get; private set; }
        public static string? AskFileName(string Prompt, bool CheckExists = true)
        {
            string? FileName = null;
            do
            {
                Console.WriteLine(Prompt);

                var Read = Console.ReadLine();

                if (Read == "*")
                    return null;

                FileName = Read;
            } while (CheckExists
                     && !File.Exists(FileName));

            LastFileName = FileName;

            return FileName;
        }
        public static int PickItem(Dictionary<int, string> dict, string Prompt = "Select option:")
        {
            foreach (var itm in dict)
            {
                Console.WriteLine($"{itm.Key} - {itm.Value}");
            }

            do
            {
                var indice = AskInteger(Prompt);
                if (dict.ContainsKey(indice))
                    return indice;
            }
            while (true);
        }
        public static int AskInteger(string Prompt)
        {
            string Linea;
            int Devolver;
            do
            {
                Console.WriteLine(Prompt);
                Linea = Console.ReadLine();
            } while (!int.TryParse(Linea, out Devolver));

            return Devolver;
        }
        public static int? AskIntegerOptional(string Prompt)
        {
            string Line;
            int Devolver;
            do
            {
                Console.WriteLine(Prompt);
                Line = Console.ReadLine();

                if (Line == "*")
                    return null;
            } while (!int.TryParse(Line, out Devolver));

            return Devolver;
        }
        public static TValue PickItem<TValue>(Dictionary<string, TValue> dict, string Prompt = "Select option:")
        {
            Dictionary<int, TValue> dictInts = [];

            int id = 0;
            foreach (var itm in dict)
            {
                dictInts.Add(++id, itm.Value);
                Console.WriteLine($"{id} - {itm.Key}");
            }

            do
            {
                var index = AskInteger(Prompt);
                if (dictInts.ContainsKey(index))
                    return dictInts[index];
            }
            while (true);
        }
        public static async Task<int> PickTableIdInt<TTable>
            (string Prompt, 
             Expression<Func<TTable, int>> ExprId, 
             Expression<Func<TTable, string>> ExprDescription,
             Expression<Func<TTable, bool>>? Filter = null)
            where TTable : class
        {
            var rows = await SelectTableRows(ExprId, ExprDescription, Filter);

            foreach (var f in rows)
                Console.WriteLine($"{f.Key} - {f.Value}");

            int rtr;
            do
            {
                rtr = AskInteger(Prompt);
            } while (!rows.ContainsKey(rtr));

            return rtr;
        }
        public static async Task<int?> PickTableIdIntOptional<TTable>
            (string Prompt,
             Expression<Func<TTable, int>> ExprId,
             Expression<Func<TTable, string>> ExprDescription,
             Expression<Func<TTable, bool>>? Filter = null)
            where TTable : class
        {
            var rows = await SelectTableRows(ExprId, ExprDescription, Filter);

            foreach (var f in rows)
                Console.WriteLine($"{f.Key} - {f.Value}");

            Console.WriteLine("* - finish");

            int? rtr;
            do
            {
                rtr = AskIntegerOptional(Prompt);

                if (!rtr.HasValue)
                    return null;
            } while (!rows.ContainsKey(rtr.Value));

            return rtr;
        }
        private static async Task<Dictionary<int, string>> SelectTableRows<TTable>(Expression<Func<TTable, int>> ExprId,
             Expression<Func<TTable, string>> ExprDescription,
             Expression<Func<TTable, bool>>? Filter = null)
            where TTable : class
        {
            BaseCtx ctx = new();

            var query = ctx.Set<TTable>().AsExpandable();

            if (Filter != null)
                query = query.Where(Filter);

            var rows = await query
                .Select(f => new { Id = ExprId.Invoke(f), Description = ExprDescription.Invoke(f) })
                .ToListAsync();

            return rows.ToDictionary(r => r.Id, r => r.Description);
        }
        public static StreamReader ReadFile(string Prompt)
        {
            var Name = AskFileName(Prompt);

            if (Name == null)
                return null;

            return new StreamReader(Name);
        }
        public static Task<int> PickGenomeId(string Prompt = "Genome ID:")
        {
            return PickTableIdInt<Genome>(Prompt, g => g.Id, g => g.Name);
        }

        public static async Task<List<long>> AskFeatureIds(string Prompt = "Feature IDs file:", int? GenomeId = null)
        {
            Prompt += "\r\n*.ids => OmicIntegrator FeatureIDs\r\n*.cod => Gene codes";
            using var ArcIds = ReadFile(Prompt);

            if (ArcIds == null)
                return null;

            var ext = Path.GetExtension(LastFileName);
            bool IsFeatureCodes;
            if (ext == ".ids")
            {
                IsFeatureCodes = false;
            }
            else if (ext == ".cod")
            {
                IsFeatureCodes = true;

                if (!GenomeId.HasValue)
                    GenomeId = await PickGenomeId();
            }
            else
            {
                Console.WriteLine("Unknown extensions. No features were selected.");
                return [];
            }
            
            List<long> IDs = new();
            List<string> Codes = new();

            var Line = await ArcIds.ReadLineAsync();
            while (!string.IsNullOrEmpty(Line))
            {
                if (!IsFeatureCodes)
                {
                    if (long.TryParse(Line, out var id))
                        IDs.Add(id);
                }
                else
                {
                    Codes.Add(Line.ToUpper());
                }
                
                Line = await ArcIds.ReadLineAsync();
            }
            ArcIds.Close();

            if (IsFeatureCodes)
            {
                BaseCtx ctx = new();

                IDs = await ctx.Features
                    .Where(f => f.Sequence.GenomeId == GenomeId.Value
                                && Codes.Contains(f.Code.ToUpper()))
                    .Select(f => f.Id)
                    .ToListAsync();
            }

            return IDs;
        }

        public static string AskString(string Prompt)
        {
            Console.WriteLine(Prompt);
            return Console.ReadLine();
        }
        public static bool AskBool(string Prompt)
        {
            Console.WriteLine(Prompt + " (Y/N):");
            do
            {
                var ans = Console.ReadLine().ToUpper();
                if (ans == "Y")
                {
                    return true;
                }
                else if (ans == "N")
                {
                    return false;
                }
            }
            while (true);
        }
        public static TEnum PickEnum<TEnum>(string Prompt = null)
            where TEnum : struct, Enum
        {
            if (string.IsNullOrEmpty(Prompt))
            {
                Prompt = $"Select {typeof(TEnum).Name}:";
            }

            var Values = Enum.GetValues<TEnum>();

            return PickItem(Values, Prompt);
        }
        public static TObj PickItem<TObj>(IEnumerable<TObj> list, string Prompt = "Select option:")
        {
            return PickItem(list.ToDictionary(l => l, l => l.ToString()), Prompt);
        }
        public static TValue? PickItemOptional<TValue>(IEnumerable<TValue> list, string Prompt = "Select option:", string EmptyPrompt = "(select none)")
        {
            Dictionary<int, TValue> dict = new();

            int i = -1;
            foreach (var val in list)
            {
                i++;
                dict.Add(i, val);
                Console.WriteLine($"{i} - {val.ToString()}");
            }
            Console.WriteLine($"* - {EmptyPrompt}");

            do
            {
                var rsp = AskString(Prompt);

                if (rsp == "*")
                    return default(TValue?);

                if (int.TryParse(rsp, out var ind)
                    && dict.TryGetValue(ind, out var rtr))
                    return rtr;
            }
            while (true);
        }

        public static TObj PickItem<TObj>(Dictionary<TObj, string> dict, string Prompt = "Select option:")
        {
            for (int i = 0; i < dict.Count; i++)
            {
                Console.WriteLine($"{i} - {dict.Values.ElementAt(i)}");
            }

            do
            {
                var ind = AskInteger(Prompt);
                if (ind >= 0 && ind < dict.Count)
                    return dict.Keys.ElementAt(ind);
            }
            while (true);
        }
        public static IEnumerable<int> PickItems(Dictionary<int, string> dict, string Prompt = "Select items:")
        {
            foreach (var itm in dict)
            {
                Console.WriteLine($"{itm.Key} - {itm.Value}");
            }
            Console.WriteLine("* - end");

            List<int> Chosen = [];

            while (true)
            {
                while (true)
                {
                    var input = AskIntegerOptional(Prompt);

                    if (!input.HasValue)
                        return Chosen;

                    if (dict.ContainsKey(input.Value))
                        Chosen.Add(input.Value);
                };
            };
        }
    }
}
