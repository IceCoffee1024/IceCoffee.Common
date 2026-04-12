using System.Threading.Channels;

namespace IceCoffee.Common
{
    /// <summary>
    /// Provides a reusable single-consumer batching loop for Channel-backed queues.
    /// </summary>
    /// <typeparam name="T">The type of item read from the channel.</typeparam>
    public sealed class BatchingChannelConsumer<T>
    {
        private readonly ChannelReader<T> _reader;
        private readonly int _batchSize;

        /// <summary>
        /// Creates a batching consumer for the specified channel reader.
        /// </summary>
        /// <param name="reader">The channel reader that supplies items to the consumer.</param>
        /// <param name="batchSize">The maximum number of items included in each batch.</param>
        public BatchingChannelConsumer(ChannelReader<T> reader, int batchSize)
        {
            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be greater than zero.");
            }

            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _batchSize = batchSize;
        }

        /// <summary>
        /// Continuously reads items from the channel and dispatches them in batches until the channel completes.
        /// </summary>
        /// <param name="processBatchAsync">Callback that processes each completed batch.</param>
        /// <param name="cancellationToken">Token used to stop the consumer loop.</param>
        /// <returns>A task that completes when the channel has been fully drained.</returns>
        public async Task ConsumeAsync(
            Func<IReadOnlyCollection<T>, CancellationToken, Task> processBatchAsync,
            CancellationToken cancellationToken = default)
        {
            if (processBatchAsync == null)
            {
                throw new ArgumentNullException(nameof(processBatchAsync));
            }

            var batch = new List<T>(_batchSize);

            while (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_reader.TryRead(out var item))
                {
                    batch.Add(item);

                    if (batch.Count < _batchSize)
                    {
                        continue;
                    }

                    await processBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                    batch.Clear();
                }

                if (batch.Count > 0)
                {
                    await processBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await processBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reads a single batch from the channel.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the read operation.</param>
        /// <returns>
        /// A batch containing up to the configured batch size.
        /// Returns an empty collection when the channel is completed and no more items remain.
        /// </returns>
        public async ValueTask<IReadOnlyList<T>> ReadBatchAsync(CancellationToken cancellationToken = default)
        {
            var batch = new List<T>(_batchSize);

            if (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false) == false)
            {
                return batch;
            }

            while (batch.Count < _batchSize && _reader.TryRead(out var item))
            {
                batch.Add(item);
            }

            return batch;
        }
    }
}