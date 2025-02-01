using JobPool;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpServer
{
    public class Session
        : IJobState
        , IDisposable
    {
        class Buffer
        {
            public Buffer(int size)
            {
                Size = -1;
                Data = new byte[size];
            }
            public int Size;
            internal byte[] Data;
            public bool HasClosed => Size == 0;
            public bool IsNotEmpty => Size > 0;
            public string GetString() => IsNotEmpty ? Encoding.UTF8.GetString(Data, 0, Size) : string.Empty;
            public Memory<byte> GetData() => new Memory<byte>(Data, 0, Size);
            public void Clear() => Array.Clear(Data);//Data.Clear();
        }
        public Session(Socket socket, IProcessorFactory<Session> factory, int bufferSize)
        {
            _processor = factory.GetProcessor();
            Socket = socket;
            buffer = new(bufferSize);
            Id = Guid.NewGuid();
        }
        
        private Buffer buffer;
        private bool _disposed = false;
        protected Socket Socket;
        private readonly IProcessor<Session> _processor;

        public EndPoint? Address => Socket?.RemoteEndPoint;
        public Guid Id { get; }
        public JobState State { get; set; }
        public async Task<bool> ReadAsync(CancellationToken token)
        {
            await Task.Yield();

            buffer.Size = -1;
            lock (Socket)
            {
                try
                {
                    Socket.Blocking = false;
                    buffer.Size = Socket.Receive(buffer.Data, SocketFlags.None);
                }
                catch (SocketException x)
                {
                    if (x.SocketErrorCode != SocketError.WouldBlock)
                        throw;

                }
                finally
                {
                    Socket.Blocking = true;
                }

                if (buffer.Size == 0)
                    State = JobState.Close;

                return buffer.Size != -1;
            }
        }
        public async Task<bool> WriteAsync(string val, CancellationToken token)  => await WriteAsync(Encoding.UTF8.GetBytes(val), token);
        public async Task<bool> WriteAsync(byte[] bytes, CancellationToken token)
        {
            await Task.Yield();

            lock (Socket)
            {
                try
                {
                    buffer.Clear();
                    System.Buffer.BlockCopy(bytes, 0, buffer.Data, 0, bytes.Length);
                    Socket.Send(buffer.Data, bytes.Length, SocketFlags.None);
                }
                catch (SocketException)
                {
                    throw;
                }
                finally
                {
                }
                return true;
            }
        }
        public override string ToString() => $"Session: {Id}";
        public string GetLastValue() => buffer.GetString();
        public Memory<byte> GetLastData() => buffer.GetData();
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == false)
            {
                _disposed = true;

                if (Socket?.Connected == true)
                    Socket?.Close();
            }

            if(disposing)
                Socket?.Dispose();
        }
        public IProcessor<Session> GetProcessor()
        {
            return _processor;
        }
        ~Session () => Dispose(false);
    }
}
