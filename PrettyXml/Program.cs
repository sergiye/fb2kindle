using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace PrettyXml
{
    class Program
    {
        private static void PrettyXml(string xmlPath)
        {
            var stringBuilder = new StringBuilder();
            var element = XElement.Parse(File.ReadAllText(xmlPath));
            var settings = new XmlWriterSettings {OmitXmlDeclaration = true, Indent = true, NewLineOnAttributes = true};
            using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                element.Save(xmlWriter);
            }
            File.WriteAllText(xmlPath, stringBuilder.ToString());
        }

        static void Main(string[] args)
        {
            foreach (var filePath in args)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        Console.WriteLine("Processing: {0}", filePath);
                        PrettyXml(filePath);
                    }
                    else
                    {
                        Console.WriteLine("File not found: {0}", filePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            if (args.Length == 0)
                Console.WriteLine("Please add file path(s) in parameters");
            else
                Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}
