using System;
using System.ComponentModel.DataAnnotations;

namespace jail.Models
{
    public class BookHistoryInfo: BookInfo
    {
        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime GeneratedTime { get; set; }
    }
}