
using EasySave.Application;

namespace EasySave.AppCommon
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string hostType = "gui";

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
