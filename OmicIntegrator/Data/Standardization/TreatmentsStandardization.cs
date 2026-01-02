using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmicIntegrator.Data.Standardization
{
    public class TreatmentsStandardization
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int GenomeId { get; set; }
        public List<SampleTypeValue> SAValues { get; set; } = [];
        public List<TreatmentValue> SValues { get; set; } = [];
    }
}
