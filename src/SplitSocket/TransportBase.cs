using System.Net;
using Microsoft.Extensions.Logging;

namespace SplitSocket;

public abstract class TransportBase : ITransportConnection, IConnectionCompleteFeature
{
    protected readonly ILogger Logger;
    private bool _completed;
    private Stack<KeyValuePair<Func<object, Task>, object>>? _onCompleted;
    private readonly CancellationTokenSource _connectionClosingCts = new CancellationTokenSource();
    private readonly TaskCompletionSource _completionTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    protected readonly long _id;

    public TransportBase(long id, ILogger logger)
    {
        _id = id;
        Logger = logger;
        ConnectionClosedRequested = _connectionClosingCts.Token;
    }
    
    /// <summary>
    /// Gets or sets a unique identifier to represent this connection in trace logs.
    /// </summary>
    public abstract string ConnectionId { get; set; }
   
    public CancellationToken ConnectionClosedRequested { get; protected set; }
    public Task ExecutionTask => _completionTcs.Task;
    
    public void RequestClose()
    {
        try
        {
            _connectionClosingCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // There's a race where the token could be disposed
            // swallow the exception and no-op
        }
    }

    public void Complete()
    {
        _completionTcs.TrySetResult();

        _connectionClosingCts.Dispose();
    }

    public virtual ValueTask DisposeAsync()
    {
        return default;
    }

    public virtual CancellationToken ConnectionClosed { get; protected set; }
    public abstract EndPoint? LocalEndPoint { get; }
    public abstract EndPoint? RemoteEndPoint { get; }
    public abstract void Abort();

    public abstract void Abort(ConnectionAbortedException abortReason);
    
    void IConnectionCompleteFeature.OnCompleted(Func<object, Task> callback, object state)
    {
        if (_completed)
        {
            throw new InvalidOperationException("The connection is already complete.");
        }

        if (_onCompleted == null)
        {
            _onCompleted = new Stack<KeyValuePair<Func<object, Task>, object>>();
        }
        _onCompleted.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
    }
    
    public Task FireOnCompletedAsync()
    {
        if (_completed)
        {
            throw new InvalidOperationException("The connection is already complete.");
        }

        _completed = true;
        var onCompleted = _onCompleted;

        if (onCompleted == null || onCompleted.Count == 0)
        {
            return Task.CompletedTask;
        }

        return CompleteAsyncMayAwait(onCompleted);
    }

    private Task CompleteAsyncMayAwait(Stack<KeyValuePair<Func<object, Task>, object>> onCompleted)
    {
        while (onCompleted.TryPop(out var entry))
        {
            try
            {
                var task = entry.Key.Invoke(entry.Value);
                if (!task.IsCompletedSuccessfully)
                {
                    return CompleteAsyncAwaited(task, onCompleted);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
            }
        }

        return Task.CompletedTask;
    }

    private async Task CompleteAsyncAwaited(Task currentTask, Stack<KeyValuePair<Func<object, Task>, object>> onCompleted)
    {
        try
        {
            await currentTask;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
        }

        while (onCompleted.TryPop(out var entry))
        {
            try
            {
                await entry.Key.Invoke(entry.Value);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
            }
        }
    }

}