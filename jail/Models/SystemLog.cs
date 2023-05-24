using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace jail.Models {
    [Serializable, DataContract]
    public class SystemLog : LongIdContainer {
        public enum LogItemType {
            All,
            Fatal,
            Error,
            Warn,
            Info,
            Debug,
            Trace,
            Full
        }

        [DataMember]
        [DataType(DataType.Date),
         DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime EnteredDate { get; set; }

        [DataMember] public LogItemType Level { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public string MachineName { get; set; }
        [DataMember] public string UserName { get; set; }
        [DataMember] public string Exception { get; set; }
        [DataMember] public string CallerAddress { get; set; }
    }
}