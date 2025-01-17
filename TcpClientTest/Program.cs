using JobPool;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using TcpServer;

namespace TcpClientTest
{
    internal class Program
    {
        const int numberOfTest = 100;
        
        static void Main(string[] args)
        {
            
            bool runTask = false;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            if (runTask == false)
            {
                using var client = new TcpSessionClient(OnReadData);
                var testData = Enumerable.Range(0, numberOfTest)
                                       .Select(i => $"Test Data {i}");

                client.ConnectAsync("127.0.0.1", 9999).ConfigureAwait(false);

                testData.ForEach(data => client.Write(data));

                client.Join();
                ;
            }

            if (runTask)
            {
                List<Task> tasks =
                    new List<Task>(Enumerable.Range(0, numberOfTest)
                        .Select(i =>
                        {
                            return Task.Factory.StartNew(async () =>
                             {
                                 try
                                 {
                                     await ProcessSimpeTestTask(i);
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
            }
            sw.Stop();
            Console.WriteLine($"Result: {sw.ElapsedMilliseconds} ms");

            Console.ReadKey();
        }
        static void OnReadData(Memory<byte> data)
        {
            Console.WriteLine(Encoding.UTF8.GetString(data.ToArray()));
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
