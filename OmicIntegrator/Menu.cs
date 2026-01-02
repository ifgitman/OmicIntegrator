using OmicIntegrator.Helpers;
using OmicIntegrator.Standardization;
using System.Reflection.Metadata.Ecma335;

namespace OmicIntegrator
{
    public static class Menu
    {
        public static async Task Program()
        {
            bool quiting = false;

            Dictionary<string, Func<Task>> Items = new()
            {
                { "Data ingestion", Ingestion.Menu.Program } ,
                { "Perform standardization", PerformStandardization.Program },
                { "Utilities", Utilities.Menu.Program } ,
                { "Quit", async () => quiting = true}
            };

            while (!quiting)
            {
                await ConsoleInput.PickItem(Items)();
            }
        }
    }
}
