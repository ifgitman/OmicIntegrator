using OmicIntegrator.Data;
using OmicIntegrator.Data.Datasets;
using OmicIntegrator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmicIntegrator.Ingestion.Datasets
{
    public static class CreateDataset
    {
        public static async Task Program()
        {
            Dataset adding = new()
            {
                GenomeId = await ConsoleInput.PickGenomeId(),
                Description = ConsoleInput.AskString("Dataset description (e.g. Pfeiffer et al. 2014):"),
                Link = ConsoleInput.AskString("URL link (e.g. doi):")
            };

            BaseCtx ctx = new();
            ctx.Datasets.Add(adding);
            await ctx.SaveChangesAsync();

            Console.WriteLine("Done");
        }
    }
}
