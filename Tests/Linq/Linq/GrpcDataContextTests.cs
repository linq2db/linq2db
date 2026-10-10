#if !NETFRAMEWORK
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Net.Client;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Remote;
using LinqToDB.Remote.Grpc;

using NUnit.Framework;

using Shouldly;

using Tests.Model;
using Tests.Model.Remote.Grpc;
using Tests.Remote.ServerContainer;

namespace Tests.Linq
{
	// The test host counts the TCP connections it accepts. Remote tests run one at a time, so the difference
	// between two readings is what the test itself opened.
	[TestFixture]
	public sealed class GrpcDataContextTests : TestBase
	{
		const int ContextCount      = 3;
		const int QueriesPerContext = 4;

		// A caller's channel is used for every query of every context built over it, and stays open when they are
		// disposed: all of them together open one connection.
		[Test]
		public async Task CallerChannelIsSharedByContexts([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			var address = GetServerAddress(context);

			using var handler = CreateHandler();
			using var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions { HttpHandler = handler });

			var before = GrpcServerConnections.Accepted;

			for (var i = 0; i < ContextCount; i++)
			{
				await using var db = new GrpcDataContext(channel, o => o.UseConfiguration(context.StripRemote()));

				await RunQueries(db);
			}

			// the contexts are gone and the channel still works
			using (var db = new GrpcDataContext(channel, o => o.UseConfiguration(context.StripRemote())))
				_ = db.GetTable<Person>().Count();

			(GrpcServerConnections.Accepted - before).ShouldBe(1);
		}

		// A context built from an address creates its channel once and keeps it for all of its queries: one
		// connection per context, not one per query.
		[Test]
		public async Task ContextReusesItsChannel([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			var address = GetServerAddress(context);

			using var handler = CreateHandler();

			var before = GrpcServerConnections.Accepted;

			for (var i = 0; i < ContextCount; i++)
			{
				var db = new GrpcDataContext(address, new GrpcChannelOptions { HttpHandler = handler }, o => o.UseConfiguration(context.StripRemote()));

				await using (db)
					await RunQueries(db);

				Shouldly.Should.Throw<ObjectDisposedException>(() => db.GetTable<Person>().ToList());
			}

			(GrpcServerConnections.Accepted - before).ShouldBe(ContextCount);
		}

		// Cancelling the caller's token while the server runs the query ends the call with the
		// OperationCanceledException every other async API throws, and cancels the query on the server.
		[Test]
		public async Task CancelledQueryThrowsOperationCanceled([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			var interceptor = new BlockingInterceptor();

			using var db = GetDataContext(context, interceptor: interceptor, transport: RemoteTransport.gRPC);
			using var cts = new CancellationTokenSource();

			interceptor.Arm();

			var query = db.Person.ToListAsync(cts.Token);

			await interceptor.Entered.WaitAsync(Wait);
			cts.Cancel();

			var exception = Assert.CatchAsync(() => query);

			exception.ShouldBeOfType<OperationCanceledException>();
			((OperationCanceledException)exception).CancellationToken.ShouldBe(cts.Token);
			exception.InnerException.ShouldBeOfType<RpcException>().StatusCode.ShouldBe(StatusCode.Cancelled);

			(await interceptor.Released.WaitAsync(Wait)).ShouldBeTrue("the server query was not cancelled");
		}

		// Every call made with a token that is already cancelled fails the same way, without reaching the server -
		// ExecuteBatchAsync included, which used to send the batch without the token.
		[Test]
		public void CancelledTokenThrowsOperationCanceled([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			var address = GetServerAddress(context);

			using var handler = CreateHandler();
			using var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions { HttpHandler = handler });
			using var cts     = new CancellationTokenSource();

			cts.Cancel();

			ILinqService client        = new GrpcLinqServiceClient(channel);
			var          configuration = context.StripRemote();

			AssertCancelled(() => client.GetInfoAsync        (configuration,          cts.Token));
			AssertCancelled(() => client.ExecuteNonQueryAsync(configuration, "query", cts.Token));
			AssertCancelled(() => client.ExecuteScalarAsync  (configuration, "query", cts.Token));
			AssertCancelled(() => client.ExecuteReaderAsync  (configuration, "query", cts.Token));
			AssertCancelled(() => client.ExecuteBatchAsync   (configuration, "query", cts.Token));

			void AssertCancelled(Func<Task> call)
			{
				var exception = Assert.CatchAsync(call);

				// grpc-dotnet itself throws it for a token cancelled before the call starts
				exception.ShouldBeOfType<OperationCanceledException>();
				((OperationCanceledException)exception).CancellationToken.ShouldBe(cts.Token);
			}
		}

		// A call cancelled by something other than the caller's token - here HttpClient.Timeout - is not the caller's
		// cancellation and still surfaces as the RpcException.
		[Test]
		public async Task TimeoutStaysRpcException([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			var interceptor = new BlockingInterceptor();
			var address     = GetServerAddress(context, interceptor);

#pragma warning disable MA0039 // Do not write your own certificate validation method
			using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
#pragma warning restore MA0039 // Do not write your own certificate validation method
			using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(1) };

			using var db = new GrpcDataContext(address, new GrpcChannelOptions { HttpClient = httpClient }, o => o.UseConfiguration(context.StripRemote()));

			await ((RemoteDataContextBase)db).ConfigureAsync(default);

			interceptor.Arm();

			var exception = Assert.CatchAsync(() => db.GetTable<Person>().ToListAsync());

			exception.ShouldBeOfType<RpcException>().StatusCode.ShouldBe(StatusCode.Cancelled);

			(await interceptor.Released.WaitAsync(Wait)).ShouldBeTrue("the server query was not cancelled");
		}

		// A batch committed with a token cancelled while the server runs it is rolled back on the server.
		[Test]
		public async Task CancelledBatchIsNotCommitted([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			var interceptor = new BlockingInterceptor();

			using var db    = GetDataContext(context, interceptor: interceptor, transport: RemoteTransport.gRPC);
			using var table = db.CreateLocalTable<BatchRow>();
			using var cts   = new CancellationTokenSource();

			var remote = (RemoteDataContextBase)db;

			remote.BeginBatch();
			db.Insert(new BatchRow { Id = 1 });

			interceptor.Arm();

			var commit = remote.CommitBatchAsync(cts.Token);

			await interceptor.Entered.WaitAsync(Wait);
			cts.Cancel();

			Assert.CatchAsync(() => commit).ShouldBeOfType<OperationCanceledException>();

			(await interceptor.Released.WaitAsync(Wait)).ShouldBeTrue("the server batch was not cancelled");

			table.Count().ShouldBe(0);
		}

		[Table]
		sealed class BatchRow
		{
			[PrimaryKey] public int Id { get; set; }
		}

		static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		// Holds the next command the server runs, once armed, until the server's cancellation token fires or
		// BlockFor elapses, so a test can cancel a call while the server is inside it.
		sealed class BlockingInterceptor : CommandInterceptor
		{
			static readonly TimeSpan BlockFor = TimeSpan.FromSeconds(10);

			int _armed;

			readonly TaskCompletionSource       _entered  = new(TaskCreationOptions.RunContinuationsAsynchronously);
			readonly TaskCompletionSource<bool> _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

			public Task       Entered  => _entered.Task;
			// true when the server's token ended the wait
			public Task<bool> Released => _released.Task;

			public void Arm()
			{
				Volatile.Write(ref _armed, 1);
			}

			async Task Block(CancellationToken cancellationToken)
			{
				if (Interlocked.Exchange(ref _armed, 0) == 0)
					return;

				_entered.TrySetResult();

				try
				{
					await Task.Delay(BlockFor, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					_released.TrySetResult(true);
					throw;
				}

				_released.TrySetResult(false);
			}

			public override async Task<Option<int>> ExecuteNonQueryAsync(CommandEventData eventData, DbCommand command, Option<int> result, CancellationToken cancellationToken)
			{
				await Block(cancellationToken);
				return result;
			}

			public override async Task<Option<object?>> ExecuteScalarAsync(CommandEventData eventData, DbCommand command, Option<object?> result, CancellationToken cancellationToken)
			{
				await Block(cancellationToken);
				return result;
			}

			public override async Task<Option<DbDataReader>> ExecuteReaderAsync(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken)
			{
				await Block(cancellationToken);
				return result;
			}
		}

		static async Task RunQueries(IDataContext db)
		{
			for (var i = 0; i < QueriesPerContext; i++)
			{
				_ = await db.GetTable<Person>().CountAsync();
				_ = db.GetTable<Person>().ToList();
			}
		}

		// Sets up the test host for this test's provider and returns its address.
		string GetServerAddress(string context, IInterceptor? serverInterceptor = null)
		{
			using var db = (TestGrpcDataContext)GetDataContext(context, interceptor: serverInterceptor, transport: RemoteTransport.gRPC);

			return db.ServerAddress;
		}

		// A SocketsHttpHandler is the handler a channel builds for itself when given none. Grpc.Net.Client opens
		// a connection per channel over it, the same as over its own handler, so it measures what a default
		// channel does. It is needed only to accept the test host's self-signed certificate.
		static SocketsHttpHandler CreateHandler()
		{
			var handler = new SocketsHttpHandler();

#pragma warning disable MA0039 // Do not write your own certificate validation method
			handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
#pragma warning restore MA0039 // Do not write your own certificate validation method

			return handler;
		}
	}
}
#endif
