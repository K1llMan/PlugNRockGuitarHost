using PlugNRockGuitarHost.Server;

namespace PlugNRockGuitarHost
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            CommandServer server = new(new() {
                Address = "http://localhost:5000"
            });

            server.Run();
        }
    }
}
