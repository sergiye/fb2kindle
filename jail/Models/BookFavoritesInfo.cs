using System;
using System.ComponentModel.DataAnnotations;

namespace jail.Models
{
    public class BookFavoritesInfo: BookInfo
    {
        public long UserId { get; set; }

        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime DateAdded { get; set; }
    }
}