using JobPool;

namespace TcpServer
{
    public class FileSessionProcessor
        : IProcessor<Session>
    {
        private readonly MemoryStream _stream;
        private readonly string _fileName = $"{Guid.NewGuid()}.txt";
        public FileSessionProcessor()
        {
            _stream = new MemoryStream(ushort.MaxValue * 16);
        }

        public async Task<bool> ProcessRead(Session session, CancellationToken token)
        {
            if (await session.ReadAsync(token))
            {
                await _stream.WriteAsync(session.GetLastData());

                if (session.State == JobState.Close)
                {
                    //Console.WriteLine($"=== Thread: {Environment.CurrentManagedThreadId} => Remove connection {session.Id} ===");
                    // end of read
                    _stream.Position = 0;
                    using var fileStream = File.Create(_fileName);
                    await _stream.CopyToAsync(fileStream);
                    _stream.Dispose();
                    session.Dispose();
                }
                else
                {
                    //Console.WriteLine($"=== Thread: {Environment.CurrentManagedThreadId} => Read connection {session.Id} ===");
                    ;
                }
            }
            return session.State != JobState.Close; 
        }

        public Task<bool> ProcessWrite(Session session, CancellationToken token)
        {
            session.State = JobState.Read;
            // Nothing to answer but keep session open.
            return Task.FromResult(true);
        }
    }
}
