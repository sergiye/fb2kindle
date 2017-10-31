using Simpl.Extensions.Database;

namespace jail.Models
{
    public class SequenceInfo: LongIdContainer
    {
        public long Number { get; set; }
        public string Value { get; set; }
        public long BookOrder { get; set; }
    }
}