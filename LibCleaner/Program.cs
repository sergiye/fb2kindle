using System;

namespace LibCleaner
{
    class Program
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
            var databasePath = string.Empty;
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

            var cleaner = new Cleaner(archivesPath);
            cleaner.DatabasePath = databasePath;
            cleaner.OnStateChanged += Console.WriteLine;
            
            if (!cleaner.CheckParameters())
            {
                ShowUsage();
                return;
            }

            cleaner.PrepareStatistics();

            Console.WriteLine("Press any key to continue or Esc to exit");
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Escape)
                return;

            cleaner.Start();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}