using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion
{
    internal class Menu
    {
        public static Task Program()
        {
            Dictionary<string, Func<Task>> Items = new()
            {
                { "Load Araport11 genome", Araport.Menu.Program } ,
                { "Load datasets", Datasets.Menu.Program } ,
                { "Load outputs from external tools", ExternalTools.Menu.Program },
                { "Cancel", () => Task.CompletedTask }
            };

            return ConsoleInput.PickItem(Items)();
        }
    }
}
