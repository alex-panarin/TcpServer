﻿using JobPool;

namespace TcpServer
{
    internal class FileSessionProcessor
        : IProcessor<Session>
    {
        private readonly MemoryStream _stream;
        private readonly string _fileName = $"{Guid.NewGuid()}.txt";
        public FileSessionProcessor()
        {
            _stream = new MemoryStream(ushort.MaxValue * 16);
        }

        public int GetBufferSize() => ushort.MaxValue;
        
        public async Task<bool> ProcessRead(Session session)
        {
            if (await session.ReadAsync())
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

        public Task<bool> ProcessWrite(Session session)
        {
            session.State = JobState.Read;
            // Nothing to answer but keep session open.
            return Task.FromResult(true);
        }
    }
}