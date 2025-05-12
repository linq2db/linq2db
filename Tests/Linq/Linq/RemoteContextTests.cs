using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
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
	}
}
