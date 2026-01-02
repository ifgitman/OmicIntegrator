using EFCore.BulkExtensions;
using OmicIntegrator.Data;
using OmicIntegrator.Helpers;

namespace OmicIntegrator.Ingestion.Araport
{
    public static class LoadGenome
    {
        public static async Task Program()
        {
            var ctx = new BaseCtx();

            using var FeatsFile = ConsoleInput.ReadFile("GFF file:");

            var Genome = new Data.Genome() { Name = ConsoleInput.AskString("Genome name:") };
            ctx.Genomes.Add(Genome);
            await ctx.SaveChangesAsync();

            var FeaturesAdding = new List<Feature>();

            Feature fea;

            var Chromosomes = new Dictionary<Feature, string>();
            var Parents = new Dictionary<Feature, string[]>();

            var Line = await FeatsFile.ReadLineAsync();

            while (Line != null)
            {
                if (Line.StartsWith("#"))
                {
                    Line = await FeatsFile.ReadLineAsync();
                    continue;
                }

                string[] Fields = Line.Split("\t");

                if (Fields.Count() < 9)
                {
                    Line = await FeatsFile.ReadLineAsync();
                    continue;
                }

                var atts = Fields[8]
                    .Split(";")
                    .Select(c => c.Split("="))
                    .Where(c => c.Count() == 2)
                    .ToDictionary(c => c[0].ToLower(), c => c[1]);


                fea = new Feature()
                {
                    Type = Fields[2],
                    Start = long.Parse(Fields[3]),
                    End = long.Parse(Fields[4]),
                    Strand = Fields[6] != "." ? Fields[6][0] : null,
                    Phase = Fields[7] != "." ? int.Parse(Fields[7]) : null,
                    Code = atts.TryGetValue("id", out string? valId) ? valId: null,
                    ShortName = atts.ContainsKey("symbol") ? atts["symbol"] :
                        atts.ContainsKey("alias") ?
                        atts["alias"] :
                        null,
                    Description = atts.ContainsKey("note") ? atts["note"] : null
                };

                Chromosomes.Add(fea, Fields[0]);

                if (atts.ContainsKey("parent"))
                {
                    fea.Parents = new List<Feature>();
                    Parents.Add(fea, atts["parent"].Split(",").Distinct().ToArray());
                }

                FeaturesAdding.Add(fea);

                Line = await FeatsFile.ReadLineAsync();
            }

            FeatsFile.Close();

            List<Sequence> SequencesAdding = [];
            foreach (var chr in Chromosomes.Values.Distinct())
            {
                SequencesAdding.Add(new() { Genome = Genome, Name = chr });
            }
            
            ctx.Sequences.AddRange(SequencesAdding);
            await ctx.SaveChangesAsync();

            foreach (var i in from f in Chromosomes
                              join c in SequencesAdding on f.Value equals c.Name
                              select new { fea = f.Key, c })
            {
                i.fea.Sequence = i.c;
                i.fea.SequenceId = i.c.Id;
            }

            Console.WriteLine($"Saving {FeaturesAdding.Count} features");

            await ctx.BulkInsertAsync(FeaturesAdding, c =>
            {
                c.PreserveInsertOrder = true;
                c.SetOutputIdentity = true;
            });

            var FPsAgregando = new List<FeatureParent>();

            foreach (var pf in from d in Parents.SelectMany(f => f.Value.Select(v => new { fea = f.Key, par = v }))
                               join par in FeaturesAdding on d.par equals par.Code
                               select new { d.fea, par })
            {
                FPsAgregando.Add(new FeatureParent() { FeatureId = pf.fea.Id, ParentId = pf.par.Id });
            }

            await ctx.BulkInsertAsync(FPsAgregando);

            Console.WriteLine("Done");
        }
    }
}
