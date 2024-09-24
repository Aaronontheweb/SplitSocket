using System.IO.Pipelines;
using System.Net;
using Microsoft.Extensions.Logging;

namespace SplitSocket;

public abstract class TransportBase : ITransportConnection
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
        // set connectionId to a hex string representation of the id
        ConnectionId = _id.ToString("X");
        Logger = logger;
        ConnectionClosedRequested = _connectionClosingCts.Token;
    }
    
    /// <summary>
    /// Gets or sets a unique identifier to represent this connection in trace logs.
    /// </summary>
    public string ConnectionId { get; }
    
    /// <summary>
    /// Gets or sets the <see cref="IDuplexPipe"/> that can be used to read or write data on this connection.
    /// </summary>
    public abstract IDuplexPipe Transport { get; set; }
   
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
    public void Abort()
    {
        Abort(new ConnectionAbortedException("The connection was aborted by the application via TransportBase.Abort()."));
    }

    public abstract void Abort(ConnectionAbortedException abortReason);
}