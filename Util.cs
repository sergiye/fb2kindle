using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Fb2Kindle
{
    internal static class Util
    {
        internal static T GetAttribute<T>(ICustomAttributeProvider assembly, bool inherit = false) where T : Attribute
        {
            var attr = assembly.GetCustomAttributes(typeof(T), inherit);
            foreach (var o in attr)
                if (o is T)
                    return o as T;
            return null;
        }

        internal static void WriteObjectToFile(string filePath, object value, bool useFormatting = false)
        {
            if (value == null) return;
            var xmlFormatting = new XmlWriterSettings { OmitXmlDeclaration = true };
            if (useFormatting)
            {
                xmlFormatting.ConformanceLevel = ConformanceLevel.Document;
                xmlFormatting.Indent = true;
                xmlFormatting.NewLineOnAttributes = true;
            }
            using (Stream file = File.OpenWrite(filePath))
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                new XmlSerializer(value.GetType()).Serialize(file, value, ns);
            }
        }

        internal static T ReadObjectFromFile<T>(string fileName) where T : class
        {
            try
            {
                if (!File.Exists(fileName))
                    return null;
                using (Stream file = File.OpenRead(fileName))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    return (T)serializer.Deserialize(file);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string GetScriptFromResource(string resourceName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(String.Format("Fb2Kindle.{0}", resourceName)))
            {
                if (stream != null)
                    using (var reader = new StreamReader(stream))
                        return reader.ReadToEnd();
                return null;
            }
        }

        internal static bool GetFileFromResource(string resourceName, string filename)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(String.Format("Fb2Kindle.{0}", resourceName)))
            {
                if (stream == null) return false;
                using (Stream file = File.OpenWrite(filename))
                {
                    var buffer = new byte[8 * 1024];
                    int len;
                    while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                        file.Write(buffer, 0, len);
                    return true;
                }
            }
        }

        internal static string GetAppPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        internal static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
        {
            var dir = new DirectoryInfo(sourceDirName);
            var dirs = dir.GetDirectories();
            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);
            var files = dir.GetFiles();
            foreach (var file in files)
                file.CopyTo(Path.Combine(destDirName, file.Name), true);
            if (!copySubDirs) return;
            foreach (var subdir in dirs)
                CopyDirectory(subdir.FullName, Path.Combine(destDirName, subdir.Name), true);
        }

        internal static ImageCodecInfo GetEncoderInfo(string extension)
        {
            extension = extension.ToLower();
            var codecs = ImageCodecInfo.GetImageEncoders();
            for (var i = 0; i < codecs.Length; i++)
                if (codecs[i].FilenameExtension.ToLower().Contains(extension))
                    return codecs[i];
            return null;
        }

        internal static string Value(IEnumerable<XElement> source, string defaultResult = null)
        {
            var value = source.Select(element => element.Value).FirstOrDefault();
            if (value == null || String.IsNullOrEmpty(value.Trim()))
                return defaultResult;
            return value.Trim();
        }

        internal static string AttributeValue(IEnumerable<XElement> source, XName name, string defaultResult = null)
        {
            var value = source.Select(element => (string)element.Attribute(name)).FirstOrDefault();
            if (value == null || String.IsNullOrEmpty(value.Trim()))
                return defaultResult;
            return value.Trim();

        }

        internal static DateTime GetBuildTime(Version ver)
        {
            var buildTime = new DateTime(2000, 1, 1).AddDays(ver.Build).AddSeconds(ver.Revision * 2);
            if (TimeZone.IsDaylightSavingTime(DateTime.Now, TimeZone.CurrentTimeZone.GetDaylightChanges(DateTime.Now.Year)))
                buildTime = buildTime.AddHours(1);
            return buildTime;
        }

        internal static XElement[] RenameTags(XElement root, string tagName, string newName, string className = null, bool clearData = false)
        {
            var list = root.Descendants(tagName).ToArray();
            foreach (var element in list)
                RenameTag(element, newName, className, clearData);
            return list;
        }

//        internal static void UpdateLinksInBook(XElement hrefsContainer, XElement targetsContainer, string sectionFilename)
//        {
//            foreach (var idEl in targetsContainer.Descendants().Where(el => el.Attribute("id") != null))
//            {
//                var noteId = "#" + (string)idEl.Attribute("id");
//                foreach (var a in hrefsContainer.Descendants("a"))
//                {
//                    var href = a.Attribute("href").Value;
//                    if (string.IsNullOrEmpty(href) || !noteId.Equals(href, StringComparison.OrdinalIgnoreCase)) continue;
//                    var value = a.Value;
//                    a.RemoveAll();
//                    a.SetAttributeValue("href", sectionFilename + noteId);
//                    a.Add(new XElement("sup", value));
//                }
//            }
//        }

        internal static void RenameTag(XElement element, string newName, string className = null, bool clearData = false)
        {
            element.Name = newName;
            if (clearData)
            {
                element.Attributes().Remove();
                element.RemoveNodes();
            }
            if (!String.IsNullOrEmpty(className))
                element.SetAttributeValue("class", className);
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
                //WindowStyle = ProcessWindowStyle.Hidden
            };
            var process = Process.Start(startInfo);
            if (addToConsole)
                while (!process.StandardOutput.EndOfStream)
                    Console.WriteLine(process.StandardOutput.ReadLine());
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
