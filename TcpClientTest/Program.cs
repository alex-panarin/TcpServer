using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace TcpClientTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const int numberOfTask = 100;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<Task> tasks = 
                new List<Task>(Enumerable.Range(0, numberOfTask)
                    .Select(i =>
                    {
                        return Task.Factory.StartNew(async () =>
                         {
                             try
                             {
                                 //await ProcessSimpeTestTask(i);
                                 await ProcessFileWriteTask(i);
                                 //await ProcessInterSession(i);
                             }
                             catch (Exception ex)
                             {
                                 Console.WriteLine($"Error === {ex.Message}");
                             }
                         });
                    })
                );

            Task.WhenAll(tasks).Wait();
            sw.Stop();
            Console.WriteLine($"Result: {sw.ElapsedMilliseconds} ms");

            Console.ReadKey();
        }

        private static async Task ProcessInterSession(int index)
        {
            using TcpClient tcpClient = new TcpClient("127.0.0.1", 9999);

            await tcpClient.Client.SendAsync(Encoding.UTF8.GetBytes($" {index} Hello"));
        }

        private static async Task ProcessSimpeTestTask(int index)
        {
            using TcpClient tcpClient = new TcpClient("127.0.0.1", 9999);

            await tcpClient.Client.SendAsync(Encoding.UTF8.GetBytes($" {index} Hello"));

            var bytes = new byte[1024];
            var number = tcpClient.Client.Receive(bytes);

            Console.WriteLine(Encoding.UTF8.GetString(bytes, 0, number));
        }

        private static async Task ProcessFileWriteTask(int index)
        {
            using TcpClient tcpClient = new TcpClient("127.0.0.1", 9999);
            using var stream = File.OpenRead("Path_To_Test_File");

            await stream.CopyToAsync(tcpClient.GetStream(), ushort.MaxValue);
        }
    }
}
