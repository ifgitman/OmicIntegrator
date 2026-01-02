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
    public static class CreateTreatment
    {
        public static async Task Program()
        {
            Treatment adding = new()
            {
                DatasetId = await ConsoleInput.PickTableIdInt<Dataset>("Select dataset:", d => d.Id, d => d.Description),
                Description = ConsoleInput.AskString("Description (e.g. 5 days dark):")
            };

            BaseCtx ctx = new();
            ctx.Treatments.Add(adding);
            await ctx.SaveChangesAsync();

            Console.WriteLine("Done");

        }
    }
}
