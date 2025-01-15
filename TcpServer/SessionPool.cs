using JobPool;

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
        protected override async Task<bool> DoRead(Session session)
        {
            return await session.GetProcessor(_processorFactory).ProcessRead(session); 
        }
        protected override async Task<bool> DoWrite(Session session)
        {
            return await session.GetProcessor(_processorFactory).ProcessWrite(session); 
        }
    }
}

