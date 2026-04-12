using System.Threading.Channels;

namespace IceCoffee.Common
{
    /// <summary>
    /// Defines the contract for a bounded background task queue.
    /// </summary>
    /// <typeparam name="T">The type of work item stored in the queue.</typeparam>
    public interface IBackgroundTaskQueue<T>
    {
        /// <summary>
        /// Gets the channel reader used by the background consumer.
        /// </summary>
        ChannelReader<T> Reader { get; }

        /// <summary>
        /// Attempts to enqueue a work item without waiting.
        /// </summary>
        /// <param name="workItem">The work item to enqueue.</param>
        /// <returns>True when the item was accepted; otherwise false.</returns>
        bool TryEnqueue(T workItem);

        /// <summary>
        /// Enqueues a work item asynchronously, waiting until capacity becomes available if needed.
        /// </summary>
        /// <param name="workItem">The work item to enqueue.</param>
        /// <param name="cancellationToken">Token used to cancel the enqueue operation.</param>
        /// <returns>A task that completes when the item has been accepted.</returns>
        ValueTask EnqueueAsync(T workItem, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the next available work item from the queue.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the read operation.</param>
        /// <returns>A task that completes with the next queued item.</returns>
        ValueTask<T> DequeueAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Signals that no more items will be accepted into the queue.
        /// </summary>
        void Complete();
    }

    /// <summary>
    /// Defines the contract for a background queue that stores asynchronous work delegates.
    /// </summary>
    public interface IBackgroundTaskQueue : IBackgroundTaskQueue<Func<CancellationToken, ValueTask>>
    {
    }
}