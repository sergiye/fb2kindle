namespace LibCleaner
{
    public class BookInfo
    {
        public readonly int Id;
        public readonly bool Deleted;
        public string FileName { get; private set; }

        public BookInfo(int id, bool deleted)
        {
            Id = id;
            Deleted = deleted;
            FileName = Id + ".fb2";
        }
    }
}