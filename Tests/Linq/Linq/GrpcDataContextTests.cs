#if !NETFRAMEWORK
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Grpc.Net.Client;

using LinqToDB;
using LinqToDB.Async;
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

		static async Task RunQueries(IDataContext db)
		{
			for (var i = 0; i < QueriesPerContext; i++)
			{
				_ = await db.GetTable<Person>().CountAsync();
				_ = db.GetTable<Person>().ToList();
			}
		}

		// Sets up the test host for this test's provider and returns its address.
		string GetServerAddress(string context)
		{
			using var db = (TestGrpcDataContext)GetDataContext(context, transport: RemoteTransport.gRPC);

			return db.ServerAddress;
		}

		// A SocketsHttpHandler is the handler a channel builds for itself when given none. Grpc.Net.Client opens
		// a connection per channel over it, the same as over its own handler, so it measures what a default
		// channel does. It is needed only to accept the test host's self-signed certificate.
		static SocketsHttpHandler CreateHandler()
		{
			var handler = new SocketsHttpHandler();

			// the test host's certificate is a throwaway self-signed one, made for this process only
#pragma warning disable MA0039 // Do not write your own certificate validation method
#pragma warning disable CA5359 // Do not disable certificate validation
			handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
#pragma warning restore CA5359 // Do not disable certificate validation
#pragma warning restore MA0039 // Do not write your own certificate validation method

			return handler;
		}
	}
}
#endif
