using JobPool;
using System.Net.Sockets;

namespace TcpServer
{
    public class SessionPool
        : JobPool<Session>
    {
        private readonly IProcessorFactory<Session> _processorFactory;
        public SessionPool(IProcessorFactory<Session> processorFactory)
            : base()
        {
            _processorFactory = processorFactory ?? throw new ArgumentNullException(nameof(processorFactory));
        }
        protected override async Task<bool> DoRead(Session session, CancellationToken token)
        {
            return await session.GetProcessor().ProcessRead(session, token); 
        }
        protected override async Task<bool> DoWrite(Session session, CancellationToken token)
        {
            return await session.GetProcessor().ProcessWrite(session, token); 
        }

        protected virtual Session GetSession(Socket socket) 
            => new Session(socket, _processorFactory, ushort.MaxValue) { State = JobState.Read};
    }
}

