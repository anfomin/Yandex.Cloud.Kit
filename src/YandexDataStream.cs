using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Microsoft.Extensions.Options;

namespace Yandex.Cloud;

/// <summary>
/// Provides method to work with Yandex.Cloud DataStreams.
/// </summary>
public class YandexDataStream
{
	static readonly JsonSerializerOptions JsonOptions = new()
	{
		Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters =
		{
			new JsonStringEnumConverter()
		}
	};
	const string ServiceUrl = "https://yds.serverless.yandexcloud.net";
	readonly ILogger _logger;
	readonly AmazonKinesisClient _client;
	readonly YandexCloudOptions _cloudOptions;
	readonly YandexDataStreamOptions _dsOptions;

	public YandexDataStream(ILogger<YandexDataStream> logger, IOptions<YandexCloudOptions> cloudOptions, IOptions<YandexDataStreamOptions> dsOptions)
	{
		_logger = logger;
		_cloudOptions = cloudOptions.Value;
		_dsOptions = dsOptions.Value;
		if (string.IsNullOrEmpty(_cloudOptions.AccountKey))
			throw new ArgumentException("Yandex.Cloud option AccountKey is required", nameof(cloudOptions));
		if (string.IsNullOrEmpty(_cloudOptions.SecretKey))
			throw new ArgumentException("Yandex.Cloud option SecretKey is required", nameof(cloudOptions));
		if (string.IsNullOrEmpty(_cloudOptions.FolderId))
			throw new ArgumentException("Yandex.Cloud option FolderId is required", nameof(cloudOptions));
		if (string.IsNullOrEmpty(_dsOptions.DatabaseId))
			throw new ArgumentException("Yandex.Cloud data stream option DatabaseId is required", nameof(dsOptions));

		_client = new(_cloudOptions.AccountKey, _cloudOptions.SecretKey, new AmazonKinesisConfig
		{
			ServiceURL = ServiceUrl,
			AuthenticationRegion = _cloudOptions.Region
		});
	}

	/// <summary>
	/// Subscribes to the new messages of the <paramref name="streamName"/> stream.
	/// </summary>
	/// <param name="streamName">Stream name to subscribe to.</param>
	/// <typeparam name="TMessage">Type of message to deserialize JSON to.</typeparam>
	public IAsyncEnumerable<Record<TMessage>> SubscribeAsync<TMessage>(string streamName)
		=> AsyncEnumerable.Create(cancel => new StreamEnumerator<TMessage>(_logger, _client, _cloudOptions, _dsOptions, streamName, cancel));

	/// <summary>
	/// Subscribes to the messages of the <paramref name="streamName"/> stream received after <paramref name="timestamp"/> and later.
	/// </summary>
	/// <param name="streamName">Stream name to subscribe to.</param>
	/// <param name="timestamp">Minimum message timestamp to receive. Not used for future new messages.</param>
	/// <typeparam name="TMessage">Type of message to deserialize JSON to.</typeparam>
	public IAsyncEnumerable<Record<TMessage>> SubscribeSinceAsync<TMessage>(string streamName, DateTime timestamp)
		=> AsyncEnumerable.Create(cancel => new StreamEnumerator<TMessage>(
			_logger, _client, _cloudOptions, _dsOptions, streamName, cancel,
			c =>
			{
				c.ShardIteratorType = ShardIteratorType.AT_TIMESTAMP;
				c.Timestamp = timestamp;
			}
		));

	/// <summary>
	/// Subscribes to the messages of the <paramref name="streamName"/> stream received for previous <paramref name="prevTime"/> and later.
	/// </summary>
	/// <param name="streamName">Stream name to subscribe to.</param>
	/// <param name="prevTime">Previous time to get messages for. Not used for future new messages.</param>
	/// <typeparam name="TMessage">Type of message to deserialize JSON to.</typeparam>
	public IAsyncEnumerable<Record<TMessage>> SubscribeSinceAsync<TMessage>(string streamName, TimeSpan prevTime)
		=> SubscribeSinceAsync<TMessage>(streamName, DateTime.UtcNow.Subtract(prevTime));

	/// <summary>
	/// Subscribes to the messages of the <paramref name="streamName"/> stream received after <paramref name="sequenceNumber"/>.
	/// </summary>
	/// <param name="streamName">Stream name to subscribe to.</param>
	/// <param name="sequenceNumber">Sequence number to receive messages after.</param>
	/// <typeparam name="TMessage">Type of message to deserialize JSON to.</typeparam>
	public IAsyncEnumerable<Record<TMessage>> SubscribeAfterAsync<TMessage>(string streamName, string sequenceNumber)
		=> AsyncEnumerable.Create(cancel => new StreamEnumerator<TMessage>(
			_logger, _client, _cloudOptions, _dsOptions, streamName, cancel,
			c =>
			{
				c.ShardIteratorType = ShardIteratorType.AFTER_SEQUENCE_NUMBER;
				c.StartingSequenceNumber = sequenceNumber;
			}
		));

	class StreamEnumerator<TMessage>(
		ILogger logger,
		AmazonKinesisClient client,
		YandexCloudOptions cloudOptions,
		YandexDataStreamOptions dsOptions,
		string streamName,
		CancellationToken cancellationToken,
		Action<GetShardIteratorRequest>? iteratorConfig = null)
		: IAsyncEnumerator<Record<TMessage>>
	{
		readonly ILogger _logger = logger;
		readonly AmazonKinesisClient _client = client;
		readonly string _streamName = streamName;
		readonly string _streamPath = $"/{cloudOptions.Region}/{cloudOptions.FolderId}/{dsOptions.DatabaseId}/{streamName}";
		readonly TimeSpan _pollingInterval = dsOptions.PollingInterval;
		readonly CancellationToken _cancellationToken = cancellationToken;
		readonly Action<GetShardIteratorRequest>? _iteratorConfig = iteratorConfig;
		readonly ConcurrentQueue<Record<TMessage>> _queue = new();
		readonly SemaphoreSlim _signal = new(0);
		Task? _streamTask;
		bool _disposed;

		public Record<TMessage> Current { get; private set; }

		public async ValueTask<bool> MoveNextAsync()
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(StreamEnumerator<TMessage>));

			_streamTask ??= Task.Run(SubscribeToStream, _cancellationToken);
			await _signal.WaitAsync(_cancellationToken);
			if (!_queue.TryDequeue(out var message))
				return false;

			Current = message;
			return true;
		}

		async Task SubscribeToStream()
		{
			// _logger.LogDebug("DataStream {Stream} loading shards", _streamName);
			var describeRequest = new DescribeStreamRequest { StreamName = _streamPath };
			var describeResponse = await _client.DescribeStreamAsync(describeRequest, _cancellationToken);
			var shards = describeResponse.StreamDescription.Shards;
			_logger.LogDebug("DataStream {Stream} subscribing {Shards} shards", _streamName, shards.Count);
			await Parallel.ForEachAsync(shards, _cancellationToken, ConsumeShardAsync);
		}

		async ValueTask ConsumeShardAsync(Shard shard, CancellationToken cancellationToken)
		{
			var iteratorRequest = new GetShardIteratorRequest
			{
				StreamName = _streamPath,
				ShardId = shard.ShardId,
				ShardIteratorType = ShardIteratorType.LATEST
			};
			_iteratorConfig?.Invoke(iteratorRequest);
			var iteratorResponse = await _client.GetShardIteratorAsync(iteratorRequest, cancellationToken);
			var recordsRequest = new GetRecordsRequest { ShardIterator = iteratorResponse.ShardIterator };
			while (!cancellationToken.IsCancellationRequested)
			{
				var recordsResponse = await _client.GetRecordsAsync(recordsRequest, cancellationToken);
				// _logger.LogDebug("DataStream {Stream} received {Count} records", _streamName, recordsResponse.Records.Count);
				foreach (var record in recordsResponse.Records)
				{
					TMessage? message;
					try
					{
						message = JsonSerializer.Deserialize<TMessage>(record.Data, JsonOptions);
						_logger.LogDebug("DataStream {Stream} received record {Shard}/{Sequence}", _streamName, shard.ShardId, record.SequenceNumber);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "DataStream {Stream} error deserializing record {Shard}/{Sequence}", _streamName, shard.ShardId, record.SequenceNumber);
						continue;
					}

					if (message != null)
					{
						_queue.Enqueue(new (record.SequenceNumber, record.ApproximateArrivalTimestamp, message));
						_signal.Release();
					}
				}
				recordsRequest.ShardIterator = recordsResponse.NextShardIterator;
				await Task.Delay(_pollingInterval, cancellationToken);
			}
		}

		public ValueTask DisposeAsync()
		{
			_streamTask?.Dispose();
			_disposed = true;
			return ValueTask.CompletedTask;
		}
	}

	public readonly record struct Record<TMessage>(string SequenceNumber, DateTime Timestamp, TMessage Message)
	{
		public override string ToString()
			=> $"Record {SequenceNumber}";
	}
}