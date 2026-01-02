using EFCore.BulkExtensions;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion
{
    public static class LoadGoHierarchy
    {
        public static async Task Program()
        {
            using var GoFile = ConsoleInput.ReadFile("GoTerms.obo file:");

            GoTerm CurrentTerm = null;
            List<GoTerm> Terms = new();
            List<GoTermsRelationship> Relationships = new();

            string Line;
            do
            {
                Line = await GoFile.ReadLineAsync();

                var Sep = Line.IndexOf(":");

                if (Sep != -1)
                {
                    var Header = Line.Substring(0, Sep);
                    var Value = Line.Substring(Sep + 2);

                    switch (Header)
                    {
                        case "id":
                            CurrentTerm = new GoTerm()
                            {
                                Id = Value
                            };

                            Terms.Add(CurrentTerm);
                            break;
                        case "name":
                            CurrentTerm.Name = Value;
                            break;
                        case "namespace":
                            CurrentTerm.Namespace = Value;
                            break;
                        case "alt_id":
                            Terms.Add(new GoTerm()
                            {
                                Id = Value,
                                Name = CurrentTerm.Name,
                                Namespace = CurrentTerm.Namespace
                            });
                            break;
                        case "is_a":
                            Relationships.Add(new()
                            {
                                ReferenceId = CurrentTerm.Id,
                                ReferredId = Value.Substring(0, Value.IndexOf("!") - 1),
                                Relationship = "is_a"
                            });
                            break;
                        case "relationship":
                            var RelacionId = Value.Substring(0, Value.IndexOf("!")-1).Trim();

                            var Campos = RelacionId.Split(" ");

                            Relationships.Add(new()
                            {
                                ReferenceId = CurrentTerm.Id,
                                ReferredId = Campos[1],
                                Relationship = Campos[0]
                            });

                            break;
                    }
                }
            }
            while (!GoFile.EndOfStream);

            GoFile.Close();

            BaseCtx ctx = new();

            Console.WriteLine($"{Terms.Count} terms");
            await ctx.BulkInsertAsync(Terms);

            Console.WriteLine($"{Relationships.Count} relationships");
            await ctx.BulkInsertAsync(Relationships);

            Console.WriteLine("Done");
        }
    }
}
