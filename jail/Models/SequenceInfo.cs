using System.Collections.Generic;

namespace jail.Models {
    public class SequenceInfo : LongIdContainer {
        public long Number { get; set; }
        public string Value { get; set; }
        public long BookOrder { get; set; }
    }

    public class SequenceData : SequenceInfo {
        public List<BookInfo> Books { get; set; }

        public SequenceData() {
            Books = new List<BookInfo>();
        }
    }
}