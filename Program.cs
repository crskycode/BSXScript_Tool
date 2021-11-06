using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BSXScript_Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args.Length != 2)
            {
                Console.WriteLine("BSXScript Tool");
                Console.WriteLine("Usage:");
                Console.WriteLine("  Export text : BSXScript_Tool -e [bsxx.dat]");
                Console.WriteLine("  Import text : BSXScript_Tool -b [bsxx.dat]");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            var mode = args[0];
            var path = Path.GetFullPath(args[1]);

            try
            {
                switch (mode)
                {
                    case "-e":
                    {
                        var script = new BSXScript();
                        script.Load(path);
                        script.ExportText(path + ".txt");
                        break;
                    }
                    case "-b":
                    {
                        var script = new BSXScript();
                        script.Load(path);
                        script.ImportText(path + ".txt");
                        script.Save(Path.ChangeExtension(path, ".new.dat"));
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
