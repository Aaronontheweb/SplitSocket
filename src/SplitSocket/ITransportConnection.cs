using System.Net;

namespace SplitSocket;

public interface ITransportConnection : IAsyncDisposable
{
    string ConnectionId { get; }
    
    /// <summary>
    /// Triggered when the client connection is closed.
    /// </summary>
    public CancellationToken ConnectionClosed { get; }

    /// <summary>
    /// Gets or sets the local endpoint for this connection.
    /// </summary>
    public EndPoint? LocalEndPoint { get; }

    /// <summary>
    /// Gets or sets the remote endpoint for this connection.
    /// </summary>
    public EndPoint? RemoteEndPoint { get; }

    /// <summary>
    /// Aborts the underlying connection.
    /// </summary>
    public void Abort();

    /// <summary>
    /// Aborts the underlying connection.
    /// </summary>
    /// <param name="abortReason">A <see cref="ConnectionAbortedException"/> describing the reason the connection is being terminated.</param>
    public void Abort(ConnectionAbortedException abortReason);
}