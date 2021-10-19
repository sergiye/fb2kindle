using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using Ionic.Zlib;

namespace LibraryCleaner {

  public class Cleaner {

    // private readonly List<int> _seqToRemove;
    private string[] _archivesFound;

    private string ArchivesPath { get; set; }
    public string ArchivesOutputPath { private get; set; }
    public int MinFilesToUpdateZip { get; set; }
    public string FileWithDeletedBooksIds { get; set; }
    public bool UpdateHashInfo { get; set; }
    public bool RemoveDeleted { get; set; }
    public bool RemoveForeign { get; set; }
    public bool RemoveNotRegisteredFilesFromZip { get; set; }
    public bool RemoveMissingArchivesFromDb { get; set; }
    public string[] GenresToRemove { private get; set; }

    public string DatabasePath {
      set => SqlHelper.DataBasePath = value;
    }

    public enum StateKind {
      Log,
      Warning,
      Error,
      Message
    }

    public event Action<string, StateKind> OnStateChanged;

    public Cleaner(string archivesPath) {
      ArchivesPath = archivesPath;
      MinFilesToUpdateZip = 10;
      FileWithDeletedBooksIds = null;
      RemoveForeign = true;
      RemoveDeleted = true;
      UpdateHashInfo = true;
      RemoveMissingArchivesFromDb = true;
      GenresToRemove = GenresListContainer.GetDefaultItems().Where(f => f.Selected).Select(f => f.Code).ToArray();
      // _seqToRemove = new List<int>
      // {
      //     14437,15976,22715,7028,7083,8303,19890,28738,29139,
      //     8361,8364,8431,8432,8434,11767,14485,14486,14487,14498,14499,14500,144501,144502,144503,144504,16384,16385,16429,18684,20833,24135,31331,
      //     3586,10046,12755,31331,3944,4218,14644,31491,30658,25226,6771,27704,7542,8718,28888,15285,18684,15151,31459,
      //     7061, 7115, 9209, 12277, 16885, 31903,//STALKER
      //     1066, 12479, 19944, //конан
      //     204, 5155, //star wars
      //     329, 15523, 16523, 28755, 30230, 34703, 37029, //Warhammer
      //     26275, //Гуров — продолжения других авторов
      //     8166, //Проза еврейской жизни
      //     19044, 20976, //Вселенная «Метро 2033»
      //     4908, //новинки  современника
      //     4258, //сумерки
      // };
    }

    private void UpdateState(string state, StateKind kind) {
      OnStateChanged?.Invoke(state, kind);
    }

    public async Task<bool> CheckParameters() {
      //try to use local db file
      if (string.IsNullOrEmpty(SqlHelper.DataBasePath) || !File.Exists(SqlHelper.DataBasePath)) {
        SqlHelper.DataBasePath = Path.Combine(Environment.CurrentDirectory, "myrulib.db");
      }

      if (!File.Exists(SqlHelper.DataBasePath)) {
        UpdateState("Database file not found!", StateKind.Error);
        return false;
      }

      //try to get archives folder from db
      if (string.IsNullOrEmpty(ArchivesPath) || !Directory.Exists(ArchivesPath)) {
        var res = await SqlHelper.GetScalarFromQuery("select text from params where id=9")
          .ConfigureAwait(false);
        if (res is string dbPath) {
          var dbFolder = Path.GetDirectoryName(SqlHelper.DataBasePath);
          if (dbFolder != null)
            ArchivesPath = Path.Combine(dbFolder, dbPath);
        }
      }

      if (!Directory.Exists(ArchivesPath)) {
        UpdateState("Archives folder not found!", StateKind.Error);
        return false;
      }

      return true;
    }

    private async Task OptimizeArchivesOnDisk() {

      if (!string.IsNullOrWhiteSpace(ArchivesOutputPath))
        Directory.CreateDirectory(ArchivesOutputPath);

      UpdateState("Calculating archives in database...", StateKind.Warning);
      var dbFiles = new Dictionary<string, List<BookFileInfo>>();
      //move remapped files info to books table
      //SqlHelper.ExecuteNonQuery(@"UPDATE books SET
      //       file_name = (SELECT files.file_name FROM files WHERE files.id_book = books.id)
      //     , id_archive = (SELECT files.id_archive FROM files WHERE files.id_book = books.id)
      //    WHERE EXISTS(SELECT * FROM files WHERE files.id_book = books.id)");
      SqlHelper.ExecuteNonQuery("delete from files");

      //get all database items data (some would be removed if not found on disk)
      using (var connection = SqlHelper.GetConnection()) {
        var sql = @"select DISTINCT b.id id_book, b.md5sum md5sum, b.file_size fileSize, b.created created, a.file_name archive_file_name, b.file_name file_name, b.id_archive id_archive from books b
JOIN archives a on a.id=b.id_archive and b.file_name is not NULL and b.file_name<>''";
        using (var command = SqlHelper.GetCommand(sql, connection))
        using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false)) {
          while (await reader.ReadAsync().ConfigureAwait(false)) {
            var fi = new BookFileInfo(SqlHelper.GetString(reader, "file_name").ToLower(),
              SqlHelper.GetInt(reader, "id_book"),
              SqlHelper.GetInt(reader, "id_archive"),
              SqlHelper.GetString(reader, "archive_file_name").ToLower(),
              SqlHelper.GetString(reader, "md5sum"),
              SqlHelper.GetInt(reader, "fileSize"),
              SqlHelper.GetInt(reader, "created"));
            List<BookFileInfo> archiveFiles;
            if (dbFiles.ContainsKey(fi.archive_file_name)) {
              archiveFiles = dbFiles[fi.archive_file_name];
            }
            else {
              archiveFiles = new List<BookFileInfo>();
              dbFiles.Add(fi.archive_file_name, archiveFiles);
            }

            //if (!archiveFiles.Any(f=>f.Equals(fi)))
            archiveFiles.Add(fi);
          }
        }
      }

      UpdateState($"Found archives in database: {dbFiles.Count}", StateKind.Message);
      //process all archives on disk
      var archivesFound = Directory.GetFiles(ArchivesPath, "*fb2*.zip", SearchOption.TopDirectoryOnly).ToList();
      archivesFound.Sort((s, s1) =>
        string.CompareOrdinal(s1, s)); // sort in reverse order to process latest archives first
      UpdateState($"Found {archivesFound.Count} archives to optimize", StateKind.Message);

      //get ids of deleted books from external file (if exists)
      var externallyRemoved = GetExternalIdsFromFile(FileWithDeletedBooksIds);
      if (externallyRemoved != null && externallyRemoved.Count > 0)
        UpdateState($"Books removed in external list: {externallyRemoved.Count}", StateKind.Warning);

      //get all files not found on disk (to remove from db)
      var totalRemoved = 0;
      for (var i = 0; i < archivesFound.Count; i++) {
        var archPath = archivesFound[i];
        try {
          UpdateState($"Processing {i:000}/{archivesFound.Count}: {archPath}", StateKind.Log);
          var archiveName = Path.GetFileName(archPath);
          if (string.IsNullOrWhiteSpace(archiveName)) {
            UpdateState($"Skipped as invalid file name: {archPath}", StateKind.Warning);
            continue;
          }

          if (!dbFiles.ContainsKey(archiveName.ToLower())) {
            UpdateState($"Skipped as not registered in DB: {archPath}", StateKind.Warning);
            continue;
          }

          using (var zip = new ZipFile(archPath)) {
            var dbArchiveFiles = dbFiles[archiveName.ToLower()];
            if (Debugger.IsAttached)
              dbArchiveFiles.Sort((b1, b2) =>
                string.Compare(b1.file_name, b2.file_name, StringComparison.OrdinalIgnoreCase));
            if (UpdateHashInfo) {
              foreach (var zipEntry in zip.Entries) {
                var info = dbArchiveFiles.Find(f => f.file_name == zipEntry.FileName);
                if (info == null) continue;

                var created = CalcCreatedDate(zipEntry.LastModified);
                if (created < 0)
                  created = info.created;
                var size = zipEntry.UncompressedSize;

                //skip md5 calc if other values match
                if (info.fileSize == size && info.created == created)
                  continue;

                // var md5Sum = CalcFileHash(zipEntry);
                // if (info.md5sum != md5Sum)
                {
                  //update db value
                  // info.md5sum = md5Sum;
                  info.fileSize = size;
                  info.created = created;
                  SqlHelper.ExecuteNonQuery(
                    $"update books set file_size={size}, created={created} where id={info.id_book}");
                  // SqlHelper.ExecuteNonQuery($"update books set md5sum='{md5Sum}', file_size={size}, created={created} where id={info.id_book}");
                }
              }
            }

            var zipFilesToRemove = new List<string>();
            var zipFiles = zip.EntryFileNames;
            //remove files from external file with deleted books list 
            foreach (var ext in externallyRemoved) {
              var info = dbArchiveFiles.Find(f => f.id_book == ext);
              if (info == null)
                continue;
              SqlHelper.ExecuteNonQuery($"delete from books where id={info.id_book}");
              var zipFile = zipFiles.FirstOrDefault(f => f == info.file_name);
              if (!string.IsNullOrWhiteSpace(zipFile)) {
                zipFilesToRemove.Add(zipFile);
                UpdateState($"Book removed by external list: {info.file_name}", StateKind.Warning);
              }
            }

            var filesNotFound = dbArchiveFiles.Where(fi => !zipFiles.Contains(fi.file_name)).ToArray();
            if (filesNotFound.Length > 0) {
              //remove not found files from DB
              UpdateState($"{filesNotFound.Length} db records marked to remove", StateKind.Message);
              var dbRemoved = 0;
              foreach (var info in filesNotFound) {
                try {
                  SqlHelper.ExecuteNonQuery($"delete from books where id={info.id_book}");
                  dbRemoved++;
                }
                catch (Exception ex) {
                  UpdateState(
                    $"Error removing DB record: {info.id_book}/{info.id_archive}/{info.file_name}: {ex.Message}",
                    StateKind.Error);
                }
              }

              if (dbRemoved > 0)
                UpdateState($"{dbRemoved} db records removed", StateKind.Warning);
            }

            dbArchiveFiles.RemoveAll(fi => !zipFiles.Contains(fi.file_name));
            var filesFound = dbArchiveFiles.Where(fi => zipFiles.Contains(fi.file_name)).ToList();

            if (RemoveNotRegisteredFilesFromZip) {
              //get files that are not registered in DB
              foreach (var zipFile in zipFiles) {
                if (filesFound.Any(f => f.file_name.Equals(zipFile)))
                  continue;
                UpdateState($"Book not registered: {zipFile}", StateKind.Warning);
                zipFilesToRemove.Add(zipFile);
              }
            }

            if (zipFilesToRemove.Count == 0) continue;
            if (zipFilesToRemove.Count < MinFilesToUpdateZip) {
              //don't waste time for re-saving zip if there are not many files to remove
              UpdateState($"Not registered books to remove: {zipFilesToRemove.Count}", StateKind.Message);
              continue;
            }

            //update zip
            foreach (var zipFile in zipFilesToRemove)
              zip.RemoveSelectedEntries(zipFile);
            UpdateState($"Saving archive {archPath}", StateKind.Log);
            zip.CompressionLevel = CompressionLevel.BestSpeed;
            if (!string.IsNullOrWhiteSpace(ArchivesOutputPath))
              zip.Save(Path.Combine(ArchivesOutputPath, archiveName));
            else
              zip.Save(); //archPath + ".new");
            UpdateState("Done", StateKind.Log);
            totalRemoved += zipFilesToRemove.Count;
          }
        }
        catch (Exception ex) {
          UpdateState($"Error Processing: {archPath}: {ex.Message}", StateKind.Error);
        }
      }

      UpdateState($"Total removed {totalRemoved} files", StateKind.Message);

      SqlHelper.ExecuteNonQuery("delete from books where id_archive not in (select id from archives)");
      SqlHelper.ExecuteNonQuery("delete from bookseq where id_book not in (select DISTINCT id FROM books)");
      SqlHelper.ExecuteNonQuery("delete from sequences where id not in (select DISTINCT id_seq FROM bookseq)");
      SqlHelper.ExecuteNonQuery("delete from genres where id_book not in (select DISTINCT id FROM books)");
      SqlHelper.ExecuteNonQuery("delete from authors where id not in (select DISTINCT id_author FROM books) and id<0");
      SqlHelper.ExecuteNonQuery("delete from fts_book where docid not in (select DISTINCT id FROM books)");
      SqlHelper.ExecuteNonQuery("VACUUM");
    }

    private void VerifyHashesInDatabase(string archiveName, ICollection<BookFileInfo> filesFound,
      ICollection<string> zipFilesToRemove, ICollection<string> zipFiles, ZipFile zip) {

      //collect all hashes in one place
      var allHashes = new Dictionary<string, BookFileInfo>();
      foreach (var info in filesFound) {
        if (allHashes.ContainsKey(info.md5sum)) {
          if (allHashes[info.md5sum].id_book.Equals(info.id_book) ||
              allHashes[info.md5sum].file_name.Equals(info.file_name, StringComparison.OrdinalIgnoreCase))
            continue;
          //remove file from zip and all lists
          zipFilesToRemove.Add(info.file_name);
          //remove duplicated records from DB
          SqlHelper.ExecuteNonQuery(
            $"delete from books where id={info.id_book} and id_archive={info.id_archive} and file_name='{info.file_name}'");

        }
        else {
          allHashes.Add(info.md5sum, info);
        }
      }

      //get file hashes that are not listed in DB
      foreach (var zipFile in zipFiles) {
        if (filesFound.Any(f => f.file_name.Equals(zipFile)))
          continue;
        //calculate zip file hash
        var zipEntry = zip.Entries.FirstOrDefault(e => e.FileName.Equals(zipFile));
        if (zipEntry == null) {
          UpdateState($"Zip entry not found: {zipFile}", StateKind.Warning);
          continue;
        }

        var md5Sum = CalcFileHash(zipEntry);
        //check hash already exists
        if (allHashes.ContainsKey(md5Sum)) {
          if (allHashes[md5Sum].file_name.Equals(zipFile, StringComparison.OrdinalIgnoreCase))
            continue;
          //remove file from zip and all lists
          zipFilesToRemove.Add(zipFile);
        }
        else {
          var fi = new BookFileInfo(zipFile, 0, 0, archiveName, md5Sum, zipEntry.UncompressedSize,
            CalcCreatedDate(zipEntry.LastModified));
          allHashes.Add(md5Sum, fi);
          filesFound.Add(fi);
        }
      }
    }

    private int CalcCreatedDate(DateTime realDate) {
      return 1 + realDate.Month * 100 +
             (realDate.Year - 2000) * 10000; //округлить до месяца, порядок добавления виден по Id
      //return realDate.Day + realDate.Month * 100 + (realDate.Year-2000) * 10000;
    }

    private string CalcFileHash(ZipEntry zipEntry) {

      //calculate hash from real zip file
      using (var ms = new MemoryStream()) {
        zipEntry.Extract(ms);
        ms.Seek(0, SeekOrigin.Begin);
        using (var alg = MD5.Create())
          return BitConverter.ToString(alg.ComputeHash(ms)).Replace("-", "").ToLower();
      }
    }

    private async Task FixImportedLinks() {

      UpdateState("Updating imported files links ...", StateKind.Message);
      var updatedBooks = 0;
      var booksToUpdate = new Dictionary<int, int>();
      using (var connection = SqlHelper.GetConnection()) {
        using (var command =
          SqlHelper.GetCommand(
            @"select DISTINCT b.id, b.file_name from books b join archives a on b.id_archive=a.id and a.file_name like '%f.fb2-%' where b.id<0 ORDER BY b.id DESC",
            connection))
        using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false)) {
          while (await reader.ReadAsync().ConfigureAwait(false)) {
            try {
              var oldId = SqlHelper.GetInt(reader, "id");
              var fileName = SqlHelper.GetString(reader, "file_name").Replace(".fb2", "");
              if (int.TryParse(fileName, out var newId)) {
                booksToUpdate.Add(oldId, newId);
              }
            }
            catch (Exception ex) {
              UpdateState(ex.Message, StateKind.Error);
            }
          }
        }
      }

      UpdateState($"Found {booksToUpdate.Count} imported files links to update...", StateKind.Warning);

      if (booksToUpdate.Count > 0) {
        using (var connection = SqlHelper.GetConnection()) {
          using (var tr = connection.BeginTransaction()) {
            try {
              foreach (var item in booksToUpdate) {
                try {
                  //check book already registered with correct id
                  var oldIdOccupied = await SqlHelper
                    .GetIntFromQuery($"select count(*) from books where id={item.Value}", connection)
                    .ConfigureAwait(false);
                  if (oldIdOccupied == 0) {
                    SqlHelper.ExecuteNonQuery($"update books set id={item.Value} where id={item.Key}",
                      connection);
                    SqlHelper.ExecuteNonQuery($"update bookseq set id_book={item.Value} where id_book={item.Key}",
                      connection);
                    SqlHelper.ExecuteNonQuery(
                      $"update fts_book_content set docid={item.Value} where docid={item.Key}", connection);
                    SqlHelper.ExecuteNonQuery($"update genres set id_book={item.Value} where id_book={item.Key}",
                      connection);
                    updatedBooks++;
                    if (updatedBooks % 100 == 0) {
                      UpdateState($"{updatedBooks} records ({updatedBooks * 100 / booksToUpdate.Count}%) done...",
                        StateKind.Log);
                    }
                  }
                  else {
                    SqlHelper.ExecuteNonQuery($"delete from books where id={item.Key}", connection);
                    // SqlHelper.ExecuteNonQuery($"delete from bookseq id_book={item.Key}", connection);
                    // SqlHelper.ExecuteNonQuery($"delete from fts_book_content where docid={item.Key}", connection);
                    // SqlHelper.ExecuteNonQuery($"delete from genres where id_book={item.Key}", connection);
                  }
                }
                catch (Exception ex) {
                  UpdateState($"Error in item {item.Key}: {ex.Message}", StateKind.Error);
                }
              }

              tr.Commit();
              UpdateState($"Updated {updatedBooks} links", StateKind.Warning);
            }
            catch (Exception ex) {
              UpdateState(ex.Message, StateKind.Error);
              tr.Rollback();
            }
          }
        }
      }

      UpdateState("Updating archives ids...", StateKind.Message);
      using (var connection = SqlHelper.GetConnection()) {
        var archives = new List<KeyValuePair<int, string>>();
        using (var command = SqlHelper.GetCommand(@"select a.id, a.file_name from archives a ORDER BY a.file_name",
          connection))
        using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false)) {
          while (await reader.ReadAsync().ConfigureAwait(false)) {
            archives.Add(new KeyValuePair<int, string>(SqlHelper.GetInt(reader, "id"),
              SqlHelper.GetString(reader, "file_name")));
          }
        }

        var errors = false;
        do {
          for (var i = 0; i < archives.Count; i++) {
            var newId = i + 1;
            if (archives[i].Key == newId) continue;
            try {
              SqlHelper.ExecuteNonQuery($"update archives set id={newId} where id={archives[i].Key}", connection);
              SqlHelper.ExecuteNonQuery($"update books set id_archive={newId} where id_archive={archives[i].Key}",
                connection);
            }
            catch (Exception ex) {
              UpdateState($"Error updating archive id from {archives[i].Key} to {newId} : {ex.Message}",
                StateKind.Error);
              errors = true;
            }
          }
        } while (errors);
      }
    }

    private static List<long> GetExternalIdsFromFile(string fileName) {

      var result = new List<long>();
      if (!File.Exists(fileName)) return result;
      var fData = File.ReadAllLines(fileName);
      foreach (var line in fData) {
        if (string.IsNullOrWhiteSpace(line)) continue;
        var txtId = line.Trim();
        if (long.TryParse(txtId, out var id))
          result.Add(id);
      }
      return result;
    }

    private async Task OptimizeRegisteredArchives(bool unregisterNotFound) {

      _archivesFound = Directory.GetFiles(ArchivesPath, "*fb2*.zip", SearchOption.TopDirectoryOnly);
//            var archivesList = new DirectoryInfo(ArchivesPath).GetFiles("*.zip", SearchOption.TopDirectoryOnly).Select(fileInfo => fileInfo.Name).ToList();
      var idsToRemove = "";
      var dbArchives = new List<string>();
      //SqlHelper.ExecuteNonQuery("delete from archives where [file_name] not like '%fb2-%'");
      using (var connection = SqlHelper.GetConnection()) {
        using (var command = SqlHelper.GetCommand("select id, file_name from archives a", connection)) {
          using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false)) {
            while (await reader.ReadAsync().ConfigureAwait(false)) {
              var archName = SqlHelper.GetString(reader, "file_name");
              if (_archivesFound.Any(s => s.EndsWith(archName, StringComparison.OrdinalIgnoreCase))) {
                dbArchives.Add(archName);
                continue;
              }

              idsToRemove += SqlHelper.GetInt(reader, "id") + ",";
              UpdateState($"Archive not found in local folder: '{archName}'", StateKind.Warning);
            }
          }
        }

        foreach (var archiveName in _archivesFound) {
//                    var archiveFileName = Path.GetFileName(archiveName);
          if (!dbArchives.Any(f => archiveName.EndsWith(f, StringComparison.OrdinalIgnoreCase))) {
            UpdateState($"Archive not registered in DB: {archiveName}", StateKind.Warning);
//                        SqlHelper.ExecuteNonQuery(string.Format("insert into archives (file_name) values ('{0}')", archiveFileName));
//                        UpdateState(string.Format("Archive added to DB: {0}", archiveFileName), StateKind.Message);
          }
        }

        if (unregisterNotFound) {
          idsToRemove = idsToRemove.TrimEnd(',');
          SqlHelper.ExecuteNonQuery($"delete from archives where id in ({idsToRemove})");
        }

        if (!string.IsNullOrWhiteSpace(ArchivesOutputPath))
          SqlHelper.ExecuteNonQuery($"update params set text='{ArchivesOutputPath}' where id=9");
      }
    }

    public async Task CalculateStats() {

      UpdateState($"Loading DB: '{SqlHelper.DataBasePath}' archives: '{ArchivesPath}' ...", StateKind.Log);
      UpdateState("Calculating DB stats...", StateKind.Log);

      var archivesRegistered = await SqlHelper.GetIntFromQuery("select count(1) from archives").ConfigureAwait(false);
      UpdateState($"Found archives to process: {archivesRegistered}", StateKind.Message);

      var totalToRemove = await SqlHelper
        .GetIntFromQuery("select count(1) from books b where b.id_archive not in (select id from archives)")
        .ConfigureAwait(false);

      //by wrong type, lang or removed
      var sql = new StringBuilder(@"select count(1) from books b where b.file_type<>'fb2' ");
      if (RemoveDeleted)
        sql.Append(" or b.deleted=1 ");
      if (RemoveForeign)
        sql.Append(
          " or (b.lang not like 'ru%' and b.lang not like 'ua%' and b.lang not like 'uk%' and b.lang not like 'en%' and b.lang<>'') ");
      totalToRemove += await SqlHelper.GetIntFromQuery(sql.ToString()).ConfigureAwait(false);

      //by genres
      if (GenresToRemove.Length > 0) {
        sql.Clear();
        sql.Append(@"select count(1) from books b join genres g on g.id_book=b.id where 1=0");
        foreach (var g in GenresToRemove) {
          sql.Append($" or g.id_genre='{g}' ");
        }

        totalToRemove += await SqlHelper.GetIntFromQuery(sql.ToString()).ConfigureAwait(false);
      }

//            using (var connection = SqlHelper.GetConnection())
//            {
//                //by sequence
//                if (_seqToRemove.Count > 0)
//                {
//                    sql = new StringBuilder(@"select a.file_name an, b.id, b.deleted from files f
//                    join books b on b.id=f.id_book
//                    join archives a on a.id=f.id_archive
//                    join bookseq bs on bs.id_book=b.id
//                    where bs.id_seq in (");
//                    sql.Append(string.Join(",", _seqToRemove));
//                    sql.Append(")");
//                    sql.Append(" order by a.file_name");
//                    using (var command = SqlHelper.GetCommand(sql.ToString(), connection))
//                    {
//                        using (var reader = command.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                var archName = SqlHelper.GetString(reader, "an");
//                                var id = SqlHelper.GetInt(reader, "id");
//                                var deleted = SqlHelper.GetBoolean(reader, "deleted");
//                                AddToRemovedFiles(_filesData, archName, new BookInfo(id, deleted));
//                            }
//                        }
//                    }
//                }
//            }

      UpdateState($"Found files to remove: {totalToRemove}", StateKind.Message);

      await FixImportedLinks().ConfigureAwait(false);
    }

    public async Task CompressLibrary() {

      if (!string.IsNullOrWhiteSpace(ArchivesOutputPath))
        Directory.CreateDirectory(ArchivesOutputPath);

      await OptimizeRegisteredArchives(RemoveMissingArchivesFromDb).ConfigureAwait(false);

      //general optimization
      UpdateState("Optimizing db tables...", StateKind.Log);

      //by wrong type, lang or removed
      var sql = new StringBuilder(@"delete from books where file_type<>'fb2' ");
      if (RemoveDeleted)
        sql.Append(" or deleted=1 ");
      if (RemoveForeign)
        sql.Append(
          " or (lang not like 'ru%' and lang not like 'ua%' and lang not like 'uk%' and lang not like 'en%' and lang<>'') ");
      var removedCount = SqlHelper.ExecuteNonQuery(sql.ToString());
      UpdateState($"Unregistered books: {removedCount}", StateKind.Message);
      var totalRemoved = removedCount;

      //by genres
      if (GenresToRemove.Length > 0) {
        sql.Clear();
        sql.Append(
          @"delete from books where id in (select b.id from books b join genres g on g.id_book=b.id where 1=0");
        foreach (var g in GenresToRemove)
          sql.Append($" or g.id_genre='{g}' ");
        sql.Append(")");

        removedCount = SqlHelper.ExecuteNonQuery(sql.ToString());
        UpdateState($"Unregistered books: {removedCount}", StateKind.Message);
        totalRemoved += removedCount;
      }

      UpdateState($"Total unregistered books: {totalRemoved}", StateKind.Message);

      await OptimizeArchivesOnDisk().ConfigureAwait(false);
    }

    private bool CheckGenres(string genres, string[] genresToRemove) {

      var len = genres.Length;
      if ((len % 2) != 0) return false;
      //if (len == 2)
      //    return genresToRemove.Any(genre => genre.Equals(genres, StringComparison.OrdinalIgnoreCase));
      for (var i = 0; i < len / 2; i++) {
        var item = genres.Substring(i * 2, 2);
        if (genresToRemove.Any(s => s.Equals(item, StringComparison.OrdinalIgnoreCase)))
          return true;
      }

      return false;
    }
  }
}