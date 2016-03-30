namespace LibCleaner
{
    public class BookInfo
    {
        public int Id;
        public bool Deleted;

        public BookInfo(int id, bool deleted)
        {
            Id = id;
            Deleted = deleted;
            FileName = Id + ".fb2";
        }

        public string FileName { get; set; }
    }
}