using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.ExternalTools
{
    internal class Menu
    {
        public static Task Program()
        {
            Dictionary<string, Func<Task>> Items = new()
            {
                { "ScanProSite" , LoadProSite.Program } ,
                { "TMHMM 2.0", LoadTmHMM2.Program },
                { "PrDOS", LoadPrDOS.Program },
                { "Cancel", () => Task.CompletedTask }
            };

            return ConsoleInput.PickItem(Items)();
        }
    }
}
