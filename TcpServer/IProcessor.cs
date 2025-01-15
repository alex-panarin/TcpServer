namespace TcpServer
{
    public interface IProcessorFactory<TArg>
    {
        IProcessor<TArg> GetProcessor();
    }

    public interface IProcessor<TArg>
    {
        int GetBufferSize();
        Task<bool> ProcessRead(TArg arg);
        Task<bool> ProcessWrite(TArg arg);
    }
}
