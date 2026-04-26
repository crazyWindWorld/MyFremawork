using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestTcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().GetAwaiter().GetResult();
        }

        static async Task Run()
        {
            var relay = new RelayServer(
                listenPort: 9000,
                targetHost: "127.0.0.1",
                targetPort: 9100);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Stopping...");
            };

            await relay.StartAsync(cts.Token);
        }
    }
}
