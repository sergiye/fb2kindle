namespace LibCleaner
{
    public class BookFileInfo
    {
        public readonly string file_name;
        public readonly int id_book;
        public readonly int id_archive;
        public readonly string archive_file_name;
        public readonly string md5sum;

        public BookFileInfo(string fileName, int idBook, int idArchive, string archiveFileName, string md5Sum)
        {
            file_name = fileName;
            id_book = idBook;
            id_archive = idArchive;
            archive_file_name = archiveFileName;
            md5sum = md5Sum;
        }
    }
}