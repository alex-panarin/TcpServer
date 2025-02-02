using System.Diagnostics;
using System.Net.Sockets;

namespace TcpServer
{
    public class TcpServer
        : SessionPool
    {
        readonly TcpListener _listener;
        public TcpServer(IProcessorFactory<Session> processorFactory, int port = 9999)
            : base(processorFactory)
        {
            _listener = TcpListener.Create(port);
        }
        public Task Start()
        {
            var task = Task.Factory.StartNew((state) =>
            {
                var listener = (TcpListener)state!;
                listener.Start();
                Console.WriteLine("Server running ...");
                try
                {
                    while (true)
                    {
                        var session = GetSession(listener.AcceptSocket());
                        AddJob(session);

                        //Debug.WriteLine($"=== Thread: {Thread.CurrentThread.ManagedThreadId} => Add new connection {session.Id} ===");
                    }
                }
                catch (SocketException x)
                {
                    if(x.SocketErrorCode == SocketError.OperationAborted
                        || x.SocketErrorCode == SocketError.Interrupted)
                    {
                        Console.WriteLine("Operation terminated by user");
                    }
                }
            }
            , _listener);

            Join();
            return task;
        }
        public void Stop()
        {
            Close();
            _listener.Stop();
        }
        public override void Dispose()
        {
            Stop();
            _listener.Dispose();
            base.Dispose();
        }
    }        
}

