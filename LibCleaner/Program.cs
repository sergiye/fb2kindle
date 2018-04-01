using System;

namespace LibCleaner
{
    static class Program
    {
        private static void ShowUsage()
        {
            Console.WriteLine("Usage: -d <database> -a <archives>");
            Console.ReadLine();
        }

        private static void Main(string[] args)
        {
            var i = 0;
            var archivesPath = string.Empty;
            var databasePath = @"d:\media\library\myrulib_flibusta\myrulib.db";
            while (i < args.Length)
            {
                switch (args[i])
                {
                    case "-d":
                        if (args.Length > i + 1)
                        {
                            databasePath = args[i + 1];
                            i++;
                        }
                        break;
                    case "-a":
                        if (args.Length > i + 1)
                        {
                            archivesPath = args[i + 1];
                            i++;
                        }
                        break;
                }
                i++;
            }

            var cleaner = new Cleaner(archivesPath) {DatabasePath = databasePath};
            cleaner.OnStateChanged += (s, kind) => Console.WriteLine(s);
            
            if (!cleaner.CheckParameters())
            {
                ShowUsage();
                return;
            }

            var startedTime = DateTime.Now;
            cleaner.OptimizeArchivesByHash(() =>
                          {
                              var timeWasted = DateTime.Now - startedTime;
                              Console.WriteLine();
                              Console.WriteLine("Time wasted: {0:G}", timeWasted);
                              Console.WriteLine("Press any key to continue...");
                              Console.ReadKey();
                          });
//            cleaner.PrepareStatistics(() =>
//            {
//                Console.WriteLine("Press any key to continue or Esc to exit");
//                var key = Console.ReadKey();
//                if (key.Key == ConsoleKey.Escape)
//                    return;
//
//                var startedTime = DateTime.Now;
//                cleaner.Start(() =>
//                {
//                    var timeWasted = DateTime.Now - startedTime;
//                    Console.WriteLine();
//                    Console.WriteLine("Time wasted: {0:G}", timeWasted);
//                    Console.WriteLine("Press any key to continue...");
//                    Console.ReadKey();
//                });
//            });
        }
    }
}