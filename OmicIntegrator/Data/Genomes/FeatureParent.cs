namespace OmicIntegrator.Data
{
    public class FeatureParent
    {
        public Feature Feature { get; set; }
        public long FeatureId { get; set; }
        public Feature Parent { get; set; }
        public long ParentId { get; set; }
    }
}