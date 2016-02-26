using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;
using Ionic.Zlib;

namespace LibCleaner
{
    public class Cleaner
    {
        private enum CleanActions
        {
            CalculateStats,
            CompressLibrary
        }

        private class QueueTask
        {
            public CleanActions ActionType { get; private set; }
            public Action OnActionFinish { get; private set; }

            public QueueTask(CleanActions actionType, Action onActionFinish)
            {
                ActionType = actionType;
                OnActionFinish = onActionFinish;
            }
        }

        private Dictionary<string, List<BookInfo>> _filesData;
        private readonly List<int> _seqToRemove;
        private string[] _archivesFound;
        private readonly CommonQueue<QueueTask> _internalTasks;

        public string ArchivesPath { get; set; }
        public string ArchivesOutputPath { get; set; }
        public bool RemoveDeleted { get; set; }
        public bool RemoveForeign { get; set; }
        public bool RemoveMissingArchivesFromDb { get; set; }
        public string[] GenresToRemove { get; set; }

        public string DatabasePath
        {
            get { return SqlHelper.DataBasePath; }
            set { SqlHelper.DataBasePath = value; }
        }

        public enum StateKind
        {
            Log,
            Warning,
            Error,
            Message
        }

        public event Action<string, StateKind> OnStateChanged;

        public Cleaner(string archivesPath)
        {
            ArchivesPath = archivesPath;
            RemoveForeign = true;
            RemoveDeleted = true;
            RemoveMissingArchivesFromDb = true;
            GenresToRemove = GenresListContainer.GetDefaultItems().Where(f=>f.Selected).Select(f=>f.Code).ToArray();
            _seqToRemove = new List<int>
            {
                14437,15976,22715,7028,7083,8303,19890,28738,29139,
                8361,8364,8431,8432,8434,11767,14485,14486,14487,14498,14499,14500,144501,144502,144503,144504,16384,16385,16429,18684,20833,24135,31331,
                3586,10046,12755,31331,3944,4218,14644,31491,30658,25226,6771,27704,7542,8718,28888,15285,18684,15151,31459,
                7061, 7115, 9209, 12277, 16885, 31903,//STALKER
                1066, 12479, 19944, //конан
                204, 5155, //star wars
                329, 15523, 16523, 28755, 30230, 34703, 37029, //Warhammer
                26275, //Гуров — продолжения других авторов
                8166, //Проза еврейской жизни
                19044, 20976, //Вселенная «Метро 2033»
                4908, //новинки  современника
                4258, //сумерки
            };

            _internalTasks = new CommonQueue<QueueTask>();
            _internalTasks.OnExecuteTask += OnInternalTask;
        }

        private void UpdateState(string state, StateKind kind)
        {
            if (OnStateChanged != null)
                OnStateChanged(state, kind);
        }

        public bool CheckParameters()
        {
            //try to use local db file
            if (string.IsNullOrEmpty(SqlHelper.DataBasePath) || !File.Exists(SqlHelper.DataBasePath))
            {
                SqlHelper.DataBasePath = Path.Combine(Environment.CurrentDirectory, "myrulib.db");
            }
            if (!File.Exists(SqlHelper.DataBasePath))
            {
                UpdateState("Database file not found!", StateKind.Error);
                return false;
            }

            //try to get archves folder from db
            if (string.IsNullOrEmpty(ArchivesPath) || !Directory.Exists(ArchivesPath))
            {
                var dbPath = SqlHelper.GetScalarFromQuery("select text from params where id=9") as string;
                if (dbPath != null)
                {
                    var dbFolder = Path.GetDirectoryName(SqlHelper.DataBasePath);
                    if (dbFolder != null)
                        ArchivesPath = Path.Combine(dbFolder, dbPath);
                }
            }
            if (!Directory.Exists(ArchivesPath))
            {
                UpdateState("Archives folder not found!", StateKind.Error);
                return false;
            }

            return true;
        }

        private void OnInternalTask(QueueTask task)
        {
            try
            {
                switch (task.ActionType)
                {
                    case CleanActions.CalculateStats:
                        CalculateStats();
                        break;
                    case CleanActions.CompressLibrary:
                        OptimizeDatabase();
                        CompressLibrary();
                        break;
                }
            }
            catch (Exception ex)
            {
                UpdateState(ex.Message, StateKind.Error);
            }
            finally
            {
                task.OnActionFinish();
            }
        }

        public void PrepareStatistics(Action onTaskFinished)
        {
            _internalTasks.EnqueueTask(new QueueTask(CleanActions.CalculateStats, onTaskFinished));
            //CalculateStats();
        }

        public void Start(Action onTaskFinished)
        {
            _internalTasks.EnqueueTask(new QueueTask(CleanActions.CompressLibrary, onTaskFinished));
            //CompressLibrary();
        }

        private void CalculateStats()
        {
            if (!string.IsNullOrWhiteSpace(ArchivesOutputPath))
                Directory.CreateDirectory(ArchivesOutputPath);

            _filesData = new Dictionary<string, List<BookInfo>>();
            UpdateState(string.Format("Loading DB: '{0}' archives: '{1}' ...", SqlHelper.DataBasePath, ArchivesPath), StateKind.Log);
            UpdateArchivesOnDisk(RemoveMissingArchivesFromDb);

            UpdateState("Calculating DB stats...", StateKind.Log);
            using (var connection = SqlHelper.GetConnection())
            {
                //by wrong type, lang or removed
                var sql = new StringBuilder(@"select a.file_name an, f.file_name fn, b.id, b.deleted from files f
                    join books b on b.id=f.id_book
                    join archives a on a.id=f.id_archive
                    where b.file_type<>'fb2' ");
                if (RemoveDeleted)
                    sql.Append(" or b.deleted=1 ");
                if (RemoveForeign)
                    sql.Append(" or b.lang<>'ru' ");

                sql.Append(" order by a.file_name");
                using (var command = SqlHelper.GetCommand(sql.ToString(), connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var archName = DBHelper.GetString(reader, "an");
                            var fileName = DBHelper.GetString(reader, "fn");
                            var id = DBHelper.GetInt(reader, "id");
                            var deleted = DBHelper.GetBoolean(reader, "deleted");
                            AddToRemovedFiles(_filesData, archName, new BookInfo(id, fileName, deleted));
                        }
                    }
                }
                //by genres
                if (GenresToRemove.Length > 0)
                {
                    sql = new StringBuilder(@"select a.file_name an, f.file_name fn, b.genres genres, b.id, b.deleted from files f
                    join books b on b.id=f.id_book
                    join archives a on a.id=f.id_archive ");
                    for (var i = 0; i < GenresToRemove.Length; i++)
                    {
                        sql.Append(i == 0
                            ? string.Format(" where b.genres like '%{0}%' ", GenresToRemove[i])
                            : string.Format(" or b.genres like '%{0}%' ", GenresToRemove[i]));
                    }
                    sql.Append(" order by a.file_name");
                    using (var command = SqlHelper.GetCommand(sql.ToString(), connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var archName = DBHelper.GetString(reader, "an");
                                var fileName = DBHelper.GetString(reader, "fn");
                                var id = DBHelper.GetInt(reader, "id");
                                var deleted = DBHelper.GetBoolean(reader, "deleted");
                                var genres = DBHelper.GetString(reader, "genres");
                                if (!CheckGenres(genres, GenresToRemove)) continue;
                                AddToRemovedFiles(_filesData, archName, new BookInfo(id, fileName, deleted));
                            }
                        }
                    }
                }
                //by sequence
                if (_seqToRemove.Count > 0)
                {
                    sql = new StringBuilder(@"select a.file_name an, f.file_name fn, b.id, b.deleted from files f
                    join books b on b.id=f.id_book
                    join archives a on a.id=f.id_archive
                    join bookseq bs on bs.id_book=b.id
                    where bs.id_seq in (");
                    sql.Append(string.Join(",", _seqToRemove));
                    sql.Append(")");
                    sql.Append(" order by a.file_name");
                    using (var command = SqlHelper.GetCommand(sql.ToString(), connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var archName = DBHelper.GetString(reader, "an");
                                var fileName = DBHelper.GetString(reader, "fn");
                                var id = DBHelper.GetInt(reader, "id");
                                var deleted = DBHelper.GetBoolean(reader, "deleted");
                                AddToRemovedFiles(_filesData, archName, new BookInfo(id, fileName, deleted));
                            }
                        }
                    }
                }
            }
            foreach (var archiveName in _archivesFound)
            {
                if (!_filesData.Keys.Any(f => archiveName.EndsWith(f, StringComparison.OrdinalIgnoreCase)))
                {
                    UpdateState(string.Format("Archive {0} not registered in DB", archiveName), StateKind.Warning);
                }
            }
            UpdateState(string.Format("Found {0} archives to proccess...", _filesData.Count), StateKind.Message);
            
            var totalToRemove = _filesData.Sum(item => item.Value.Count);
            UpdateState(string.Format("Found {0} files to remove...", totalToRemove), StateKind.Message);
        }

        private void CompressLibrary()
        {
            if (_filesData == null || _filesData.Count == 0)
            {
                UpdateState("Nothing to do!", StateKind.Message);
                return;
            }

            var totalRemoved = 0;
            foreach (var item in _filesData)
            {
                var archPath = string.Format("{0}\\{1}", ArchivesPath, item.Key);
                if (!File.Exists(archPath))
                {
                    //UpdateState("File '{0}' not found", archPath);
                    continue;
                }
                UpdateState("Processing: " + item.Key, StateKind.Log);
                var removedCount = 0;
                using (var zip = new ZipFile(archPath))
                {
                    foreach (var file in item.Value)
                    {
                        if (!zip.ContainsEntry(file.FileName))
                        {
                            //UpdateState("File '{0}' not found in archive", file);
                            continue;
                        }
                        //zip.getsize()
                        zip.RemoveEntry(file.FileName);
                        //UpdateState("Removed: '{0}' from archive", file);
                        removedCount++;
                    }
                    UpdateState(string.Format("Removed {0} files", removedCount), StateKind.Message);
                    if (removedCount <= 0) continue;

                    var outputFile = string.IsNullOrWhiteSpace(ArchivesOutputPath)
                        ? archPath
                        : string.Format("{0}\\{1}", ArchivesOutputPath, item.Key);
                    UpdateState(string.Format("Saving archive {0}", outputFile), StateKind.Log);

                    zip.CompressionLevel = CompressionLevel.BestCompression;
                    zip.Save(outputFile);
                    UpdateState("Done", StateKind.Log);
                }
                totalRemoved += removedCount;
            }

            CleanDatabaseRecords(_filesData);
            UpdateState(string.Format("Total removed {0} files", totalRemoved), StateKind.Message);
        }

        private void AddToRemovedFiles(Dictionary<string, List<BookInfo>> filesData, string archName, BookInfo bookinfo)
        {
            if (!_archivesFound.Any(s => s.EndsWith(archName, StringComparison.OrdinalIgnoreCase)))
                return;
            if (!filesData.ContainsKey(archName))
                filesData.Add(archName, new List<BookInfo> { bookinfo });
            else
            {
                if (filesData[archName].Any(s => s.Id.Equals(bookinfo.Id)))
                    return;
                filesData[archName].Add(bookinfo);
            }
        }

        private bool CheckGenres(string genres, string[] genresToRemove)
        {
            var len = genres.Length;
            if ((len % 2) != 0) return false;
            //if (len == 2)
            //    return genresToRemove.Any(genre => genre.Equals(genres, StringComparison.OrdinalIgnoreCase));
            for (var i = 0; i < len / 2; i++)
            {
                var item = genres.Substring(i * 2, 2);
                if (genresToRemove.Any(s => s.Equals(item, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }

        private void CleanDatabaseRecords(Dictionary<string, List<BookInfo>> filesData, bool remove = false)
        {
            UpdateState("Updating db tables...", StateKind.Log);
            //var totalToUpdate = _filesData.Sum(item => item.Value.Count(f=>!f.Deleted));
            //var updated = 0;
            foreach (var item in filesData)
            {
                var ids = item.Value.Where(f => !f.Deleted).Select(f => f.Id).ToArray();
                if (ids.Length <= 0) continue;
                SqlHelper.ExecuteNonQuery(string.Format("update books set deleted = 1 where id in ({0})", string.Join(",", ids)));
                //updated += ids.Length;
                //UpdateState("Updated: {0} of {1} items", updated, totalToUpdate);
            }
            SqlHelper.ExecuteNonQuery("update books set deleted = 1 where file_type<>'fb2'");
            if (RemoveForeign)
                SqlHelper.ExecuteNonQuery("update books set deleted = 1 where lang<>'ru'");
            if (remove)
            {
                UpdateState("Cleaning db tables...", StateKind.Log);
                SqlHelper.ExecuteNonQuery("delete from books where lang<>'ru' or file_type<>'fb2' or deleted=1");
                SqlHelper.ExecuteNonQuery("delete from files where id_book not in (select id from books)");
                SqlHelper.ExecuteNonQuery("delete from bookseq where id_book not in (select id from books)");
                SqlHelper.ExecuteNonQuery("delete from sequences where id not in (select id_seq from bookseq)");
            }
        }

        private void OptimizeDatabase()
        {
            UpdateState("Optimizing db tables...", StateKind.Log);
            SqlHelper.ExecuteNonQuery("delete from archives where [file_name] not like '%fb2-%'");
            SqlHelper.ExecuteNonQuery("delete from files where id_archive not in (select id from archives)");
            
//            SqlHelper.ExecuteNonQuery("delete from books where lang<>'ru' or file_type<>'fb2' or deleted=1");
//            SqlHelper.ExecuteNonQuery("delete from files where id_book not in (select id from books)");
//            SqlHelper.ExecuteNonQuery("delete from bookseq where id_book not in (select id from books)");
//            SqlHelper.ExecuteNonQuery("delete from sequences where id not in (select id_seq from bookseq)");
        }

        private void UpdateArchivesOnDisk(bool removeFromDb)
        {
            _archivesFound = Directory.GetFiles(ArchivesPath, "*.zip", SearchOption.TopDirectoryOnly);
            //var archivesList = new DirectoryInfo(ArchivesPath).GetFiles("*.zip", SearchOption.TopDirectoryOnly).Select(fileInfo => fileInfo.Name).ToList();

            var idsToRemove = "";
            using (var connection = SqlHelper.GetConnection())
            {
                using (var command = SqlHelper.GetCommand("select id, file_name from archives a", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var archName = DBHelper.GetString(reader, "file_name");
                            //var ai = archivesList.Find(f => f == archName);
                            //if (ai == null)
                            //    idsToRemove += DBHelper.GetInt(reader, "id") + ",";
                            if (_archivesFound.All(s => !s.EndsWith(archName, StringComparison.OrdinalIgnoreCase)))
                            {
                                idsToRemove += DBHelper.GetInt(reader, "id") + ",";
                                UpdateState(string.Format("Archive '{0}' was not found in local folder", archName), StateKind.Warning);
                            }
                        }
                    }
                }

                if (!removeFromDb) return;
                idsToRemove = idsToRemove.TrimEnd(',');
                SqlHelper.ExecuteNonQuery(string.Format("delete from archives where id in ({0})", idsToRemove));
                SqlHelper.ExecuteNonQuery(string.Format("delete from files where id_archive in ({0})", idsToRemove));
                //SqlHelper.ExecuteNonQuery("delete from files where id_archive not in (select id from archives)");

                if (!string.IsNullOrWhiteSpace(ArchivesOutputPath))
                {
                    SqlHelper.ProcessUpdate("params", new[] {new SqlHelper.QueryParameter("text", ArchivesOutputPath)}, "id=9");
                    //SqlHelper.ExecuteNonQuery(string.Format("update params set text={0} where id=9", ArchivesOutputPath));
                }
            }
        }
    }
}
