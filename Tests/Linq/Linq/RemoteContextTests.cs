using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.DataProvider.SQLite;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.Remote;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public sealed class RemoteContextTests : TestBase
	{
		[Test]
		public void TestILinqService_GetInfo([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			using var db = GetDataContext(context, transport: transport);

			_ = db.MappingSchema;
		}

		[Test]
		public void TestILinqService_ExecuteNonQuery([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			using var db = GetDataContext(context, transport: transport);

			_ = db.Person.Where(r => r.ID == -1).Delete();
		}

		[Test]
		public void TestILinqService_ExecuteScalar([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			using var db = GetDataContext(context, transport: transport);

			_ = db.Person.Count();
		}

		[Test]
		public void TestILinqService_ExecuteReader([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			using var db = GetDataContext(context, transport: transport);

			_ = db.Person.ToArray();
		}

		[Test]
		public async Task TestILinqService_ExecuteBatch([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			using var db = GetDataContext(context, transport: transport);

			var dbRemote = (RemoteDataContextBase)db;

			dbRemote.BeginBatch();

			_ = db.Person.Where(r => r.ID == -1).Delete();
			_ = await db.Person.Where(r => r.ID == -2).DeleteAsync();

			dbRemote.CommitBatch();
		}

		[Test]
		public async Task TestILinqService_GetInfoAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			using var db = GetDataContext(context, transport: transport);

			await ((RemoteDataContextBase)db).ConfigureAsync(default);
		}

		[Test]
		public async Task TestILinqService_ExecuteNonQueryAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			using var db = GetDataContext(context, transport: transport);

			_ = await db.Person.Where(r => r.ID == -1).DeleteAsync();
		}

		[Test]
		public async Task TestILinqService_ExecuteScalarAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			using var db = GetDataContext(context, transport: transport);

			_ = await db.Person.CountAsync();
		}

		[Test]
		public async Task TestILinqService_ExecuteReaderAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			using var db = GetDataContext(context, transport: transport);

			_ = await db.Person.ToArrayAsync();
		}

		[Test]
		public async Task TestILinqService_ExecuteBatchAsync([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			using var db = GetDataContext(context, transport: transport);

			var dbRemote = (RemoteDataContextBase)db;

			dbRemote.BeginBatch();

			_ = db.Person.Where(r => r.ID == -1).Delete();
			_ = await db.Person.Where(r => r.ID == -2).DeleteAsync();

			await dbRemote.CommitBatchAsync();
		}

		sealed class CustomSqliteProvider : SQLiteDataProvider
		{
			public CustomSqliteProvider(RemoteTransport transport)
				: base($"{ProviderName.SQLiteClassic}:{transport}", SQLiteProvider.System)
			{
			}
		}

		[Test]
		public void TestFlagsTransfered([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context, [Values] RemoteTransport transport)
		{
			if (!context.IsRemote()) Assert.Ignore("Skip non-remote context");

			var provider = new CustomSqliteProvider(transport);

			var originalFlags = provider.SqlProviderFlags;

			// bool
			originalFlags.IsAccessBuggyLeftJoinConstantNullability = true;
			originalFlags.SupportsPredicatesComparison = false;

			// nullable enum
			originalFlags.TakeHintsSupported = TakeHints.WithTies;
			// enum
			originalFlags.DefaultMultiQueryIsolationLevel = IsolationLevel.Chaos;

			// int
			originalFlags.MaxInListValuesCount = -123;
			// int?
			originalFlags.SupportedCorrelatedSubqueriesLevel = 234;

			// hashset
			originalFlags.CustomFlags.Add($"{context}:{transport}:flag1");
			originalFlags.CustomFlags.Add($"{context}:{transport}:flag2");

			var configuration = $"{context}:{transport}";
			DataConnection.AddConfiguration(configuration, "unused", provider);
			using var db = GetDataContext(context, o => o.UseDataProvider(provider).UseConfiguration(configuration), transport: transport);

			var remoteFlags = db.SqlProviderFlags;

			Assert.That(remoteFlags, Is.Not.SameAs(originalFlags));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(remoteFlags.IsAccessBuggyLeftJoinConstantNullability, Is.EqualTo(originalFlags.IsAccessBuggyLeftJoinConstantNullability));
				Assert.That(remoteFlags.SupportsPredicatesComparison, Is.EqualTo(originalFlags.SupportsPredicatesComparison));
				Assert.That(remoteFlags.TakeHintsSupported, Is.EqualTo(originalFlags.TakeHintsSupported));
				Assert.That(remoteFlags.DefaultMultiQueryIsolationLevel, Is.EqualTo(originalFlags.DefaultMultiQueryIsolationLevel));
				Assert.That(remoteFlags.MaxInListValuesCount, Is.EqualTo(originalFlags.MaxInListValuesCount));
				Assert.That(remoteFlags.SupportedCorrelatedSubqueriesLevel, Is.EqualTo(originalFlags.SupportedCorrelatedSubqueriesLevel));

				Assert.That(remoteFlags.CustomFlags, Has.Count.EqualTo(originalFlags.CustomFlags.Count));
			}

			foreach (var flag in originalFlags.CustomFlags)
			{
				Assert.That(remoteFlags.CustomFlags, Does.Contain(flag));
			}
		}

		// InsertOrReplace is the shape that exposes this: it builds a two-query plan and
		// QueryRunner.NonQueryQuery2 runs both on the *same* IQueryRunner (UPDATE, then INSERT when the
		// update matched nothing), while the runner is disposed once at the end. The remote runner used to
		// assign _client = GetClient() unconditionally on every execute, so the second one overwrote the
		// first and the first was never disposed - one leaked client (and, for a real transport, its
		// connection) per InsertOrReplace. Counting creates against disposes catches that without depending
		// on any particular transport's internals.
		[Test]
		public async Task RemoteRunnerDoesNotLeakClientAcrossTwoQueryPlan()
		{
			// A file, not ":memory:": linq2db opens and closes a connection per query, and an unshared
			// in-memory SQLite database dies with the connection that created it, taking the table with it.
			var databasePath = Path.Combine(Path.GetTempPath(), $"linq2db-remote-client-lifetime-{Guid.NewGuid():N}.sqlite");

			try
			{
				// SQLite has a native upsert, and with it InsertOrReplace compiles to a single statement -
				// which never reaches the two-execute path this is about. Turning the flag off selects
				// MakeAlternativeInsertOrUpdate's UPDATE-then-INSERT plan instead, which is the shape every
				// provider without native support (Access among them) really uses.
				var provider = new NoNativeUpsertSqliteProvider();

				await using var backend = new DataConnection(new DataOptions()
					.UseConnectionString($"Data Source={databasePath}")
					.UseDataProvider(provider));

				await backend.CreateTableAsync<ClientLifetimeEntity>();

				await using var context = new CountingRemoteDataContext(backend);

				await context.InsertOrReplaceAsync(new ClientLifetimeEntity { Id = 1, Value = "first" });

				using (Assert.EnterMultipleScope())
				{
					Assert.That(context.NonQueryExecutions, Is.EqualTo(2), "the two-query InsertOrReplace plan did not run - the test no longer exercises the path it was written for");
					Assert.That(context.ClientsDisposed, Is.EqualTo(context.ClientsCreated), "every client the runner obtained must be disposed");
				}
			}
			finally
			{
				try
				{
					Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
					File.Delete(databasePath);
				}
				catch
				{
					// best-effort: a temp file left behind is not worth failing the test over
				}
			}
		}

		[Table]
		sealed class ClientLifetimeEntity
		{
			[PrimaryKey] public int     Id    { get; set; }
			[Column]     public string? Value { get; set; }
		}

		sealed class NoNativeUpsertSqliteProvider : SQLiteDataProvider
		{
			public NoNativeUpsertSqliteProvider() : base("SQLite.NoNativeUpsert", SQLiteProvider.Microsoft)
			{
				SqlProviderFlags.IsInsertOrUpdateSupported = false;
			}
		}

		// Serves the remote protocol in-process, straight off a real database, so the test exercises the
		// actual runner/serialization path rather than a mock of it. Only the client's lifetime is
		// instrumented.
		sealed class CountingRemoteDataContext : RemoteDataContextBase
		{
			readonly DataOptions _backendOptions;

			// RemoteDataContextBase caches ConfigurationInfo process-wide keyed on ConfigurationString, so a
			// context left without one writes SQLite-with-upsert-disabled under the key every configuration-less
			// remote context shares. The name resolves to nothing - BackendLinqService ignores it.
			public CountingRemoteDataContext(DataConnection backend) : base(new DataOptions().UseConfiguration("Test.CountingRemote"))
			{
				_backendOptions = backend.Options;
			}

			public int ClientsCreated     { get; private set; }
			public int ClientsDisposed    { get; private set; }
			public int NonQueryExecutions { get; private set; }

			protected override string ContextIDPrefix => "Test.CountingRemote";

			protected override ILinqService GetClient()
			{
				ClientsCreated++;

				return new CountingClient(this, new BackendLinqService(_backendOptions) { AllowUpdates = true });
			}

			// LinqService's own CreateDataContext builds a DataConnection from a *configuration name*; this
			// test has no registered configuration, only an explicit set of options, so it has to be pointed
			// at them directly.
			sealed class BackendLinqService : LinqService
			{
				readonly DataOptions _options;

				public BackendLinqService(DataOptions options)
				{
					_options = options;
				}

				public override DataConnection CreateDataContext(string? configuration) => new (_options);
			}

			sealed class CountingClient : ILinqService, IDisposable
			{
				readonly CountingRemoteDataContext _owner;
				readonly ILinqService              _inner;

				public CountingClient(CountingRemoteDataContext owner, ILinqService inner)
				{
					_owner = owner;
					_inner = inner;
				}

				public string? RemoteClientTag { get => _inner.RemoteClientTag; set => _inner.RemoteClientTag = value; }

				public void Dispose() => _owner.ClientsDisposed++;

				public Task<int> ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
				{
					_owner.NonQueryExecutions++;

					return _inner.ExecuteNonQueryAsync(configuration, queryData, cancellationToken);
				}

				public Task<LinqServiceInfo> GetInfoAsync        (string? configuration, CancellationToken cancellationToken = default) => _inner.GetInfoAsync(configuration, cancellationToken);
				public Task<string?>         ExecuteScalarAsync  (string? configuration, string queryData, CancellationToken cancellationToken = default) => _inner.ExecuteScalarAsync(configuration, queryData, cancellationToken);
				public Task<string>          ExecuteReaderAsync  (string? configuration, string queryData, CancellationToken cancellationToken = default) => _inner.ExecuteReaderAsync(configuration, queryData, cancellationToken);
				public Task<int>             ExecuteBatchAsync   (string? configuration, string queryData, CancellationToken cancellationToken = default) => _inner.ExecuteBatchAsync(configuration, queryData, cancellationToken);
			}
		}
	}
}
