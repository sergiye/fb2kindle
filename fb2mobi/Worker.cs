using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

using CommandLine.Utility;

namespace fb2mobi
{
    class Worker
    {
        bool error_;

        Arguments arguments_;
        
        string bookname_;
        string outputdir_;
        string inputFile_;
        string workDir_;

        public static void print_usage()
        {
            Console.WriteLine("  -us\t Use source file name as output.");
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
            return outputdir_;
        }

        public string getInputFile()
        {
            return inputFile_;
        }

        public Worker(Arguments args)
        {
            error_      = true;
            
            arguments_  = args;
           
            bookname_   = "";
            outputdir_  = "";
            inputFile_  = Path.GetFullPath(arguments_[0]);
            workDir_    = "";

            if (!File.Exists(inputFile_))
                return;

            string inputFileDir     = Path.GetDirectoryName(inputFile_);
            string outputFileDir    = inputFileDir;
            string bookname         = "";

            // first. get a book file name from explicit output file
            string of = arguments_[1];
            if (of.Length != 0)
            {
                try
                {
                    if(Directory.Exists(of)){ // it'a a dir
                        outputFileDir = of;
                    }
                    else {
                        bookname = Path.GetFileNameWithoutExtension(of);

                        string wd = Path.GetDirectoryName(of);
                        if (wd.Length != 0)
                            if (Directory.Exists(wd))
                                outputFileDir = wd;
                    }

                }
                catch (Exception)
                {
                    outputFileDir   = inputFileDir;
                    bookname        = "";
                }
            }

            // get a book file name from input file name or from book title
            if (bookname.Length == 0)
            {
                bookname = Path.GetFileNameWithoutExtension(inputFile_);

                if (arguments_["us"] != "true")
                {
                    try {
                        // get book name
                        XmlDocument dd = new XmlDocument();
                        dd.Load(inputFile_);
                        XmlNode root = dd["FictionBook"]["description"], data;
                        if ((data = root["title-info"]) != null)
                        {
                            data = data["book-title"];
                            if (data != null)
                            {
                                string val = data.InnerText;
                                if (val.Length > 0)
                                    bookname = val;
                            }
                        }
                    } catch (Exception) { }
                }

                if (arguments_["nt"] != "true")
                    bookname = transliteName(bookname);
            }

            if (bookname.Length == 0)
                bookname = "fb2mobi";

            char[] trimchars = { '\\' };
            outputFileDir = outputFileDir.TrimEnd(trimchars);
            inputFileDir = inputFileDir.TrimEnd(trimchars);

            bool ok = prepareOutputFilePlace(outputFileDir, bookname);
            if(!ok && outputFileDir != inputFileDir)
                ok = prepareOutputFilePlace(inputFileDir, bookname);
            if(!ok)
                ok = prepareOutputFilePlace(Path.GetTempPath()+ "\\fb2mobi", bookname);

            if (ok)
            {
                if (arguments_["cl"] == "true")
                {
                    string testdir = Path.GetTempPath() + "\\fb2mobi_";
                    for (int cont = 0; ; ++cont)
                    {
                        workDir_ = testdir;
                        workDir_ += cont.ToString();
                        if (!File.Exists(workDir_) && !Directory.Exists(workDir_))
                            break;
                    }
                }
                else
                {
                    workDir_ = outputdir_;
                    workDir_ += bookname_;
                }

                Directory.CreateDirectory(workDir_);
                workDir_ += "\\";
            }

            error_ = !ok;
        }


        bool prepareOutputFilePlace(string dir, string file)
        {
            try
            {
                string name;
                for(int cont = 0;; ++cont){
                    name = file;
                    if(cont != 0)
                        name += cont.ToString();
                    string testName = dir + "\\" + name;
                    if (!File.Exists(testName + ".mobi") && !Directory.Exists(testName))
                        break;
                }

                bookname_ = name;
                outputdir_ = dir + "\\";

            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        string transliteName(string file)
        {
            char[] rus = new char[] {
                    'а', 'б', 'в', 'г', 'д', 'е', 'ж', 'з', 'и', 'й'
                  , 'к', 'л', 'м', 'н', 'о', 'п', 'р', 'с', 'т', 'у'
                  , 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ы', 'э', 'ю', 'я'
                  , 'ь', '\\',':', '/', '?', '*', ' '};
            char[] lat = new char[] {
                    'a', 'b', 'v', 'g', 'd', 'e', 'j', 'z', 'i', 'y'
                  , 'k', 'l', 'm', 'n', 'o', 'p', 'r', 's', 't', 'u'
                  , 'f', 'h', 'c', 'h', 's', 's', 'i', 'e', 'u', 'a'
                  , '\'', '_', '_', '_', '_', '_', '_'};
            string name = "";
            for (int idx = 0; idx < file.Length; ++idx)
            {
                char ch = char.ToLower(file[idx]);
                if (ch == '.')
                    break;
                int i = Array.FindIndex<char>(rus, delegate(char c) { return c == ch; });
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
            XmlDocument dd = new XmlDocument();
            dd.Load(inputFile_);
            XmlNode bin = dd["FictionBook"]["binary"];
            while (bin != null)
            {
                FileStream fs = new FileStream(getWorkDir() + bin.Attributes["id"].InnerText, FileMode.Create);
                BinaryWriter w = new BinaryWriter(fs);
                w.Write(Convert.FromBase64String(bin.InnerText));
                w.Close();
                fs.Close();
                bin = bin.NextSibling;
            }
        }

        public void transform(string xsl, string name)
        {
            XmlTextReader reader = new XmlTextReader(inputFile_);

            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(xsl);

            XmlTextWriter writer = new XmlTextWriter(getWorkDir() + name, null);
            writer.Formatting = Formatting.Indented;

            xslt.Transform(reader, null, writer, null);

            writer.Close();
        }
    }
}
