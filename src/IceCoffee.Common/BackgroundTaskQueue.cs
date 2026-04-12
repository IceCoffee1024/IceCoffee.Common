using System.Threading.Channels;

namespace IceCoffee.Common
{
    /// <summary>
    /// A bounded in-memory queue that buffers background work items before they are consumed.
    /// </summary>
    /// <typeparam name="T">The type of work item stored in the queue.</typeparam>
    public class BackgroundTaskQueue<T> : IBackgroundTaskQueue<T>
    {
        private readonly Channel<T> _channel;

        /// <summary>
        /// Creates a bounded queue with the specified capacity and the default wait-on-full behavior.
        /// </summary>
        /// <param name="capacity">Maximum number of items allowed in the queue buffer.</param>
        public BackgroundTaskQueue(int capacity)
        {
            BoundedChannelOptions options = new(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            };
            _channel = Channel.CreateBounded<T>(options);
        }

        /// <summary>
        /// Creates a bounded queue using the provided channel options.
        /// </summary>
        /// <param name="channelOptions">Channel configuration that controls queue capacity and concurrency behavior.</param>
        public BackgroundTaskQueue(BoundedChannelOptions channelOptions)
        {
            _channel = Channel.CreateBounded<T>(channelOptions);
        }

        /// <inheritdoc/>
        public ChannelReader<T> Reader => _channel.Reader;

        /// <inheritdoc/>
        public bool TryEnqueue(T workItem)
        {
            return _channel.Writer.TryWrite(workItem);
        }

        /// <inheritdoc/>
        public ValueTask EnqueueAsync(T workItem, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(workItem, cancellationToken);
        }

        /// <inheritdoc/>
        public ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public void Complete()
        {
            _channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// A background task queue specialized for asynchronous work delegates.
    /// </summary>
    public class BackgroundTaskQueue : BackgroundTaskQueue<Func<CancellationToken, ValueTask>>
    {
        /// <summary>
        /// Creates a bounded background task queue with the specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of work items allowed in the queue buffer.</param>
        public BackgroundTaskQueue(int capacity) : base(capacity)
        {
        }

        /// <summary>
        /// Creates a bounded background task queue using the provided channel options.
        /// </summary>
        /// <param name="channelOptions">Channel configuration that controls queue capacity and concurrency behavior.</param>
        public BackgroundTaskQueue(BoundedChannelOptions channelOptions) : base(channelOptions)
        {
        }
    }
}