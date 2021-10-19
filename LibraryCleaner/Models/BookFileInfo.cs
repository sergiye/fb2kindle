using System;
using System.Diagnostics;

namespace LibraryCleaner {
    [DebuggerDisplay("Id={id_book}; file_name={file_name}; archive_file_name={archive_file_name}")]
    public class BookFileInfo : IEquatable<BookFileInfo> {
        public readonly string file_name;
        public readonly int id_book;
        public readonly int id_archive;
        public readonly string archive_file_name;
        public string md5sum;
        public long fileSize;
        public int created;

        public BookFileInfo(string fileName, int idBook, int idArchive, string archiveFileName, string md5Sum,
            long size, int createDate) {
            file_name = fileName;
            id_book = idBook;
            id_archive = idArchive;
            archive_file_name = archiveFileName;
            md5sum = md5Sum;
            fileSize = size;
            created = createDate;
        }

        public bool Equals(BookFileInfo other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return file_name.Equals(other.file_name) && id_book == other.id_book &&
                   id_archive == other.id_archive && archive_file_name == other.archive_file_name
                   && md5sum == other.md5sum && fileSize == other.fileSize && created == other.created;
        }
    }
}