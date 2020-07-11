using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace jail.Models
{
    public class BookFavoritesInfo: BookInfo
    {
        public long UserId { get; set; }

        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime DateAdded { get; set; }
    }

    public class BookFavoritesViewModel
    {
        public IEnumerable<BookFavoritesInfo> Data { get; set; }
        public int Page { get; set; }
        public int NumberOfPages { get; set; }
        public int Skipped { get; set; }
        public int TotalCount { get; set; }

        public BookFavoritesViewModel(IEnumerable<BookFavoritesInfo> data, int page, int numberOfPages, int totalCount, int skipped)
        {
          Data = data;
          Page = page;
          Skipped = skipped;
          NumberOfPages = numberOfPages;
          TotalCount = totalCount;
        }
    }
}