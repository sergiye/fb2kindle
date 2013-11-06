using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zip;

namespace LibCleaner
{
    class Program
    {
        private static Dictionary<string, List<string>> GetFilesData()
        {
            using (var connection = SqlHelper.GetConnection())
            {
                var result = new Dictionary<string, List<string>>();
                var sql = new StringBuilder("select a.file_name an, f.file_name fn from files f");
                sql.Append(" join books b on b.id=f.id_book");
                sql.Append(" join archives a on a.id=f.id_archive");
                sql.Append(" where b.lang<>'ru' or b.file_type<>'fb2' or b.deleted=1");
                sql.Append(" or b.genres='F1' or b.genres='F9' or b.genres='E1' or b.genres='E3' or b.genres like '4%'");
                connection.Open();
                using (var command = SqlHelper.GetCommand(sql.ToString(), connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader != null && reader.Read())
                        {
                            var archName = DBHelper.GetString(reader, "an");
                            var fileName = DBHelper.GetString(reader, "fn");
                            if (!result.ContainsKey(archName))
                                result.Add(archName, new List<string> { fileName });
                            else
                                result[archName].Add(fileName);
                        }
                    }
                }
                return result;
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Calculating DB stats...");
            var filesData = GetFilesData();
            Console.WriteLine("Found {0} archives to proccess...", filesData.Count);
            foreach (var item in filesData)
            {
                Console.WriteLine("Processing: " + item.Key);
                var archPath = string.Format("..\\fb2.Flibusta.Net\\{0}", item.Key);
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

            //SqlHelper.ExecuteNonQuery();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
