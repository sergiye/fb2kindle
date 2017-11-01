using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

namespace jail.Classes
{
    public class MobiConverter
    {
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
            for (var idx = 0; idx < file.Length; ++idx)
            {
                var ch = char.ToLower(file[idx]);
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
            var firstFileName = string.Empty;
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
                if (string.IsNullOrWhiteSpace(firstFileName))
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

        public static void SaveAnnotation(string inputFile, string outputFile)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(inputFile);
            foreach (XmlNode node in xmlDoc.GetElementsByTagName("annotation"))
            {
                File.WriteAllText(outputFile, node.InnerText);
                break;
            }
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
    }
}