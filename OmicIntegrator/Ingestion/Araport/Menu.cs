using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.Araport
{
    internal class Menu
    {
        public static Task Program()
        {
            Dictionary<string, Func<Task>> Items = new()
            {
                { "Load genome annotation (GFF file)", LoadGenome.Program } ,
                { "Setup chromosomal sequences", ChromosomalSequencesSetup.Program } ,
                { "Set representative gene models", SetRepresentativeGeneModels.Program } ,
                { "Load GO terms relationships", LoadGoHierarchy.Program},
                { "Load Araport11 GO annotations", LoadTairGoTerms.Program},
                { "Cancel", () => Task.CompletedTask }
            };

            return ConsoleInput.PickItem(Items)();
        }
    }
}
