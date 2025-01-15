using JobPool;

namespace TcpServer
{
    public class SessionProcessor
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
                    //Console.WriteLine($"=== Thread: {Environment.CurrentManagedThreadId} => Remove connection {session.Id} ===");
                    session.Dispose();
                    return false;
                }
                session.State = JobState.Write; // Need Answer
            }
            
            return session.State != JobState.Close;
        }

        public async Task<bool> ProcessWrite(Session session)
        {
            var value = session.GetLastValue();
            session.State = JobState.Read;
            //Console.WriteLine($"=== Thread: {Environment.CurrentManagedThreadId} => Write: {value} ===");
            await session.WriteAsync($"Echo: {value}");
            
            return session.State == JobState.Read;
        }
    }
}
