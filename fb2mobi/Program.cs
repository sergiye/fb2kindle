using System;
using System.IO;

using CommandLine.Utility;

namespace fb2mobi
{
    class FB2mobiMain
    {
        private static string kindlegen = "kindlegen.exe";
        private static string opfxsl    = "FB2_2_opf.xsl";
        private static string bodyxsl   = "FB2_2_xhtml.xsl";
        private static string ncxxsl    = "FB2_2_ncx.xsl";

        static void print_usage()
        {
            Console.WriteLine("Usage: fb2mobi <file.fb2> [<output.mobi>] [{-,/,--}param]");
            Console.WriteLine("  -nc \t No compress output file. Increase speed and size :-)");
            Console.WriteLine("  -cl \t Clean output dir after convert.");
            Console.WriteLine("  -v0 \t Suppress verbose.");
            Console.WriteLine("  -v1 \t Suppress verbose. Only output file name.");
            Worker.print_usage();
        }

        static void print_copyright()
        {
            Console.WriteLine("FB2mobi v 2.0.4 Copyright (c) 2008-2012 Rakunov Alexander 2012-01-07");
            Console.WriteLine("Project home: http://code.google.com/p/fb2mobi/\n");
        }

        [STAThread]
        static int Main(string[] args)
        {

            if(args.Length == 0){
                print_copyright();
                print_usage();
                return 1;
            }
            Arguments CommandLine = new Arguments(args);

            bool verbose = (CommandLine["v0"] == "" && CommandLine["v1"] == "");

            if (verbose)
                print_copyright();

            if (CommandLine["?"] == "true" || CommandLine["help"] == "true" || CommandLine["h"] == "true")
            {
                print_usage();
                return 0;
            }

            string filename = CommandLine[0];
            if (!File.Exists(filename))
            {
                Console.Error.WriteLine("File: \"" + filename + "\" not found\n");
                if (verbose)
                    print_usage();
                return 1;
            }


            // SET CURRENT DIR TO FB2MOBI EXECUTE DIR


            string PathToExecute = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            if (PathToExecute.Trim().Length > 0)
                Directory.SetCurrentDirectory(PathToExecute);

            if (!File.Exists(kindlegen))
            {
                Console.Error.WriteLine("File: \"" + kindlegen + "\" not found\n");
                return 1;
            }


            // PREPARE DATA


            Worker sp = new Worker(CommandLine);
            
            if (sp.error())
            {
                Console.Error.WriteLine("Init error.\n");
                if (verbose)
                    print_usage();
                return 1;
            }


            // GET SOURCE FILES FOR KINGLEGEN FROM FB2


            try
            {
                sp.saveImages();
                sp.transform(bodyxsl, "index.html");
                sp.transform(opfxsl, sp.getBookName(".opf"));
                sp.transform(ncxxsl, "book.ncx");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("error occured: " + e.Message);
                return 1;
            }


            // RUN KINDLEGEN


            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = kindlegen;

            string KindleGenArguments = CommandLine["nc"] == "true" ? " -c0" : " -c2";
            KindleGenArguments += " \"" + sp.getWorkDir() + sp.getBookName(".opf") + "\"";

            process.StartInfo.Arguments = KindleGenArguments;

            process.Start();

            string str;
            while ((str = process.StandardOutput.ReadLine()) != null)
                if (verbose && str.Length > 0)
                    Console.WriteLine(str);

            process.Close();

            // CLEAN AND PUBLISH
            if (verbose)
                Console.WriteLine("");

            string bookname = sp.getBookName(".mobi");
            if (File.Exists(sp.getWorkDir() + bookname))
            {
                File.Move(sp.getWorkDir() + bookname, sp.getOutputDir() + bookname);

                if (CommandLine["cl"] == "true")
                {
                    try
                    {
                        Directory.Delete(sp.getWorkDir(), true);
                    }
                    catch (Exception) { }
                }
                else if(verbose)
                    Console.WriteLine("Output: " + sp.getWorkDir());

                if(verbose)
                    Console.WriteLine("Book: " + sp.getOutputDir() + bookname);
                else if(CommandLine["v1"] == "true")
                    Console.WriteLine(sp.getOutputDir() + bookname);
                
                return 0;

            }
            else
            {
                if(verbose)
                    Console.WriteLine("The output file is missing.");
                try
                {
                    Directory.Delete(sp.getWorkDir(), true);
                }
                catch (Exception) { }
            }
            
            return 1;
        }
    }
}
