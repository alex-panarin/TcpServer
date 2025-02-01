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
            if(count != -1 && count < 2) throw new ArgumentOutOfRangeException($"Count - {count} - should be grater then 1");

            if (count == -1)
                count = Environment.ProcessorCount / 2;

            _doReadJob = doReadJob ?? DoRead;
            _doWriteJob = doWriteJob ?? DoWrite;

            Debug.WriteLine($"Job queue start {count}");

            Enumerable.Range(0, count)
                .ForEach(_ =>
                {
                    // _semaphoreRead, _channelOne.Writer, _channelTwo.Reader
                    _tasks.Add(Task.Factory.StartNew(state => ProcessJob((StateObject)state!)
                        , new StateObject(_semaphoreRead, _channelOne.Writer, _channelTwo.Reader, _cancellationTokenSource.Token)
                        , _cancellationTokenSource.Token));
                    _tasks.Add(Task.Factory.StartNew(state => ProcessJob((StateObject)state!)
                        , new StateObject(_semaphoreWrite, _channelTwo.Writer, _channelOne.Reader, _cancellationTokenSource.Token)
                        , _cancellationTokenSource.Token));
                });
        }
        record StateObject(SemaphoreSlim Semaphore
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
        private async Task ProcessJob(StateObject state)
        {
            await Task.Yield();
            //Debug.WriteLine($"Read Thread: {Environment.CurrentManagedThreadId} Start");

            try
            {
                while (!state.Token.IsCancellationRequested)
                {
                    if (_interrupted) continue;

                    await state.Semaphore.WaitAsync(state.Token);

                    var val = await state.Reader.ReadAsync(state.Token);

                    state.Semaphore.Release();

                    await ProcessReadWrite(val, state.Writer, state.Token);
                }
            }
            catch (OperationCanceledException)
            {
                ;
            }
        }
        
        private async Task ProcessReadWrite(TValue val, ChannelWriter<TValue> writer, CancellationToken token)
        {
            var result = true;

            if (val.State == JobState.Read)
            {
                result = await _doReadJob(val, token);
                //Debug.WriteLine($"Read Thread: {Environment.CurrentManagedThreadId}, Get value: {val}");
            }
            else if (val.State == JobState.Write)
            {
                result = await _doWriteJob(val, token);
                //Debug.WriteLine($"Write Thread: {Environment.CurrentManagedThreadId}, Set value: {val}");
            }
            
            if (result == false || val.State == JobState.Close)
                return;

            await writer.WriteAsync(val, token);
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
