using System.Diagnostics;
using System.Threading.Channels;

namespace JobPool
{
    public enum JobState
    {
        Run,
        Read,
        Write,
        Close
    }

    public interface IJobState
    {
        JobState State { get; }
    }

    public abstract class JobPool<TValue>
        : IDisposable
        where TValue : class, IJobState
    {
        readonly List<Task> _tasks = [];
        readonly SemaphoreSlim _semaphoreRead = new SemaphoreSlim(1, 1);
        readonly SemaphoreSlim _semaphoreWrite = new SemaphoreSlim(1, 1);
        readonly CancellationTokenSource _cancellationTokenSource = new();
        readonly Channel<TValue> _channelOne = Channel.CreateUnbounded<TValue>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = false,
        });
        readonly Channel<TValue> _channelTwo = Channel.CreateUnbounded<TValue>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = false,
        });
        volatile bool _interrupted = true;
        private readonly Func<TValue, CancellationToken, Task<bool>> _doReadJob;
        private readonly Func<TValue, CancellationToken, Task<bool>> _doWriteJob;

        public JobPool(int count = -1)
            : this(null, null, count)
        {
        }
        public JobPool(Func<TValue, CancellationToken, Task<bool>>? doReadJob
            , Func<TValue, CancellationToken, Task<bool>>? doWriteJob
            , int count = -1) 
            
        {
            if(count != -1 && count < 1) throw new ArgumentOutOfRangeException($"Count - {count} - should be grater then or equals 1");

            if (count == -1)
                count = Environment.ProcessorCount / 2;

            _doReadJob = doReadJob ?? DoRead;
            _doWriteJob = doWriteJob ?? DoWrite;

            Debug.WriteLine($"Job queue start {count}");

            var contextOne = new ContextObject(_semaphoreRead, _channelOne.Writer, _channelTwo.Reader, _cancellationTokenSource.Token);
            var contextTwo = new ContextObject(_semaphoreWrite, _channelTwo.Writer, _channelOne.Reader, _cancellationTokenSource.Token);
            Enumerable.Range(0, count)
                .ForEach(_ =>
                {
                    _tasks.Add(Task.Factory.StartNew(context => ProcessJob((ContextObject)context!)
                        , contextOne));
                    _tasks.Add(Task.Factory.StartNew(context => ProcessJob((ContextObject)context!)
                        , contextTwo));
                });
        }
        record ContextObject(SemaphoreSlim Semaphore
            , ChannelWriter<TValue> Writer
            , ChannelReader<TValue> Reader
            , CancellationToken Token)
        {
        }
        protected abstract Task<bool> DoRead(TValue value, CancellationToken token);
        protected abstract Task<bool> DoWrite(TValue value, CancellationToken token);

        public void AddJob(TValue val)
        {
            //_queue.Enqueue(val);
            _channelTwo.Writer.TryWrite(val);
        }
        private async ValueTask ProcessJob(ContextObject state)
        {
            // Debug.WriteLine($"Task Thread: {Thread.CurrentThread.ManagedThreadId} Start");

            try
            {
                while (!state.Token.IsCancellationRequested)
                {
                    if (_interrupted) continue;

                    await state.Semaphore.WaitAsync(state.Token);
                    //Debug.WriteLine($"--- Process Thread: {Thread.CurrentThread.ManagedThreadId}");
                    var value = await state.Reader.ReadAsync(state.Token);
                    
                    state.Semaphore.Release();

                    await ProcessReadWrite(value, state.Writer, state.Token);
                }
            }
            catch (OperationCanceledException)
            {
                ;
            }
        }
        
        private async ValueTask ProcessReadWrite(TValue value, ChannelWriter<TValue> writer, CancellationToken token)
        {
            var result = true;

            if (value.State == JobState.Read)
            {
                result = await _doReadJob(value, token);
                //Debug.WriteLine($"--- Get value: {value}, Thread: {Thread.CurrentThread.ManagedThreadId}");
            }
            else if (value.State == JobState.Write)
            {
                result = await _doWriteJob(value, token);
                //Debug.WriteLine($"--- Set value: {value}, Thread: {Thread.CurrentThread.ManagedThreadId}");
            }

            if (result == false || value.State == JobState.Close)
                return;

            await writer.WriteAsync(value, token);
        }
        public void Join()
        {
            _interrupted = false;
            Task.WhenAll(_tasks).Wait();
        }
        public virtual void Dispose()
        {
            Close();
            _semaphoreRead.Dispose();
            _semaphoreWrite.Dispose();
            _cancellationTokenSource.Dispose();
        }
        public virtual void Close()
        {
            if(_cancellationTokenSource.IsCancellationRequested == false)
                _cancellationTokenSource.Cancel();
        }
    }
}
