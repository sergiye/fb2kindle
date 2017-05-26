using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

namespace fb2mobi
{
    internal class Worker
    {
        private readonly Arguments arguments_;
        private readonly bool error_;
        private readonly string inputFile_;
        private readonly string workDir_;
        private string _outputdir;
        private string bookname_;

        public Worker(Arguments args)
        {
            error_ = true;
            arguments_ = args;
            bookname_ = "";
            _outputdir = "";
            inputFile_ = Path.GetFullPath(arguments_[0]);
            workDir_ = "";

            if (!File.Exists(inputFile_))
                return;

            var inputFileDir = Path.GetDirectoryName(inputFile_);
            var outputFileDir = inputFileDir;
            var bookname = string.Empty;

            // first. get a book file name from explicit output file
            var of = arguments_[1];
            if (of.Length != 0)
            {
                try
                {
                    if (Directory.Exists(of))
                    {
                        // it'a a dir
                        outputFileDir = of;
                    }
                    else
                    {
                        bookname = Path.GetFileNameWithoutExtension(of);
                        var wd = Path.GetDirectoryName(of);
                        if (wd.Length != 0)
                            if (Directory.Exists(wd))
                                outputFileDir = wd;
                    }
                }
                catch (Exception)
                {
                    outputFileDir = inputFileDir;
                    bookname = string.Empty;
                }
            }

            // get a book file name from input file name or from book title
            if (string.IsNullOrEmpty(bookname))
            {
                bookname = Path.GetFileNameWithoutExtension(inputFile_);

                if (arguments_["us"] == "true")
                {
                    try
                    {
                        // get book name
                        var dd = new XmlDocument();
                        dd.Load(inputFile_);
                        XmlNode root = dd["FictionBook"]["description"], data;
                        if ((data = root["title-info"]) != null)
                        {
                            data = data["book-title"];
                            if (data != null)
                            {
                                var val = data.InnerText;
                                if (val.Length > 0)
                                    bookname = val;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                if (arguments_["nt"] != "true")
                    bookname = transliteName(bookname);
            }

            if (string.IsNullOrEmpty(bookname))
                bookname = "fb2mobi";

            char[] trimchars = {'\\'};
            outputFileDir = outputFileDir.TrimEnd(trimchars);
            inputFileDir = inputFileDir.TrimEnd(trimchars);

            var ok = prepareOutputFilePlace(outputFileDir, bookname);
            if (!ok && outputFileDir != inputFileDir)
                ok = prepareOutputFilePlace(inputFileDir, bookname);
            if (!ok)
                ok = prepareOutputFilePlace(Path.GetTempPath() + "\\fb2mobi", bookname);

            if (ok)
            {
                if (arguments_["cl"] == "true")
                {
                    var testdir = Path.GetTempPath() + "\\fb2mobi_";
                    for (var cont = 0;; ++cont)
                    {
                        workDir_ = testdir;
                        workDir_ += cont.ToString();
                        if (!File.Exists(workDir_) && !Directory.Exists(workDir_))
                            break;
                    }
                }
                else
                {
                    workDir_ = _outputdir;
                    workDir_ += bookname_;
                }

                Directory.CreateDirectory(workDir_);
                workDir_ += "\\";
            }

            error_ = !ok;
        }

        public static void print_usage()
        {
            Console.WriteLine("  -us\t Use source book name as output file name.");
            Console.WriteLine("  -nt\t No translite output file name.");
        }

        public bool error()
        {
            return error_;
        }

        public string getBookName(string ext)
        {
            return bookname_ + ext;
        }

        public string getBookName()
        {
            return getBookName("");
        }

        public string getWorkDir()
        {
            return workDir_;
        }

        public string getOutputDir()
        {
            return _outputdir;
        }

        public string getInputFile()
        {
            return inputFile_;
        }


        private bool prepareOutputFilePlace(string dir, string file)
        {
            try
            {
                string name;
                for (var cont = 0;; ++cont)
                {
                    name = file;
                    if (cont != 0)
                        name += cont.ToString();
                    var testName = dir + "\\" + name;
                    if (!File.Exists(testName + ".mobi") && !Directory.Exists(testName))
                        break;
                }

                bookname_ = name;
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
            dd.Load(inputFile_);
            XmlNode bin = dd["FictionBook"]["binary"];
            while (bin != null)
            {
                var fs = new FileStream(getWorkDir() + bin.Attributes["id"].InnerText, FileMode.Create);
                var w = new BinaryWriter(fs);
                w.Write(Convert.FromBase64String(bin.InnerText));
                w.Close();
                fs.Close();
                bin = bin.NextSibling;
            }
        }

        public void transform(string xsl, string name)
        {
            var reader = new XmlTextReader(inputFile_);
            var xslt = new XslCompiledTransform();
            xslt.Load(xsl);
            var writer = new XmlTextWriter(getWorkDir() + name, null) {Formatting = Formatting.Indented};
            xslt.Transform(reader, null, writer, null);
            writer.Close();
        }
    }
}