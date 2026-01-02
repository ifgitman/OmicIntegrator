using OmicIntegrator.Helpers;
using OmicIntegrator.Ingestion.Datasets.Transcriptomes;

namespace OmicIntegrator.Ingestion.Datasets
{
    internal class Menu
    {
        public static Task Program()
        {
            Dictionary<string, Func<Task>> Items = new()
            {
                { "Create dataset", CreateDataset.Program} ,
                { "Create treatment", CreateTreatment.Program} ,
                { "Load RNAseq sample", LoadRnaSample.Program} ,
                { "Load proteome", Proteomes.Menu.Program} ,
                { "Cancel", () => Task.CompletedTask }
            };

            return ConsoleInput.PickItem(Items)();
        }
    }
}
