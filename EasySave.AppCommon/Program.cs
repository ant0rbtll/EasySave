using System.Runtime.InteropServices;
using EasySave.Application;

namespace EasySave.AppCommon
{
    public class Program
    {
        // Windows P/Invoke pour attacher/allouer une console en mode console
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        private const int ATTACH_PARENT_PROCESS = -1;

        [STAThread]
        public static void Main(string[] args)
        {
            // Si des arguments sont passés → mode console, sinon → GUI
            string hostType = args.Length > 0
                ? "console"
                : Environment.GetEnvironmentVariable("EASYSAVE_HOST") ?? "gui";

            // Sur Windows (WinExe), rattacher la console du parent si mode console
            if (hostType == "console" && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!AttachConsole(ATTACH_PARENT_PROCESS))
                    AllocConsole();
            }

            // Sélection du host
            IApplicationHost host = hostType.ToLower() switch
            {
                "gui" => new GUI.Host(),
                "console" => new UI.Host(),
                _ => throw new ArgumentException($"Unknown host type: {hostType}")
            };

            // Lancement
            new ApplicationManager(args).RunHost(host);
        }
    }
}
