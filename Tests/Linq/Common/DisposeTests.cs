using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Common
{
	[TestFixture]
	public class DisposeTests : TestBase
	{
		[Obsolete("This API will be removed in version 7. Use DataContext with SetKeepConnectionAlive[Async] instead."), EditorBrowsable(EditorBrowsableState.Never)]
		[Test]
		public void DoubleDispose_DataConnection([IncludeDataSources(ProviderName.SQLiteClassic)] string context, [Values] bool closeAfterUse)
		{
			using var db = new DataConnection(context);
			((IDataContext)db).CloseAfterUse = closeAfterUse;
			_ = db.GetTable<Person>().ToArray();
			db.Dispose();
		}

		[Obsolete("This API will be removed in version 7. Use DataContext with SetKeepConnectionAlive[Async] instead."), EditorBrowsable(EditorBrowsableState.Never)]
		[Test]
		public void DoubleDispose_DataContext_Old([IncludeDataSources(ProviderName.SQLiteClassic)] string context, [Values] bool closeAfterUse)
		{
			using var db = new DataContext(context);
			((IDataContext)db).CloseAfterUse = closeAfterUse;
			_ = db.GetTable<Person>().ToArray();
			db.Dispose();
		}

		[Test]
		public void DoubleDispose_DataContext([IncludeDataSources(ProviderName.SQLiteClassic)] string context, [Values] bool closeAfterUse)
		{
			using var db = new DataContext(context);
			db.SetKeepConnectionAlive(closeAfterUse);
			_ = db.GetTable<Person>().ToArray();
			db.Dispose();
		}

		[Obsolete("This API will be removed in version 7. Use DataContext with SetKeepConnectionAlive[Async] instead."), EditorBrowsable(EditorBrowsableState.Never)]
		[Test]
		public void DoubleDispose_RemoteDataContext([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context, [Values] bool closeAfterUse)
		{
			if (!context.IsRemote())
				Assert.Inconclusive("Skip non-remote context");

			using var db = GetDataContext(context);
			((IDataContext)db).CloseAfterUse = closeAfterUse;
			_ = db.Person.ToArray();
			db.Dispose();
		}

		[Obsolete("This API will be removed in version 7. Use DataContext with SetKeepConnectionAlive[Async] instead."), EditorBrowsable(EditorBrowsableState.Never)]
		[Test]
		public async ValueTask DoubleDisposeAsync_DataConnection([IncludeDataSources(ProviderName.SQLiteClassic)] string context, [Values] bool closeAfterUse)
		{
			await using var db = new DataConnection(context);
			((IDataContext)db).CloseAfterUse = closeAfterUse;
			_ = db.GetTable<Person>().ToArray();
			await db.DisposeAsync();
		}

		[Obsolete("This API will be removed in version 7. Use DataContext with SetKeepConnectionAlive[Async] instead."), EditorBrowsable(EditorBrowsableState.Never)]
		[Test]
		public async ValueTask DoubleDisposeAsync_DataContext_Old([IncludeDataSources(ProviderName.SQLiteClassic)] string context, [Values] bool closeAfterUse)
		{
			await using var db = new DataContext(context);
			((IDataContext)db).CloseAfterUse = closeAfterUse;
			_ = db.GetTable<Person>().ToArray();
			await db.DisposeAsync();
		}

		[Test]
		public async ValueTask DoubleDisposeAsync_DataContext([IncludeDataSources(ProviderName.SQLiteClassic)] string context, [Values] bool closeAfterUse)
		{
			await using var db = new DataContext(context);
			await db.SetKeepConnectionAliveAsync(closeAfterUse);
			_ = db.GetTable<Person>().ToArray();
			await db.DisposeAsync();
		}

		[Obsolete("This API will be removed in version 7. Use DataContext with SetKeepConnectionAlive[Async] instead."), EditorBrowsable(EditorBrowsableState.Never)]
		[Test]
		public async ValueTask DoubleDisposeAsync_RemoteDataContext([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context, [Values] bool closeAfterUse)
		{
			if (!context.IsRemote())
				Assert.Inconclusive("Skip non-remote context");

			await using var db = GetDataContext(context);
			((IDataContext)db).CloseAfterUse = closeAfterUse;
			_ = db.Person.ToArray();
			await db.DisposeAsync();
		}
	}
}
