using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zip;

namespace LibCleaner
{
    class Program
    {
        private static string _archivesPath;

        private static void ShowUsage()
        {
            Console.WriteLine("Usage: -d <database> -a <archives>");
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }
            var i = 0;
            while (i < args.Length)
            {
                switch (args[i])
                {
                    case "-d":
                        if (args.Length > i + 1)
                        {
                            SqlHelper.DataBasePath = args[i + 1];
                            i++;
                        }
                        break;
                    case "-a":
                        if (args.Length > i + 1)
                        {
                            _archivesPath = args[i + 1];
                            i++;
                        }
                        break;
                }
                i++;
            }
            if (string.IsNullOrEmpty(SqlHelper.DataBasePath) || string.IsNullOrEmpty(_archivesPath))
            {
                ShowUsage();
                return;
            }

            Console.WriteLine("Refreshing DB data...");
            var archivesList = new List<string>();//Directory.GetFiles(_archivesPath, "*.zip", SearchOption.TopDirectoryOnly)
            var di = new DirectoryInfo(_archivesPath);
            foreach (var fileInfo in di.GetFiles("*.zip", SearchOption.TopDirectoryOnly))
                archivesList.Add(fileInfo.Name);
            var idsToRemove = "";
            using (var connection = SqlHelper.GetConnection())
            {
                connection.Open();
                using (var command = SqlHelper.GetCommand("select id, file_name from archives a", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var archName = DBHelper.GetString(reader, "file_name");
                            var ai = archivesList.Find(f => f == archName);
                            if (ai == null)
                                idsToRemove += DBHelper.GetInt(reader, "id") + ",";
                        }
                    }
                }
                idsToRemove = idsToRemove.TrimEnd(',');
                SqlHelper.ExecuteNonQuery(string.Format("delete from archives where id in ({0})", idsToRemove));
                SqlHelper.ExecuteNonQuery(string.Format("delete from files where id_archive in ({0})", idsToRemove));
                //SqlHelper.ExecuteNonQuery("delete from files where id_archive not in (select id from archives)");
            }

            Console.WriteLine("Calculating DB stats...");
            idsToRemove = "";
            var filesData = new Dictionary<string, List<string>>();
            using (var connection = SqlHelper.GetConnection())
            {
                var sql = new StringBuilder("select a.file_name an, f.file_name fn from files f");
                sql.Append(" join books b on b.id=f.id_book");
                sql.Append(" join archives a on a.id=f.id_archive");
                sql.Append(" where b.lang<>'ru' or b.file_type<>'fb2' or b.deleted=1");
                sql.Append(" or b.genres='F9' or b.genres='E1' or b.genres='E3'");
                //sql.Append(" or b.genres like '4%'");
                connection.Open();
                using (var command = SqlHelper.GetCommand(sql.ToString(), connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var archName = DBHelper.GetString(reader, "an");
                            var fileName = DBHelper.GetString(reader, "fn");
                            if (!filesData.ContainsKey(archName))
                                filesData.Add(archName, new List<string> { fileName });
                            else
                                filesData[archName].Add(fileName);
                        }
                    }
                }

                //by sequence
                sql.Clear();
                sql.Append("select a.file_name an, f.file_name fn, b.id from files f");
                sql.Append(" join books b on b.id=f.id_book");
                sql.Append(" join archives a on a.id=f.id_archive");
                sql.Append(" join bookseq bs on bs.id_book=b.id");
                sql.Append(" where (b.deleted is null or b.deleted<>1) and (bs.id_seq in ");
                //Bash.org, газеты
                sql.Append("(14437,15976,22715,7028,7083,8303,19890,28738,29139,");
                //журнал Если
                sql.Append("8361,8364,8431,8432,8434,11767,14485,14486,14487,14498,14499,14500,144501,144502,144503,144504,16384,16385,16429,18684,20833,24135,31331,");
                sql.Append("3586,10046,12755,31331,3944,4218,14644,31491,30658,25226,6771,27704,7542,8718,28888,15285,18684,15151,31459))");
                using (var command = SqlHelper.GetCommand(sql.ToString(), connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var archName = DBHelper.GetString(reader, "an");
                            var fileName = DBHelper.GetString(reader, "fn");
                            if (!filesData.ContainsKey(archName))
                                filesData.Add(archName, new List<string> { fileName });
                            else
                                filesData[archName].Add(fileName);
                            idsToRemove += DBHelper.GetInt(reader, "id") + ",";
                        }
                    }
                }
            }
            Console.WriteLine("Found {0} archives to proccess...", filesData.Count);
            foreach (var item in filesData)
            {
                Console.WriteLine("Processing: " + item.Key);
                var archPath = string.Format("{1}\\{0}", item.Key, _archivesPath);
                if (!File.Exists(archPath))
                {
                    Console.WriteLine("File '{0}' not found", archPath);
                    continue;
                }
                var removedCount = 0;
                using (var zip = new ZipFile(archPath))
                {
                    foreach (var file in item.Value)
                    {
                        if (!zip.ContainsEntry(file))
                        {
//                            Console.WriteLine("File '{0}' not found in archive", file);
                            continue;
                        }
                        zip.RemoveEntry(file);
                        //Console.WriteLine("Removed: '{0}' from archive", file);
                        removedCount++;
                    }
                    Console.WriteLine("Removed {0} files", removedCount);
                    if (removedCount <= 0) continue;
                    Console.Write("Saving archive...");
                    zip.Save();
                    Console.WriteLine("Done");
                }
            }

            Console.Write("Cleaning db tables...");
            SqlHelper.ExecuteNonQuery("delete from books where lang<>'ru' or file_type<>'fb2' or deleted=1");
            idsToRemove = idsToRemove.TrimEnd(',');
            SqlHelper.ExecuteNonQuery(string.Format("delete from books where id in ({0})", idsToRemove));
            SqlHelper.ExecuteNonQuery("delete from files where id_book not in (select id from books)");
            SqlHelper.ExecuteNonQuery("delete from bookseq where id_book not in (select id from books)");
            SqlHelper.ExecuteNonQuery("delete from sequences where id not in (select id_seq from bookseq)");

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
