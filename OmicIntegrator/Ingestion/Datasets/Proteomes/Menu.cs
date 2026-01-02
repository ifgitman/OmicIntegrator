using Microsoft.EntityFrameworkCore;
using NPOI.HPSF;
using OmicIntegrator.Data;
using OmicIntegrator.Data.Datasets;
using OmicIntegrator.Helpers;
using OmicIntegrator.Ingestion.Datasets.Transcriptomes;
using Org.BouncyCastle.Crypto.Signers;
using SixLabors.ImageSharp.Processing;

namespace OmicIntegrator.Ingestion.Datasets.Proteomes
{
    internal class Menu
    {
        public static async Task Program()
        {
            Dictionary<string, Func<IProteomeLoader>> constructors = new()
            {
                { "Arico et al. 2021", () => new Specific.Arico() },
                { "Kruse et al. 2020", () => new Specific.Kruse() },
                { "Reichel et al. 2016", () => new Specific.Reichel() },
                { "Zander et al. 2020", () => new Specific.Zander() },
                { "Zander et al. 2020 (phospho)", () => new Specific.ZanderPhospho() },
            };

            var Loader = ConsoleInput.PickItem(constructors, "Select data source:")();

            var GenomeId = await ConsoleInput.PickGenomeId();
            var DatasetId = await ConsoleInput.PickTableIdInt<Dataset>("Select dataset:", d => d.Id, d => d.Description, d => d.GenomeId == GenomeId);

            var fileName = ConsoleInput.AskFileName("Proteome file:");

            Dictionary<string, Func<Task>?> Actions = new()
            {
                { "Load peptides (once per dataset)", async () => await Loader.LoadPeptides(new(){ GenomeId = GenomeId, DatasetId = DatasetId, FileName = fileName })} ,
                { "Load samples", async() => 
                {
                    BaseCtx ctx = new();
                    
                    var treatmentId = await ConsoleInput.PickTableIdInt<Treatment>("Select treatment:", t => t.Id, t => t.Description, t => t.DatasetId == DatasetId);

                    var peptidesIds = (await ctx.ProteomesPeptides
                                            .Where(p => p.DatasetId == DatasetId)
                                            .Select(p => new { p.Id, p.IdInDataSet })
                                            .ToListAsync())
                                  .ToDictionary(p => p.IdInDataSet, p => p.Id);

                    await Loader.LoadSamples(new() { TreatmentId = treatmentId, FileName = fileName, PeptidesIds = peptidesIds});
                } },
                { "Return", null }
            };

            while (true)
            {
                var action = ConsoleInput.PickItem(Actions);

                if (action == null)
                    return;

                await action();

                Console.WriteLine("Done");
            }
        }
    }
    public interface IProteomeLoader
    {
        Task LoadPeptides(LoadPeptidesRequest pedido);
        Task LoadSamples(LoadSamplesRequest pedido);
    }
    public class LoadPeptidesRequest
    {
        public int DatasetId { get; set; }
        public int GenomeId { get; set; }
        public string FileName { get; set; }
    }
    public class LoadSamplesRequest
    {
        public string FileName { get; set; }
        public int TreatmentId { get; set; }
        public Dictionary<string, long> PeptidesIds { get; set; }
    }

}
