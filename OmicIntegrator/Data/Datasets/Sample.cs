using OmicIntegrator.Data.Datasets.Proteomes;
using OmicIntegrator.Data.Datasets.Transcriptomes;

namespace OmicIntegrator.Data.Datasets
{
    public class Sample
    {
        public int Id { get; set; }
        public Treatment Treatment { get; set; }
        public int TreatmentId { get; set; }
        public string Description { get; set; }
        public decimal? IntensityThreshold { get; set; }
        public SampleType Type { get; set; }
        public List<RnaValue> RnaValues { get; set; } = new();
        public List<ProteomeValue> ProteomeValues { get; set; } = new();
    }
    public enum SampleType
    {
        Transcriptome,
        Proteome,
        Phosphoproteome
    }

}
