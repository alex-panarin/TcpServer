namespace TcpServer
{
    public interface IProcessorFactory<TArg>
    {
        IProcessor<TArg> GetProcessor();
    }

    public interface IProcessor<TArg>
    {
        Task<bool> ProcessRead(TArg arg, CancellationToken token);
        Task<bool> ProcessWrite(TArg arg, CancellationToken token);
    }
}
