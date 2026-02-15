using System.Runtime.InteropServices;
using EasySave.Application;

namespace EasySave.AppCommon
{
    /// <summary>
    /// Main entry point that selects the GUI or console host.
    /// </summary>
    public class Program
    {
        // Windows P/Invoke used to attach/allocate a console in console mode
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        private const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        /// Starts the application in console or GUI mode based on arguments and environment.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <exception cref="ArgumentException">Thrown when the requested host type is unknown.</exception>
        [STAThread]
        public static void Main(string[] args)
        {
            // If arguments are provided -> console mode, otherwise -> GUI
            string hostType = args.Length > 0
                ? "console"
                : Environment.GetEnvironmentVariable("EASYSAVE_HOST") ?? "gui";

            // On Windows (WinExe), attach to parent console in console mode
            if (hostType == "console" && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!AttachConsole(ATTACH_PARENT_PROCESS))
                    AllocConsole();
            }

            // Host selection
            IApplicationHost host = hostType.ToLower() switch
            {
                "gui" => new GUI.Host(),
                "console" => new UI.Host(),
                _ => throw new ArgumentException($"Unknown host type: {hostType}")
            };

            // Run
            new ApplicationManager(args).RunHost(host);
        }
    }
}
