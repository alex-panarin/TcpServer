﻿using System.Diagnostics;
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
        private readonly Func<TValue, Task<bool>> _doReadJob;
        private readonly Func<TValue, Task<bool>> _doWriteJob;

        public JobPool(int count = -1)
            : this(null, null, count)
        {
        }
        public JobPool(Func<TValue, Task<bool>>? doReadJob
            , Func<TValue, Task<bool>>? doWriteJob
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
                    _tasks.Add(ProcessPool(_semaphoreRead, _channelOne.Writer, _channelTwo.Reader));
                    _tasks.Add(ProcessJob(_semaphoreWrite, _channelOne.Reader, _channelTwo.Writer));
                });
        }

        protected abstract Task<bool> DoRead(TValue value);
        protected abstract Task<bool> DoWrite(TValue value);

        public void AddJob(TValue val)
        {
            //_queue.Enqueue(val);
            _channelTwo.Writer.TryWrite(val);
        }
        private async Task ProcessPool(SemaphoreSlim @event, ChannelWriter<TValue> writer, ChannelReader<TValue> reader)
        {
            await Task.Yield();
            //Debug.WriteLine($"Read Thread: {Environment.CurrentManagedThreadId} Start");

            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    if (_interrupted) continue;

                    await @event.WaitAsync(_cancellationTokenSource.Token);

                    var val = await reader.ReadAsync(_cancellationTokenSource.Token);
                    
                    @event.Release();

                    await ProcessReadWrite(val, writer);
                }
            }
            catch (OperationCanceledException)
            {
                ;
            }
        }
        private async Task ProcessJob(SemaphoreSlim @event, ChannelReader<TValue> reader, ChannelWriter<TValue> writer)
        {
            await Task.Yield();
            //Debug.WriteLine($"Write Thread: {Environment.CurrentManagedThreadId} Start");
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    if (_interrupted) continue;
                    //Debug.WriteLine($"Write Thread: {Environment.CurrentManagedThreadId} Running ...");
                    await @event.WaitAsync(_cancellationTokenSource.Token);

                    var val = await reader.ReadAsync(_cancellationTokenSource.Token);
                    
                    @event.Release();

                    await ProcessReadWrite(val, writer);
                }
            }
            catch (OperationCanceledException)
            {
                ;
            }
        }

        private async Task ProcessReadWrite(TValue val, ChannelWriter<TValue> writer)
        {
            var result = true;

            if (val.State == JobState.Read)
            {
                result = await _doReadJob(val);
                //Debug.WriteLine($"Read Thread: {Environment.CurrentManagedThreadId}, Get value: {val}");
            }
            else if (val.State == JobState.Write)
            {
                result = await _doWriteJob(val);
                //Debug.WriteLine($"Write Thread: {Environment.CurrentManagedThreadId}, Set value: {val}");
            }
            
            if (result == false || val.State == JobState.Close)
                return;

            await writer.WriteAsync(val, _cancellationTokenSource.Token);
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
