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
            var filesData = new Dictionary<string, List<BookInfo>>();
            using (var connection = SqlHelper.GetConnection())
            {
                var genresToRemove = new List<string>
                {
                    "E0", "E1", "E2", "E3", //e0,e1,e2,e3 = юмор
                    
                    "44", //44 - Короткие любовные романы
                    "45", //45 - Эротика

                    "71", //71 - Поэзия
                    "72", //72 - Драматургия
                    
                    "91", //91 - История
                    "92", //92 - Психология
                    "93", //93 - Культурология
                    "94", //94 - Религиоведение
                    "95", //95 - Философия
                    "96", //96 - Политика
                    "97", //97 - Деловая литература
                    "98", //98 - Юриспруденция
                    "99", //99 - Языкознание
                    "9A", //9A - Медицина
                    "9B", //9B - Физика
                    "9C", //9C - Математика
                    "9D", //9D - Химия
                    "9E", //9E - Биология
                    "9F", //9F - Технические науки
                    "90", //90 - Научная литература
                    
                    "04", //04 - Банковское дело
                    "00", //00 - Экономика
                    "09", //09 - Корпоративная культура
                    "0C", //0C - О бизнесе популярно
                    "0F", //0F - Справочники по экономике

                    "A1", //A1 = Интернет
                    "A2", //A2 = Программирование
                    "A3", //A3 = Компьютерное железо
                    "A4", //A4 = Программы
                    "A5", //A5 = Базы данных
                    "A6", //A6 = ОС и сети
                    "A0", //A0 = Компьтерная литература

                    "B1", //B1 = Энциклопедии
                    "B2", //B2 = Словари
                    "B3", //B3 = Справочники
                    "B4", //B4 = Руководства
                    "B0", //B0 = Справочная литература

                    "C1", //с1 - Биографии и Мемуары
                    "C2", //с2 - Публицистика
                    "C3", //с3 - критика
                    "C4", //с4 - Искусство и Дизайн
                    "C5", //C5 - Документальная литература
                    
                    "D1", //D1 = Религия (?)
                    "D2", //D2 = эзотерика
                    "D3", //D3 = Самосовершенствование
                    "D0", //D0 = Религиозная литература

                    "F1", //F1 - Кулинария
                    "F2", //F2 - Домашние животные
                    "F3", //F3 - Хобби и ремесла
                    "F4", //F4 - Развлечения
                    "F5", //F5 - Здоровье
                    "F6", //F6 - Сад и огород
                    "F7", //F7 - Сделай сам
                    "F8", //F8 - Спорт
                    "F9", //F9 - Эротика, Секс
                    "F0", //F0 - Домоводство
                    "FA", //FA - Путеводители
                };

                var sql = new StringBuilder(@"select a.file_name an, f.file_name fn, b.genres genres, b.id, b.deleted from files f
                    join books b on b.id=f.id_book
                    join archives a on a.id=f.id_archive
                    where b.genres like '%45%' ");
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
                            var id = DBHelper.GetInt(reader, "id");
                            var deleted = DBHelper.GetBoolean(reader, "deleted");
                            var genres = DBHelper.GetString(reader, "genres");
                            if (!CheckGenres(genres, genresToRemove)) continue;
                            AddToRemovedFiles(filesData, archName, new BookInfo(id, fileName, deleted));
                        }
                    }
                }
                //by wrong type, lang or removed
                sql = new StringBuilder(@"select a.file_name an, f.file_name fn, b.id, b.deleted from files f
                    join books b on b.id=f.id_book
                    join archives a on a.id=f.id_archive
                    where b.lang<>'ru' or b.file_type<>'fb2' or b.deleted=1");
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
                            AddToRemovedFiles(filesData, archName, new BookInfo(id, fileName, deleted));
                        }
                    }
                }
                //by sequence
                sql = new StringBuilder(@"select a.file_name an, f.file_name fn, b.id, b.deleted from files f
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
                            var id = DBHelper.GetInt(reader, "id");
                            var deleted = DBHelper.GetBoolean(reader, "deleted");
                            AddToRemovedFiles(filesData, archName, new BookInfo(id, fileName, deleted));
                        }
                    }
                }
            }
            Console.WriteLine("Found {0} archives to proccess...", filesData.Count);

            var totalToRemove = filesData.Sum(item => item.Value.Count);
            Console.WriteLine("Found {0} files to remove...", totalToRemove);
            Console.WriteLine("Press any key to continue or Esc to exit");
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Escape)
            {
                return;
            }

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
                        if (!zip.ContainsEntry(file.FileName))
                        {
//                            Console.WriteLine("File '{0}' not found in archive", file);
                            continue;
                        }
                        //zip.getsize()
                        zip.RemoveEntry(file.FileName);
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
                totalRemoved += removedCount;
            }

            CleanDatabaseRecords(filesData);
            Console.WriteLine("Total removed {0} files", totalRemoved);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static bool AddToRemovedFiles(Dictionary<string, List<BookInfo>> filesData, string archName, BookInfo bookinfo)
        {
            if (!filesData.ContainsKey(archName))
                filesData.Add(archName, new List<BookInfo> { bookinfo });
            else
            {
                if (filesData[archName].Any(s => s.Id.Equals(bookinfo.Id)))
                    return false;
                filesData[archName].Add(bookinfo);
            }
            return true;
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
                if (genresToRemove.Any(s => s.Equals(item, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }

        private static void CleanDatabaseRecords(Dictionary<string, List<BookInfo>> filesData, bool remove = false)
        {
            Console.WriteLine("Updating db tables...");
            var totalToUpdate = filesData.Sum(item => item.Value.Count(f=>!f.Deleted));
            var updated = 0;
            foreach (var item in filesData)
            {
                var ids = item.Value.Where(f=>!f.Deleted).Select(f => f.Id).ToArray();
                if (ids.Length <= 0) continue;
                SqlHelper.ExecuteNonQuery(string.Format("update books set deleted = 1 where id in ({0})", string.Join(",", ids)));
                updated += ids.Length;
                Console.WriteLine("Updated: {0} of {1} items", updated, totalToUpdate);
            }
            SqlHelper.ExecuteNonQuery("update books set deleted = 1 where lang<>'ru' or file_type<>'fb2'");
            if (remove)
            {
                Console.Write("Cleaning db tables...");
                SqlHelper.ExecuteNonQuery("delete from books where lang<>'ru' or file_type<>'fb2' or deleted=1");
                SqlHelper.ExecuteNonQuery("delete from files where id_book not in (select id from books)");
                SqlHelper.ExecuteNonQuery("delete from bookseq where id_book not in (select id from books)");
                SqlHelper.ExecuteNonQuery("delete from sequences where id not in (select id_seq from bookseq)");
            }
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
                        while (reader.Read())
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
