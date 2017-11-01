using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

namespace jail.Classes
{
    public class MobiConverter
    {
        private readonly bool _error;
        private readonly string _inputFile;
        private readonly string _workDir;
        private string _outputdir;

        public bool InitializationError { get { return _error; }}

        public MobiConverter(string filePath)
        {
            _error = true;
            _outputdir = "";
            _inputFile = filePath;
            _workDir = "";

            if (!File.Exists(_inputFile))
                return;

            char[] trimchars = { '\\' };
            var inputFileDir = Path.GetDirectoryName(_inputFile).TrimEnd(trimchars);
            var outputFileDir = inputFileDir;
            var bookname = transliteName(Path.GetFileNameWithoutExtension(_inputFile));
            if (string.IsNullOrEmpty(bookname))
                bookname = "fb2mobi";

            var ok = prepareOutputFilePlace(outputFileDir, bookname);
            if (!ok && outputFileDir != inputFileDir)
                ok = prepareOutputFilePlace(inputFileDir, bookname);
//            if (!ok)
//                ok = prepareOutputFilePlace(Path.GetTempPath() + "\\fb2mobi", bookname);

            if (ok)
            {
                _workDir = _outputdir + bookname;
                Directory.CreateDirectory(_workDir);
                _workDir += "\\";
            }
            _error = !ok;
        }

        private bool prepareOutputFilePlace(string dir, string file)
        {
            try
            {
                string name;
                for (var cont = 0; ; ++cont)
                {
                    name = file;
                    if (cont != 0)
                        name += cont.ToString();
                    var testName = dir + "\\" + name;
                    if (!File.Exists(testName + ".mobi") && !Directory.Exists(testName))
                        break;
                }
                _outputdir = dir + "\\";
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private string transliteName(string file)
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

        public void saveImages()
        {
            var dd = new XmlDocument();
            dd.Load(_inputFile);
            XmlNode bin = dd["FictionBook"]["binary"];
            while (bin != null)
            {
                using (var fs = new FileStream(_workDir + bin.Attributes["id"].InnerText, FileMode.Create))
                {
                    using (var w = new BinaryWriter(fs))
                    {
                        w.Write(Convert.FromBase64String(bin.InnerText));
                        w.Close();
                    }
                    fs.Close();
                }
                bin = bin.NextSibling;
            }
        }

        public string transform(string xsl, string name)
        {
            var outputFile = _workDir + name;
            using (var reader = new XmlTextReader(_inputFile))
            {
                var xslt = new XslCompiledTransform();
                xslt.Load(xsl);
                using (var writer = new XmlTextWriter(outputFile, null) { Formatting = Formatting.Indented })
                {
                    xslt.Transform(reader, null, writer, null);
                    writer.Close();
                }
            }
            return outputFile;
        }
    }
}