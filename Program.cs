using System;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (Array.Exists(args, arg => string.Equals(arg, "--self-test", StringComparison.OrdinalIgnoreCase)))
        {
            try { return Game.SelfTest(); }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new Game());
        return 0;
    }
}

