using JobPool;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace TcpServer
{
    public class TcpSessionClient
        : JobPool<Session>
    {
        private readonly ConcurrentQueue<string> _queue = [];
        private readonly Action<Memory<byte>>? _readCallback;
        private readonly Socket _socket;
        private readonly Session _session;

        public TcpSessionClient(Action<Memory<byte>>? readCallback)
            : base(4)
        {
            _readCallback = readCallback;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _session = new Session(_socket) { State = JobState.Read };
        }

        public async Task ConnectAsync(string host, int port)
        {
            await _socket.ConnectAsync(IPAddress.Parse(host), port);
            AddJob(_session);
        }
        protected override async Task<bool> DoRead(Session session)
        {
            if (await session.ReadAsync())
            {
                if (session.State == JobState.Close)
                {
                    session.Dispose();
                }
                else
                {
                    _readCallback?.Invoke(session.GetLastData());
                    session.State = JobState.Write;
                }
            }
            else if (_queue.TryDequeue(out var value))
            {
                await session.WriteAsync(value);
                session.State = JobState.Write;
            }

            return session.State != JobState.Close;
        }

        protected override async Task <bool> DoWrite(Session session)
        {
            if (session.State == JobState.Close)
            {
                session.Dispose();
            }
            else if (_queue.TryDequeue(out var value))
            {
                await session.WriteAsync(value);
                session.State = JobState.Read;
            }
            
            return await Task.FromResult(session.State != JobState.Close);
        }

        public void Write(string value)
        {
            _queue.Enqueue(value);
        }

        public override void Dispose()
        {
            _session.Dispose();
            base.Dispose();
        }
    }
}
