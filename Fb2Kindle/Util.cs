using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
                                RedirectStandardError = true,
                                CreateNoWindow = true,
                                StandardOutputEncoding = Encoding.UTF8
                                //WindowStyle = ProcessWindowStyle.Hidden
                            };
//            using (var process = Process.Start(startInfo))
//            {
//                if (addToConsole)
//                {
//                    while (!process.StandardOutput.EndOfStream)
//                        WriteLine(process.StandardOutput.ReadLine());
//                }
//                process.WaitForExit();
//                return process.ExitCode;
//            }

//            using (var process = Process.Start(startInfo))
//            {
//                using (var reader = process.StandardOutput)
//                {
//                    string result = reader.ReadToEnd();
//                    if (addToConsole) WriteLine(result);
//                }
//                return process.ExitCode;
//            }

            using (var process = new Process {StartInfo = startInfo})
            {
                process.OutputDataReceived += (sender, e) =>
                                              {
                                                  if (addToConsole) WriteLine(e.Data);
                                              };
                process.ErrorDataReceived += (sender, e) =>
                                             {
                                                 if (addToConsole) WriteLine(e.Data, ConsoleColor.Red);
                                             };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        internal static void WriteLine(string message = null, ConsoleColor? color = null, ConsoleColor? backColor = null)
        {
            Write(message, color, backColor, true);
        }

        internal static void Write(string message = null, ConsoleColor? color = null, ConsoleColor? backColor = null, bool newLine = false)
        {
            if (backColor.HasValue)
                Console.BackgroundColor = backColor.Value;
            if (color.HasValue)
                Console.ForegroundColor = color.Value;
            if (newLine)
                Console.WriteLine(message);
            else
                Console.Write(message);
            Console.ResetColor();
        }

        #region Images

        internal static ImageCodecInfo GetEncoderInfo(string extension)
        {
            extension = extension.ToLower();
            var codecs = ImageCodecInfo.GetImageEncoders();
            for (var i = 0; i < codecs.Length; i++)
                if (codecs[i].FilenameExtension.ToLower().Contains(extension))
                    return codecs[i];
            return null;
        }

        internal static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID.Equals(format.Guid));
        }

        internal static string GetMimeType(this Image image)
        {
            return image.RawFormat.GetMimeType();
        }

        internal static string GetMimeType(this ImageFormat imageFormat)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            return codecs.First(codec => codec.FormatID == imageFormat.Guid).MimeType;
        }

        internal static ImageFormat GetImageFormatFromMimeType(string contentType, ImageFormat defaultResult)
        {
            if (contentType.Equals(ImageFormat.Jpeg.GetMimeType(), StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Jpeg;
            }
            if (contentType.Equals(ImageFormat.Bmp.GetMimeType(), StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Bmp;
            }
            if (contentType.Equals(ImageFormat.Png.GetMimeType(), StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Png;
            }
//            foreach (var codecInfo in ImageCodecInfo.GetImageEncoders())
//            {
//                if (codecInfo.MimeType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
//                {
//
//                }
//            }
            return defaultResult;
        }

        internal static Image GrayScale(Image img, bool fast, ImageFormat format)
        {
            Stream imageStream = new MemoryStream();
            if (fast)
            {
                using (var bmp = new Bitmap(img))
                {
                    var gsBmp = MakeGrayscale3(bmp);
                    gsBmp.Save(imageStream, format);
                }
            }
            else
            {
                using (var bmp = new Bitmap(img))
                {
                    for (var y = 0; y < bmp.Height; y++)
                    for (var x = 0; x < bmp.Width; x++)
                    {
                        var c = bmp.GetPixel(x, y);
                        var rgb = (c.R + c.G + c.B) / 3;
                        bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                    }
                    bmp.Save(imageStream, format);
                }
            }
            return Image.FromStream(imageStream);
        }

        internal static Bitmap MakeGrayscale3(Bitmap original)
        {
            var newBitmap = new Bitmap(original.Width, original.Height);
            var g = Graphics.FromImage(newBitmap);
            var colorMatrix = new ColorMatrix(new[]
                                              {
                                                  new[] {.3f, .3f, .3f, 0, 0},
                                                  new[] {.59f, .59f, .59f, 0, 0},
                                                  new[] {.11f, .11f, .11f, 0, 0},
                                                  new float[] {0, 0, 0, 1, 0},
                                                  new float[] {0, 0, 0, 0, 1}
                                              });
            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            g.Dispose();
            return newBitmap;
        }

        #endregion Images

        internal static void SaveXmlToFile(XElement xml, string file)
        {
            if (Debugger.IsAttached)
                xml.Save(file);
            else
                xml.Save(file, SaveOptions.DisableFormatting);
        }
    }
}
