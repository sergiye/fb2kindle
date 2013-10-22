using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace fb2mobi
{
    //based on https://code.google.com/p/fb2mobi/source/checkout

    internal class FB2mobiMain
    {
        private const string kindlegen = "kindlegen.exe";
        private const string opfxsl = "opf.xsl";
        private const string bodyxsl = "xhtml.xsl";
        private const string ncxxsl = "ncx.xsl";

        private static void print_usage()
        {
            Console.WriteLine("Usage: fb2mobi <file.fb2> [<output.mobi>] [{-,/,--}param]");
            Console.WriteLine("  -c \t Ñompress output file. Decrease size and speed :-)");
            Console.WriteLine("  -v0 \t Suppress verbose.");
            Console.WriteLine("  -v1 \t Suppress verbose. Only output file name.");
            Worker.print_usage();
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        public static T GetAttribute<T>(ICustomAttributeProvider assembly, bool inherit = false) where T : Attribute
        {
            var attr = assembly.GetCustomAttributes(typeof(T), inherit);
            foreach (var o in attr)
                if (o is T)
                    return o as T;
            return null;
        }

        private static void print_copyright()
        {
            var asm = Assembly.GetExecutingAssembly();
            var ver = asm.GetName().Version;
            Console.Write(asm.GetName().Name + " v " + ver.ToString(3) + "  ");
//            var title = GetAttribute<AssemblyTitleAttribute>(asm);
//            if (title != null)
//                Console.Write(title.Title);
            var desc = GetAttribute<AssemblyDescriptionAttribute>(asm);
            if (desc != null)
                Console.Write(desc.Description);
            Console.WriteLine();
        }

        [STAThread]
        private static int Main(string[] args)
        {
            print_copyright();
            if (args.Length == 0)
            {
                print_usage();
                return 1;
            }
            var CommandLine = new Arguments(args);
            var verbose = (CommandLine["v0"] == "" && CommandLine["v1"] == "");
            if (CommandLine["?"] == "true" || CommandLine["help"] == "true" || CommandLine["h"] == "true")
            {
                print_usage();
                return 0;
            }

            var filename = CommandLine[0];
            if (!File.Exists(filename))
            {
                Console.Error.WriteLine("File: \"" + filename + "\" not found\n");
                if (verbose)
                    print_usage();
                return 1;
            }

            // SET CURRENT DIR TO FB2MOBI EXECUTE DIR
            var PathToExecute = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            if (PathToExecute.Trim().Length > 0)
                Directory.SetCurrentDirectory(PathToExecute);

            if (!File.Exists(kindlegen))
            {
                Console.Error.WriteLine("File: \"" + kindlegen + "\" not found\n");
                return 1;
            }

            // PREPARE DATA
            var sp = new Worker(CommandLine);

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
            var process = new Process {StartInfo = {UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, FileName = kindlegen}};
            var KindleGenArguments = CommandLine["c"] != "true" ? " -c0" : " -c2";
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

            var bookname = sp.getBookName(".mobi");
            if (File.Exists(sp.getWorkDir() + bookname))
            {
                File.Move(sp.getWorkDir() + bookname, sp.getOutputDir() + bookname);
                try
                {
                    Directory.Delete(sp.getWorkDir(), true);
                }
                catch (Exception)
                {
                }

                if (verbose)
                    Console.WriteLine("Book: " + sp.getOutputDir() + bookname);
                else if (CommandLine["v1"] == "true")
                    Console.WriteLine(sp.getOutputDir() + bookname);
                return 0;
            }
            else
            {
                if (verbose)
                    Console.WriteLine("The output file is missing.");
                try
                {
                    Directory.Delete(sp.getWorkDir(), true);
                }
                catch (Exception)
                {
                }
            }
            return 1;
        }
    }
}