using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.DataProvider.SQLite;
using LinqToDB.Internal.SqlProvider;
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
	}
}
