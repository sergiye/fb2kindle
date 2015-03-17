﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;
using Ionic.Zlib;

namespace LibCleaner
{
    class Program
    {
        private static string _archivesPath;

        private static void ShowUsage(string warning)
        {
            if (!string.IsNullOrEmpty(warning))
                Console.WriteLine(warning);
            Console.WriteLine("Usage: -d <database> -a <archives>");
            Console.ReadLine();
        }

        static void Main(string[] args)
        {
//            if (args.Length == 0)
//            {
//                ShowUsage("no parameters used");
//                return;
//            }
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

            //try to use local db file
            if (string.IsNullOrEmpty(SqlHelper.DataBasePath) || !File.Exists(SqlHelper.DataBasePath))
            {
                SqlHelper.DataBasePath = Path.Combine(Environment.CurrentDirectory, "myrulib.db");
            }
            if (!File.Exists(SqlHelper.DataBasePath))
            {
                ShowUsage("Database file not found!");
                return;
            }

            //try to get archves folder from db
            if (string.IsNullOrEmpty(_archivesPath) || !Directory.Exists(_archivesPath))
            {
                var dbPath = SqlHelper.GetScalarFromQuery("select text from params where id=9") as string;
                if (dbPath != null)
                {
                    var dbFolder = Path.GetDirectoryName(SqlHelper.DataBasePath);
                    if (dbFolder != null) 
                        _archivesPath = Path.Combine(dbFolder, dbPath);
                }
            }
            if (!Directory.Exists(_archivesPath))
            {
                ShowUsage("Archives folder not found!");
                return;
            }

            Console.WriteLine("Loading DB: '{0}' archives: '{1}' ...", SqlHelper.DataBasePath, _archivesPath);
            //RemoveMissingArchives();

            Console.WriteLine("Calculating DB stats...");
            var idsToRemove = "";
            var filesData = new Dictionary<string, List<string>>();
            using (var connection = SqlHelper.GetConnection())
            {
                //e0,e1,e2,e3 = юмор
                //96 - политика
                //0c - о бизнесе популярно
                //с1 - биографии и мемуары
                //с2 - публицистика
                //с3 - критика
                //с4 - Искусство и Дизайн
                //d2 = эзотерика
                //45, f9 = эротика
                var genresToRemove = new List<string> {"E0", "E1", "E2", "E3", "96", "D2", "0C", "C1", "C2", "C3", "C4", "F9", "45"};

                var sql = new StringBuilder(@"select a.file_name an, f.file_name fn, b.genres genres from files f
                    join books b on b.id=f.id_book
                    join archives a on a.id=f.id_archive
                    where b.lang<>'ru' or b.file_type<>'fb2' or b.deleted=1");
                foreach (var genre in genresToRemove)
                    sql.Append(string.Format(" or b.genres like '%{0}%' ", genre));
                using (var command = SqlHelper.GetCommand(sql.ToString(), connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var archName = DBHelper.GetString(reader, "an");
                            var fileName = DBHelper.GetString(reader, "fn");
                            var genres = DBHelper.GetString(reader, "genres");
                            if (!CheckGenres(genres, genresToRemove)) continue;
                            if (!filesData.ContainsKey(archName))
                                filesData.Add(archName, new List<string> { fileName });
                            else
                                filesData[archName].Add(fileName);
                        }
                    }
                }

                //by sequence
                sql = new StringBuilder(@"select a.file_name an, f.file_name fn, b.id from files f
                    join books b on b.id=f.id_book
                    join archives a on a.id=f.id_archive
                    join bookseq bs on bs.id_book=b.id
                    where (b.deleted<>1) and (bs.id_seq in
                    (14437,15976,22715,7028,7083,8303,19890,28738,29139,
                    8361,8364,8431,8432,8434,11767,14485,14486,14487,14498,14499,14500,144501,144502,144503,144504,16384,16385,16429,18684,20833,24135,31331,
                    3586,10046,12755,31331,3944,4218,14644,31491,30658,25226,6771,27704,7542,8718,28888,15285,18684,15151,31459))");
                using (var command = SqlHelper.GetCommand(sql.ToString(), connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var archName = DBHelper.GetString(reader, "an");
                            var fileName = DBHelper.GetString(reader, "fn");
                            if (!filesData.ContainsKey(archName))
                                filesData.Add(archName, new List<string> {fileName});
                            else
                            {
                                if (filesData[archName].Contains(fileName))
                                    continue;
                                filesData[archName].Add(fileName);
                            }
                            idsToRemove += DBHelper.GetInt(reader, "id") + ",";
                        }
                    }
                }
            }
            Console.WriteLine("Found {0} archives to proccess...", filesData.Count);
            var totalRemoved = 0;
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
                        totalRemoved ++;
                        if (!zip.ContainsEntry(file))
                        {
//                            Console.WriteLine("File '{0}' not found in archive", file);
                            continue;
                        }
                        //zip.getsize()
                        zip.RemoveEntry(file);
                        //Console.WriteLine("Removed: '{0}' from archive", file);
                        removedCount++;
                    }
                    Console.WriteLine("Removed {0} files", removedCount);
                    if (removedCount <= 0) continue;
                    Console.Write("Saving archive...");
                    zip.CompressionLevel = CompressionLevel.BestCompression;
                    zip.Save();
                    Console.WriteLine("Done");
                }
            }

            CleanDatabaseRecords(idsToRemove);
            Console.WriteLine("Total removed {0} files", totalRemoved);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static bool CheckGenres(string genres, List<string> genresToRemove)
        {
            var len = genres.Length;
            if ((len % 2) != 0) return false;
//            if (len == 2)
//                return genresToRemove.Any(genre => genre.Equals(genres, StringComparison.OrdinalIgnoreCase));
            for (var i = 0; i < len / 2; i++)
            {
                var item = genres.Substring(i*2, 2);
                if (genresToRemove.Any(genre => genre.Equals(item, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }

        private static void CleanDatabaseRecords(string idsToRemove)
        {
            Console.Write("Cleaning db tables...");
            SqlHelper.ExecuteNonQuery("delete from books where lang<>'ru' or file_type<>'fb2' or deleted=1");
            idsToRemove = idsToRemove.TrimEnd(',');
            SqlHelper.ExecuteNonQuery(string.Format("delete from books where id in ({0})", idsToRemove));
            SqlHelper.ExecuteNonQuery("delete from files where id_book not in (select id from books)");
            SqlHelper.ExecuteNonQuery("delete from bookseq where id_book not in (select id from books)");
            SqlHelper.ExecuteNonQuery("delete from sequences where id not in (select id_seq from bookseq)");
        }

        private static void RemoveMissingArchives()
        {
            var archivesList = Directory.GetFiles(_archivesPath, "*.zip", SearchOption.TopDirectoryOnly);
            //var archivesList = new DirectoryInfo(_archivesPath).GetFiles("*.zip", SearchOption.TopDirectoryOnly).Select(fileInfo => fileInfo.Name).ToList();
            var idsToRemove = "";
            using (var connection = SqlHelper.GetConnection())
            {
                using (var command = SqlHelper.GetCommand("select id, file_name from archives a", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader != null && reader.Read())
                        {
                            var archName = DBHelper.GetString(reader, "file_name");
//                            var ai = archivesList.Find(f => f == archName);
//                            if (ai == null)
//                                idsToRemove += DBHelper.GetInt(reader, "id") + ",";
                            if (archivesList.All(s => !s.EndsWith(archName)))
                                idsToRemove += DBHelper.GetInt(reader, "id") + ",";
                        }
                    }
                }
                idsToRemove = idsToRemove.TrimEnd(',');
                SqlHelper.ExecuteNonQuery(string.Format("delete from archives where id in ({0})", idsToRemove));
                SqlHelper.ExecuteNonQuery(string.Format("delete from files where id_archive in ({0})", idsToRemove));
                //SqlHelper.ExecuteNonQuery("delete from files where id_archive not in (select id from archives)");
            }
        }
    }
}
