﻿namespace TcpServer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var server = new TcpServer(new SessionProcessorFactory<SessionProcessor>());

            var task = Task.Run(() =>
            {
                Console.WriteLine("Type \"stop\" to interrupt the job :)");
                while (true)
                {
                    var input = Console.ReadLine();
                    if (input == "stop")
                    {
                        server.Stop();
                        Console.WriteLine("Server stopped...");
                        break;
                    }
                }
            });

            await server.Start();
            await task;
        }
    }
}