namespace TcpServer
{
    public class SessionProcessorFactory<TProcessor>
        : IProcessorFactory<Session>
        where TProcessor : IProcessor<Session>, new()
    {
        public IProcessor<Session> GetProcessor()
        {
            return new TProcessor();
        }
    }
}
