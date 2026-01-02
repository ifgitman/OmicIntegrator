using OmicIntegrator.Helpers;

namespace OmicIntegrator.Utilities
{
    internal class Menu
    {
        public static Task Program()
        {
            Dictionary<string, Func<Task>> Items = new()
            {
                { "Extract sequences", SequencesDownload.Program } ,
                { "Retrieve genes by  GO term", RetrieveGenesByGoTerm.Program },
                { "Find motif in protein sequences", ProteinMotifFinder.Program },
                { "Download standardized data", DownloadStandardizedData.Program },
                { "Cancel", () => Task.CompletedTask }
            };

            return ConsoleInput.PickItem(Items)();
        }
    }
}
