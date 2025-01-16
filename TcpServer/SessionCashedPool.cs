using System.Collections.Concurrent;
using System.Net.Sockets;

namespace TcpServer
{
    public class SessionCashedPool
        : SessionPool
    {
        private readonly ConcurrentDictionary<Guid, Session> _sessions = [];
        public SessionCashedPool(IProcessorFactory<Session> processorFactory)
            : base(processorFactory)
        {
            
        }

        protected override Session GetSession(Socket socket)
        {
            var session = base.GetSession(socket);
            return _sessions.GetOrAdd(session.Id, session);
        }

        protected Session? GetSession(Guid id)
        {
            if(_sessions.TryGetValue(id, out var session)) 
                return session;

            return null;
        }
    
    }
}
