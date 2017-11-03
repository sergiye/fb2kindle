using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
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

        public static string SaveImages(string inputFile, string outputFolder, bool onlyCover = false)
        {
            var firstFileName = String.Empty;
            var dd = new XmlDocument();
            dd.Load(inputFile);
            XmlNode bin = dd["FictionBook"]["binary"];
            while (bin != null)
            {
                var fileName = outputFolder + bin.Attributes["id"].InnerText;
                using (var fs = new FileStream(fileName, FileMode.Create))
                {
                    using (var w = new BinaryWriter(fs))
                    {
                        w.Write(Convert.FromBase64String(bin.InnerText));
                        w.Close();
                    }
                    fs.Close();
                }
                if (String.IsNullOrWhiteSpace(firstFileName))
                {
                    firstFileName = fileName;
                    if (onlyCover)
                        break;
                }
                bin = bin.NextSibling;
            }
            return firstFileName;
        }

        public static void SaveCover(string inputFile, string outputFile)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(inputFile);
            XmlNode bin = xmlDoc["FictionBook"]["binary"];
            while (bin != null)
            {
                using (var fs = new FileStream(outputFile, FileMode.Create))
                {
                    using (var w = new BinaryWriter(fs))
                    {
                        w.Write(Convert.FromBase64String(bin.InnerText));
                        w.Close();
                    }
                    fs.Close();
                }
                break;
            }
        }

        public static string GetAnnotation(string inputFile, string outputFile)
        {
            if (File.Exists(outputFile))
                return File.ReadAllText(outputFile);

            var result = string.Empty;

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(inputFile);
            var nodes = xmlDoc.GetElementsByTagName("annotation");
            foreach (XmlNode node in nodes)
            {
                result = node.InnerText;
                File.WriteAllText(outputFile, result);
                return result;
            }

            var book = new XElement("bookData", Fb2Kindle.Convertor.LoadBookWithoutNs(inputFile).Elements("body"));
            if (book != null)
            {
                result = Regex.Replace(book.Value.Trim().Shorten(1024).Replace("\n", "<br/>"), @"\s+", " ").Replace("<br/> <br/> ", "<br/>");
            }
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

        public static void ExtractZipFile(string archivePath, string fileName, string outputFileName)
        {
            using (var zip = new ZipFile(archivePath))
            {
                var zipEntry = zip.Entries.FirstOrDefault(e => e.FileName.Equals(fileName));
                if (zipEntry == null)
                    throw new FileNotFoundException("Book file not found in archive");
                using (var fs = System.IO.File.Create(outputFileName))
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
            var fileName = Regex.Replace(String.Format("{0}_{1}{2}",
                    book.Authors.First().FullName.ToLower().Transliterate(),
                    book.Title.ToLower().Transliterate(), ext),
                @"[!@#$%_ ']", "_");
            return fileName;
        }

        public static string GetCorrectedFileName(string filename)
        {
            var fileName = Regex.Replace(filename.ToLower().Transliterate(), @"[!@#$%_ ']", "_");
            return fileName;
        }
    }
}