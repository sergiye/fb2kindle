﻿using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Fb2Kindle
{
    class Program
    {
        private static void ShowHelpText(Assembly asm)
        {
            Console.WriteLine();
            Console.WriteLine(asm.GetName().Name + " <path> [-css <styles.css>] [-d] [-ni]");
            Console.WriteLine();
            Console.WriteLine("<path>: input fb2 file or files mask (ex: *.fb2) or path to *fb2 files");
            Console.WriteLine("-css <styles.css>: styles used in destination book");
            Console.WriteLine("-d: delete source file after successful convertion");
            Console.WriteLine("-c: use compression (slow)");
            Console.WriteLine("-o: hide detailed output");
            Console.WriteLine("-s: add sequence and number to title");
            Console.WriteLine("-ni: no images");
            Console.WriteLine("-ntoc: no table of content");
            Console.WriteLine("-nch: no chapters");
            Console.WriteLine("-save: save parameters to be used at the next start");
            Console.WriteLine("-a: all fb2 books in app folder");
            Console.WriteLine("-r: process files in subfolders (work with -a key)");
            Console.WriteLine("-w: wait for key press on finish");
            Console.WriteLine();
        }

        private static void ShowMainInfo(Assembly asm)
        {
            //            Console.Clear();
            Console.WriteLine();
            var ver = asm.GetName().Version;
            Console.WriteLine(asm.GetName().Name + " Version: " + ver.ToString(3) + "; Build time: " + Util.GetBuildTime(ver).ToString("yyyy/MM/dd HH:mm:ss"));
            var title = Util.GetAttribute<AssemblyTitleAttribute>(asm);
            if (title != null)
                Console.WriteLine(title.Title);
            Console.WriteLine();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            const string allBooksPattern = "*.fb2";
            var wait = false;
            var join = false;
            var save = false;
            var recursive = false;
            var detailedOutput = true;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                ShowMainInfo(asm);

                var appPath = Util.GetAppPath();
                var settingsFile = appPath + @"\config.xml";
                var currentSettings = Util.ReadObjectFromFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
                var bookPath = string.Empty;
                string cssStyles = null;

                if (args.Length == 0)
                {
                    ShowHelpText(asm);
                    Console.Write("Process all files with default parameters (-a -r -w)? Press 'Enter' to continue: ");
                    if (Console.ReadKey().Key != ConsoleKey.Enter)
                        return;
                    wait = true;
                    bookPath = allBooksPattern;
                    recursive = true;
                }
                else
                {
                    for (var j = 0; j < args.Length; j++)
                    {
                        switch (args[j].ToLower().Trim())
                        {
                            case "-nch":
                                currentSettings.nch = true;
                                break;
                            case "-ni":
                                currentSettings.ni = true;
                                break;
                            case "-ntoc":
                                currentSettings.ntoc = true;
                                break;
                            case "-c":
                                currentSettings.c = true;
                                break;
                            case "-s":
                                currentSettings.s = true;
                                break;
                            case "-d":
                                currentSettings.d = true;
                                break;
                            case "-css":
                                if (args.Length > (j + 1))
                                {
                                    var cssFile = args[j + 1];
                                    if (!File.Exists(cssFile))
                                        cssFile = appPath + "\\" + cssFile;
                                    if (!File.Exists(cssFile))
                                    {
                                        Console.WriteLine("css styles file not found");
                                        return;
                                    }
                                    cssStyles = File.ReadAllText(cssFile, Encoding.UTF8);
                                    if (String.IsNullOrEmpty(cssStyles))
                                    {
                                        Console.WriteLine("css styles file is empty");
                                        return;
                                    }
                                    j++;
                                }
                                break;
                            case "-save":
                                save = true;
                                break;
                            case "-w":
                                wait = true;
                                break;
                            case "-r":
                                recursive = true;
                                break;
                            case "-a":
                                bookPath = allBooksPattern;
                                break;
                            case "-j":
                                join = true;
                                break;
                            case "-o":
                                detailedOutput = false;
                                break;
                            default:
                                if (j == 0)
                                    bookPath = args[j];
                                break;
                        }
                    }
                }
                if (string.IsNullOrEmpty(bookPath))
                {
                    Console.WriteLine("No input file");
                    return;
                }
                if (save)
                    Util.WriteObjectToFile(settingsFile, currentSettings, true);

                var workPath = Path.GetDirectoryName(bookPath);
                if (string.IsNullOrEmpty(workPath))
                    workPath = appPath;
                else
                    bookPath = Path.GetFileName(bookPath);
                if (string.IsNullOrEmpty(bookPath))
                    bookPath = allBooksPattern;
                var searchOptions = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(workPath, bookPath, searchOptions);
                if (files.Length == 0)
                    Console.WriteLine("No fb2 files found");
                var conv = new Convertor(currentSettings, cssStyles, detailedOutput);
                if (join)
                    conv.ConvertBookSequence(files);
                else
                    foreach (var file in files)
                        conv.ConvertBook(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (wait)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
}