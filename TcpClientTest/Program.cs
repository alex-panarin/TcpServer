using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace TcpClientTest
{
    internal class Program
    {
        const int numberOfTests = 1000;

        static void Main(string[] args)
        {
            Stopwatch sw = new();
            sw.Start();
            List<Task> tasks =
                new List<Task>(Enumerable.Range(0, numberOfTests)
                    .Select(i =>
                    {
                        return Task.Factory.StartNew(async () =>
                            {
                                try
                                {
                                    await ProcessSimpeTestTask(i);
                                    //await  ProcessWritePoolTestTask(i);
                                    //await ProcessFileWriteTask(i);

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
       
        private static async Task ProcessSimpeTestTask(int index)
        {
            using TcpClient tcpClient = new TcpClient("127.0.0.1", 9999);

            await tcpClient.Client.SendAsync(Encoding.UTF8.GetBytes($" {index} Hello"));

            var bytes = new byte[1024];
            var number = tcpClient.Client.Receive(bytes);

            Console.WriteLine(Encoding.UTF8.GetString(bytes, 0, number));
        }

        private static async Task ProcessWritePoolTestTask(int index)
        {
            using TcpClient tcpClient = new TcpClient("127.0.0.1", 9999);

            var testData = Enumerable.Range(0, 10)
                .Select(i => Encoding.UTF8.GetBytes($"Test Data {(index + 1) + i}\r\n")); 
            foreach (var test in testData)
            {
                await tcpClient.Client.SendAsync(test);
            }

            var bytes = new byte[1024];
            var number = tcpClient.Client.Receive(bytes);

            Console.WriteLine(Encoding.UTF8.GetString(bytes, 0, number));
        }

        private static async Task ProcessFileWriteTask(int index)
        {
            const string path = "Path_To_Test_File";
            using TcpClient tcpClient = new TcpClient("127.0.0.1", 9999);
            using var stream = File.OpenRead(path);

            await stream.CopyToAsync(tcpClient.GetStream(), ushort.MaxValue);
        }
    }
}
