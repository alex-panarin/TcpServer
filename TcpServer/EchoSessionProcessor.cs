using JobPool;
using System.Diagnostics;

namespace TcpServer
{
    public class EchoSessionProcessor
        : IProcessor<Session>
    {
        public async Task<bool> ProcessRead(Session session, CancellationToken token)
        {
            if (await session.ReadAsync(token))
            {
                if (session.State == JobState.Close)
                {
                    //Debug.WriteLine($"=== Thread: {Thread.CurrentThread.ManagedThreadId} => Remove connection {session.Id} ===");
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

        public async Task<bool> ProcessWrite(Session session, CancellationToken token)
        {
            var value = session.GetLastValue();
            session.State = JobState.Read;
            Console.WriteLine($"=== Thread: {Thread.CurrentThread.ManagedThreadId} => Write Echo: {value} ===");
            await session.WriteAsync($"Echo: {value}", token);
            
            return true;
        }
    }
}
