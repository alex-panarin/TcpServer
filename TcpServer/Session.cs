﻿using JobPool;
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
            public void Clear() => Data.Clear();
        }
        public Session(Socket socket, IProcessorFactory<Session> factory)
        {
            _processor = factory.GetProcessor();
            Socket = socket;
            buffer = new(_processor.GetBufferSize());
            Id = Guid.NewGuid();
        }
        public Session(Socket socket, int bufferSize = byte.MaxValue)
        {
            Socket = socket;
            buffer = new(bufferSize);
            Id = Guid.NewGuid();
        }
        private Buffer buffer;
        private bool _disposed = false;
        protected Socket Socket;
        private IProcessor<Session>? _processor;

        public EndPoint? Address => Socket?.RemoteEndPoint;
        public Guid Id { get; }
        public JobState State { get; set; }
        public Task<bool> ReadAsync()
        {
            return Task.FromResult(Read());
        }
        public Task<bool> WriteAsync(string val)  => Task.FromResult(Write(val));
        public Task<bool> WriteAsync(byte[] bytes) => Task.FromResult(Write(bytes));
        public bool Read()
        {
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
        public bool Write(string val) => Write(Encoding.UTF8.GetBytes(val));
        public bool Write(byte[] bytes)
        {
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
        public IProcessor<Session> GetProcessor(IProcessorFactory<Session> processorFactory)
        {
            _processor ??= processorFactory.GetProcessor();
            return _processor;
        }
        ~Session () => Dispose(false);
    }
}
