﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using Fb2Kindle;
using Ionic.Zip;
using jail.Models;
using Simpl.Extensions;

namespace jail.Classes
{
    public static class BookHelper
    {
        public static string Transliterate(this string str)
        {
            string[] lat_up = { "A", "B", "V", "G", "D", "E", "Yo", "Zh", "Z", "I", "Y", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "F", "Kh", "Ts", "Ch", "Sh", "Shch", "\"", "Y", "'", "E", "Yu", "Ya" };
            string[] lat_low = { "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "shch", "\"", "y", "'", "e", "yu", "ya" };
            string[] rus_up = { "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я" };
            string[] rus_low = { "а", "б", "в", "г", "д", "е", "ё", "ж", "з", "и", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ъ", "ы", "ь", "э", "ю", "я" };
            for (int i = 0; i <= 32; i++)
            {
                str = str.Replace(rus_up[i], lat_up[i]);
                str = str.Replace(rus_low[i], lat_low[i]);
            }
            return str;
        }

        public static string TransliteName(string file)
        {
            var rus = new[]
                {
                    'а', 'б', 'в', 'г', 'д', 'е', 'ж', 'з', 'и', 'й'
                    , 'к', 'л', 'м', 'н', 'о', 'п', 'р', 'с', 'т', 'у'
                    , 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ы', 'э', 'ю', 'я'
                    , 'ь', '\\', ':', '/', '?', '*', ' '
                };
            var lat = new[]
                {
                    'a', 'b', 'v', 'g', 'd', 'e', 'j', 'z', 'i', 'y'
                    , 'k', 'l', 'm', 'n', 'o', 'p', 'r', 's', 't', 'u'
                    , 'f', 'h', 'c', 'h', 's', 's', 'i', 'e', 'u', 'a'
                    , '\'', '_', '_', '_', '_', '_', '_'
                };
            var name = "";
            foreach (var t in file)
            {
                var ch = Char.ToLower(t);
                if (ch == '.')
                    break;
                var i = Array.FindIndex(rus, c => c == ch);
                if (i >= 0)
                    name += lat[i];
                else if (ch >= '0' && ch <= 127)
                    name += ch;
                if (name.Length > 31)
                    break;
            }
            return name;
        }

        internal static XElement LoadBookWithoutNs(string bookPath)
        {
            try
            {
                XElement book;
                //book = XDocument.Parse(File.ReadAllText(bookPath), LoadOptions.PreserveWhitespace).Root;
                //book = ReadXDocumentWithInvalidCharacters(bookPath).Root;
                using (Stream file = File.OpenRead(bookPath))
                {
                    book = XElement.Load(file, LoadOptions.PreserveWhitespace);
                }
                XNamespace ns = "";
                foreach (var el in book.DescendantsAndSelf())
                {
                    el.Name = ns.GetName(el.Name.LocalName);
                    var atList = el.Attributes().ToList();
                    el.Attributes().Remove();
                    foreach (var at in atList)
                        el.Add(new XAttribute(ns.GetName(at.Name.LocalName), at.Value));
                }
                book = new XElement("book", book.Elements("description"), book.Elements("body"), book.Elements("binary"));
                return book;
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex, "Unknown file format: " + ex.Message);
                return null;
            }
        }

        internal static int StartProcess(string fileName, string args, bool addToConsole)
        {
            var startInfo = new ProcessStartInfo
                            {
                                FileName = fileName,
                                Arguments = args,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                CreateNoWindow = true,
                            };
            var process = Process.Start(startInfo);
            if (addToConsole)
                while (!process.StandardOutput.EndOfStream)
                    Logger.WriteDebug(process.StandardOutput.ReadLine());
            process.WaitForExit();
            return process.ExitCode;
        }

        public static string GetApplicationPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
            //            var asm = Assembly.GetExecutingAssembly();
            //            var directoryInfo = new FileInfo(asm.Location).Directory;
            //            return directoryInfo != null ? directoryInfo.FullName : Path.GetDirectoryName(asm.Location);
        }

        internal static string ConverterName
        {
            get
            {
                return "Fb2KindleP.exe";
            }
        }

        internal static string ConverterPath
        {
            get
            {
                return string.Format("{0}bin\\{1}", GetApplicationPath(), ConverterName);
            }
        }

        internal static bool ConvertBook(string inputFile, bool writeLog)
        {
            var res = StartProcess(ConverterPath, inputFile, writeLog);
            if (res == 2)
            {
                Logger.WriteWarning("Error converting to mobi");
                return false;
            }
            return true;
        }

        public static void SaveCover(string inputFile, string outputFile)
        {
            var book = LoadBookWithoutNs(inputFile);
            if (book == null) return;
            var coverImage = book.Descendants("coverpage").Elements("image").FirstOrDefault();
            if (coverImage != null)
            {
                var coverPage = (string)coverImage.Attribute("href");
                if (!string.IsNullOrWhiteSpace(coverPage))
                {
                    var node = book.XPathSelectElement(string.Format("descendant::binary[@id='{0}']", coverPage.Replace("#", "")));
                    if (node != null)
                    {
                        File.WriteAllBytes(outputFile, Convert.FromBase64String(node.Value));
                        return;
                    }
                }
            }
            foreach (var binEl in book.Elements("binary"))
            {
                File.WriteAllBytes(outputFile, Convert.FromBase64String(binEl.Value));
                return;
            }
        }

        public static string GetAnnotation(string inputFile, string outputFile)
        {
            if (File.Exists(outputFile))
                return File.ReadAllText(outputFile);
            string result;
            var book = LoadBookWithoutNs(inputFile);
            var desc = book.XPathSelectElement("descendant::annotation");
            if (desc != null)
            {
                result = desc.Value;
                File.WriteAllText(outputFile, result);
                return result;
            }
            result = Regex.Replace(book.XPathSelectElement("descendant::body").Value.Trim().Shorten(1024).Replace("\n", "<br/>"), @"\s+", " ")
                .Replace("<br/> <br/> ", "<br/>");
            File.WriteAllText(outputFile, result);
            return result;
        }

        public static void Transform(string inputFile, string outputFile, string xsl)
        {
            using (var reader = new XmlTextReader(inputFile))
            {
                var xslt = new XslCompiledTransform();
                xslt.Load(xsl);
                using (var writer = new XmlTextWriter(outputFile, null) { Formatting = Formatting.Indented })
                {
                    xslt.Transform(reader, null, writer, null);
                    writer.Close();
                }
            }
        }

        public static void Transform2(string inputFile, string detailsFolder)
        {
            var conv = new Convertor(new DefaultOptions(), null, false);
            conv.ConvertBook(inputFile, true, detailsFolder);
        }

        public static void ExtractZipFile(string archivePath, string fileName, string outputFileName)
        {
            using (var zip = new ZipFile(archivePath))
            {
                var zipEntry = zip.Entries.FirstOrDefault(e => e.FileName.Equals(fileName));
                if (zipEntry == null)
                    throw new FileNotFoundException("Book file not found in archive");
                using (var fs = File.Create(outputFileName))
                    zipEntry.Extract(fs);
            }
        }

        public static byte[] ExtractZipFile(string archivePath, string fileName)
        {
            using (var zip = new ZipFile(archivePath))
            {
                var zipEntry = zip.Entries.FirstOrDefault(e => e.FileName.Equals(fileName));
                if (zipEntry == null)
                    throw new FileNotFoundException("Book file not found in archive");
                var ms = new MemoryStream();
                {
                    zipEntry.Extract(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms.ToArray();
                }
            }
        }

        public static string GetBookDownloadFileName(BookInfo book, string ext = ".fb2")
        {
            var fileName = string.Format("{0}{1}", Regex.Replace(Regex.Replace(String.Format("{0}_{1}",
                    book.Authors.First().FullName.ToLower().Transliterate(),
                    book.Title.ToLower().Transliterate()),
                @"[!@#$%_,. …\[\]\-']", "_"), @"(\p{P})(?<=\1\p{P}+)", "").Trim('_'), ext);
            return fileName;
        }

        public static string GetCorrectedFileName(string filename)
        {
            var fileName = Regex.Replace(filename.ToLower().Transliterate(), @"[!@#$%_ ']", "_");
            return fileName;
        }
    }
}