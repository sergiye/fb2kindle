namespace LibCleaner
{
    public class BookInfo
    {
        public int Id;
        public string FileName;
        public bool Deleted;

        public BookInfo(int id, string fileName, bool deleted)
        {
            Id = id;
            FileName = fileName;
            Deleted = deleted;
        }
    }
}