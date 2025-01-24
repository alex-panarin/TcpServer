using JobPool;

namespace TcpServer
{
    public class EchoSessionProcessor
        : IProcessor<Session>
    {
        public int GetBufferSize()
        {
            return ushort.MaxValue;
        }

        public async Task<bool> ProcessRead(Session session)
        {
            if (await session.ReadAsync())
            {
                if (session.State == JobState.Close)
                {
                    Console.WriteLine($"=== Thread: {Environment.CurrentManagedThreadId} => Remove connection {session.Id} ===");
                    session.Dispose();
                    session.State = JobState.Close;
                }
                else
                {
                    session.State = JobState.Write; // Need Answer
                }
            }
            
            return session.State != JobState.Close;
        }

        public async Task<bool> ProcessWrite(Session session)
        {
            var value = session.GetLastValue();
            session.State = JobState.Read;
            Console.WriteLine($"=== Thread: {Environment.CurrentManagedThreadId} => Write Echo: {value} ===");
            await session.WriteAsync($"Echo: {value}");
            
            return true;
        }
    }
}
