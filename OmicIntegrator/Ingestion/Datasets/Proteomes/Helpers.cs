using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmicIntegrator.Ingestion.Datasets.Proteomes
{
    public static class Helpers
    {
        public static int? IndexOfBorders(string Refference, string Substring)
        {
            int? rtr = Refference.IndexOf(Substring);

            if (rtr != -1)
                return rtr;

            rtr = Substring.IndexOf(Refference);

            if (rtr != -1)
                return rtr * -1;

            var find = (string rfr, string sub) =>
            {
                for (var i = 0; i < rfr.Length; i++)
                {
                    var length = Math.Min(rfr.Length - i, sub.Length);

                    if (rfr.Substring(i, length) ==
                        sub.Substring(0, length))
                        return (int?)i;
                }

                return null;
            };

            var Partial1 = find(Refference, Substring);

            var Partial2 = find(Substring, Refference);

            if (((Refference.Length - Partial1) ?? -1) > ((Substring.Length - Partial2) ?? -1))
                return Partial1;
            else
                return Partial2 * -1;
        }
    }
}
