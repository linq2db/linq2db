﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Informix;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.xUpdate
{
	using LinqToDB.DataProvider.Oracle;
	using LinqToDB.Interceptors;

	using Model;

	[TestFixture]
	[Order(10000)]
	public class BulkCopyTests : TestBase
	{
		// TODO: update Sybase.sql to use proper type for identity. now it uses INT for most of tables, which
		// is silently treated as non-identity field
		[Table("KeepIdentityTest", Configuration = ProviderName.DB2)]
		[Table("KeepIdentityTest", Configuration = ProviderName.Sybase)]
		[Table("AllTypes")]
		public class TestTable1
		{
			[Column(Configuration = ProviderName.ClickHouse)]
			[Column(IsIdentity = true)]
			public int ID { get; set; }

			[Column("intDataType")]
			[Column("Value", Configuration = ProviderName.DB2)]
			[Column("Value", Configuration = ProviderName.Sybase)]
			public int Value { get; set; }
		}

		[Table("KeepIdentityTest", Configuration = ProviderName.DB2)]
		[Table("KeepIdentityTest", Configuration = ProviderName.Sybase)]
		[Table("AllTypes")]
		public class TestTable2
		{
			[Column(SkipOnInsert = true, Configuration = ProviderName.ClickHouse)]
			[Column(SkipOnInsert = true, IsIdentity = true)]
			public int ID { get; set; }

			[Column("intDataType")]
			[Column("Value", Configuration = ProviderName.DB2)]
			[Column("Value", Configuration = ProviderName.Sybase)]
			public int Value { get; set; }
		}

		[Test]
		public async Task KeepIdentity_SkipOnInsertTrue(
			[DataSources(false, TestProvName.AllClickHouse)] string context,
			[Values(null, true, false)                     ] bool? keepIdentity,
			[Values                                        ] BulkCopyType copyType,
			[Values(0, 1, 2)                               ] int asyncMode) // 0 == sync, 1 == async, 2 == async with IAsyncEnumerable
		{
			if ((context == ProviderName.Sybase) && copyType == BulkCopyType.ProviderSpecific && keepIdentity != true)
				Assert.Inconclusive("Sybase native bulk copy doesn't support identity insert (despite documentation)");

			ResetAllTypesIdentity(context);

			if (context.IsAnyOf(TestProvName.AllOracleNative) && copyType == BulkCopyType.ProviderSpecific)
				Assert.Inconclusive("Oracle BulkCopy doesn't support identity triggers");

			// don't use transactions as some providers will fallback to non-provider-specific implementation then
			using (var db = GetDataConnection(context))
			{
				var lastId = db.InsertWithInt32Identity(new TestTable2());
				try
				{
					var options = new BulkCopyOptions()
					{
						KeepIdentity = keepIdentity,
						BulkCopyType = copyType
					};

					if (!await ExecuteAsync(db, context, perform, keepIdentity, copyType))
						return;

					var data = db.GetTable<TestTable2>().Where(_ => _.ID > lastId).OrderBy(_ => _.ID).ToArray();

					Assert.AreEqual(2, data.Length);

					// oracle supports identity insert only starting from version 12c, which is not used yet for tests
					var useGenerated = keepIdentity != true
						|| context.IsAnyOf(TestProvName.AllOracle);

					Assert.AreEqual(lastId + (!useGenerated ? 10 : 1), data[0].ID);
					Assert.AreEqual(200, data[0].Value);
					Assert.AreEqual(lastId + (!useGenerated ? 20 : 2), data[1].ID);
					Assert.AreEqual(300, data[1].Value);

					async Task perform()
					{
						var values = new[]
							{
								new TestTable2()
								{
									ID = lastId + 10,
									Value = 200
								},
								new TestTable2()
								{
									ID = lastId + 20,
									Value = 300
								}
							};
						if (asyncMode == 0) // synchronous
						{
							db.BulkCopy(
								options,
								values);
						}
						else if (asyncMode == 1) // asynchronous
						{
							await db.BulkCopyAsync(
								options,
								values);
						}
						else // asynchronous with IAsyncEnumerable
						{
							await db.BulkCopyAsync(
								options,
								AsAsyncEnumerable(values));
						}
					}
				}
				finally
				{
					// cleanup
					db.GetTable<TestTable2>().Delete(_ => _.ID >= lastId);
				}
			}
		}

		[Test]
		public async Task KeepIdentity_SkipOnInsertFalse(
			[DataSources(false, TestProvName.AllClickHouse)]
		                                string       context,
			[Values(null, true, false)] bool?        keepIdentity,
			[Values]                    BulkCopyType copyType,
			[Values(0, 1, 2)]           int          asyncMode) // 0 == sync, 1 == async, 2 == async with IAsyncEnumerable
		{
			if ((context == ProviderName.Sybase) && copyType == BulkCopyType.ProviderSpecific && keepIdentity != true)
				Assert.Inconclusive("Sybase native bulk copy doesn't support identity insert (despite documentation)");

			ResetAllTypesIdentity(context);

			// don't use transactions as some providers will fallback to non-provider-specific implementation then
			using (var db = GetDataConnection(context))
			{
				var lastId = db.InsertWithInt32Identity(new TestTable1());
				try
				{
					var options = new BulkCopyOptions()
					{
						KeepIdentity = keepIdentity,
						BulkCopyType = copyType
					};

					if (!await ExecuteAsync(db, context, perform, keepIdentity, copyType))
						return;

					var data = db.GetTable<TestTable1>().Where(_ => _.ID > lastId).OrderBy(_ => _.ID).ToArray();

					Assert.AreEqual(2, data.Length);

					// oracle supports identity insert only starting from version 12c, which is not used yet for tests
					var useGenerated = keepIdentity != true
						|| context.IsAnyOf(TestProvName.AllOracle);

					Assert.AreEqual(lastId + (!useGenerated ? 10 : 1), data[0].ID);
					Assert.AreEqual(200, data[0].Value);
					Assert.AreEqual(lastId + (!useGenerated ? 20 : 2), data[1].ID);
					Assert.AreEqual(300, data[1].Value);

					async Task perform()
					{
						var values = new[]
							{
								new TestTable1()
								{
									ID = lastId + 10,
									Value = 200
								},
								new TestTable1()
								{
									ID = lastId + 20,
									Value = 300
								}
							};
						if (asyncMode == 0) // synchronous
						{
							db.BulkCopy(
								options,
								values);
						}
						else if (asyncMode == 1) // asynchronous
						{
							await db.BulkCopyAsync(
								options,
								values);
						}
						else // asynchronous with IAsyncEnumerable
						{
							await db.BulkCopyAsync(
								options,
								AsAsyncEnumerable(values));
						}
					}
				}
				finally
				{
					// cleanup
					db.GetTable<TestTable1>().Delete(_ => _.ID >= lastId);
				}
			}
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		private async IAsyncEnumerable<T> AsAsyncEnumerable<T>(IEnumerable<T> enumerable)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			var enumerator = enumerable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current;
			}
		}

		private async Task<bool> ExecuteAsync(DataConnection db, string context, Func<Task> perform, bool? keepIdentity, BulkCopyType copyType)
		{
			if (context.IsAnyOf(TestProvName.AllFirebird)
				&& keepIdentity == true
				&& (copyType    == BulkCopyType.Default
					|| copyType == BulkCopyType.MultipleRows
					|| copyType == BulkCopyType.ProviderSpecific))
			{
				var ex = Assert.CatchAsync(async () => await perform())!;
				Assert.IsInstanceOf<LinqToDBException>(ex);
				Assert.AreEqual("BulkCopyOptions.KeepIdentity = true is not supported by Firebird provider. If you use generators with triggers, you should disable triggers during BulkCopy execution manually.", ex.Message);
				return false;
			}

			bool notSupported = false;
			if (context.IsAnyOf(TestProvName.AllInformix))
			{
				notSupported = !((InformixDataProvider)db.DataProvider).Adapter.IsIDSProvider
					|| copyType == BulkCopyType.MultipleRows;
			}

			// RowByRow right now uses DataConnection.Insert which doesn't support identity insert
			if ((copyType       == BulkCopyType.RowByRow
					|| context.IsAnyOf(TestProvName.AllAccess)
					|| notSupported
					|| (context.IsAnyOf(TestProvName.AllSapHana)
						&& (copyType == BulkCopyType.MultipleRows || copyType == BulkCopyType.Default))
					|| (context.IsAnyOf(ProviderName.SapHanaOdbc) && copyType == BulkCopyType.ProviderSpecific))
				&& keepIdentity == true)
			{
				var ex = Assert.CatchAsync(async () => await perform())!;
				Assert.IsInstanceOf<LinqToDBException>(ex);
				Assert.AreEqual("BulkCopyOptions.KeepIdentity = true is not supported by BulkCopyType.RowByRow mode", ex.Message);
				return false;
			}

			await perform();
			return true;
		}

		// DB2:
		[Test]
		public void ReuseOptionTest([DataSources(false, ProviderName.DB2)] string context)
		{
			using (var db = GetDataConnection(context))
			using (new RestoreBaseTables(db))
			using (db.BeginTransaction())
			{
				var options = GetDefaultBulkCopyOptions(context);

				db.Parent.BulkCopy(options, new[] { new Parent { ParentID = 111001 } });
				db.Child .BulkCopy(options, new[] { new Child { ParentID = 111001 } });
			}
		}

		// ClickHouse: parameters support not implemented (yet?)
		[Test]
		public void UseParametersTest([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using var db = new TestDataConnection(context);
			using var _ = new RestoreBaseTables(db);
			using var tr = db.BeginTransaction();
			var options = new BulkCopyOptions(){ UseParameters = true, MaxBatchSize = 50, BulkCopyType = BulkCopyType.MultipleRows };
			var start   = 111001;

			var rowsToInsert = Enumerable.Range(start, 149)
				.Select(r => new Parent() {ParentID = r, Value1 = r-start}).ToList();

			db.Parent.BulkCopy(options, rowsToInsert);

			Assert.AreEqual(rowsToInsert.Count,
				db.Parent.Where(r =>
					r.ParentID >= rowsToInsert[0].ParentID && r.ParentID <= rowsToInsert.Last().ParentID).Count());
		}

		[Table]
		public class SimpleBulkCopyTable
		{
			[Column] public int Id { get; set; }
		}

#if NET6_0_OR_GREATER
		[Table]
		public class DateOnlyTable
		{
			[Column] public DateOnly Date { get; set; }
		}
#endif		

		[Table]
		public class IdentitySimpleBulkCopyTable
		{
			[Column, Identity] public int Id { get; set; }
		}

		[Test]
		public void BulkCopyWithDataContext(
			[DataSources(false)]        string       context,
			[Values]                    BulkCopyType copyType)
		{
			using (var db = new DataContext(context))
			using (var table = db.CreateLocalTable<SimpleBulkCopyTable>())
			{
				var options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };
				db.DataProvider.BulkCopy(db.Options.WithOptions(options), table, new[] { new SimpleBulkCopyTable() { Id = 1 } });
			}
		}

		[Test]
		public async Task BulkCopyWithDataContextAsync(
			[DataSources(false)] string context,
			[Values] BulkCopyType copyType)
		{
			using (var db = new DataContext(context))
			using (var table = db.CreateLocalTable<SimpleBulkCopyTable>())
			{
				var options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };
				await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, new[] { new SimpleBulkCopyTable() { Id = 1 } }, default);

				options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };
				await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, AsyncEnumerableData(2, 1), default);
			}
		}

		[Test]
		public void BulkCopyWithDataContextFromTable(
			[DataSources(false)] string context,
			[Values] BulkCopyType copyType)
		{
			using (var db = new DataContext(context))
			using (var table = db.CreateLocalTable<SimpleBulkCopyTable>())
			{
				var options = GetDefaultBulkCopyOptions(context);
				table.BulkCopy(options, new[] { new SimpleBulkCopyTable() { Id = 1 } });

				options = GetDefaultBulkCopyOptions(context) with { MaxBatchSize = 5 };
				table.BulkCopy(options, new[] { new SimpleBulkCopyTable() { Id = 2 } });

				options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };
				table.BulkCopy(options, new[] { new SimpleBulkCopyTable() { Id = 3 } });
			}
		}

		[Test]
		public async Task BulkCopyWithDataContextFromTableAsync(
			[DataSources(false)] string context,
			[Values] BulkCopyType copyType)
		{
			using (var db = new DataContext(context))
			using (var table = db.CreateLocalTable<SimpleBulkCopyTable>())
			{
				var options = GetDefaultBulkCopyOptions(context);
				await table.BulkCopyAsync(options, new[] { new SimpleBulkCopyTable() { Id = 1 } });

				options = GetDefaultBulkCopyOptions(context) with { MaxBatchSize = 5 };
				await table.BulkCopyAsync(options, new[] { new SimpleBulkCopyTable() { Id = 2 } });

				options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };
				await table.BulkCopyAsync(options, new[] { new SimpleBulkCopyTable() { Id = 3 } });

				options = GetDefaultBulkCopyOptions(context);
				await table.BulkCopyAsync(options, AsyncEnumerableData(10, 1));

				options = GetDefaultBulkCopyOptions(context) with { MaxBatchSize = 5 };
				await table.BulkCopyAsync(options, AsyncEnumerableData(20, 1));

				options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };
				await table.BulkCopyAsync(options, AsyncEnumerableData(30, 1));
			}
		}

#if NET6_0_OR_GREATER
		[Test]
		public void BulkCopyDateOnly(
			[DataSources(false)] string context,
			[Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			using var db    = new DataConnection(context);
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };
			using var table = db.CreateLocalTable<DateOnlyTable>();
			
			db.DataProvider.BulkCopy(
				db.Options.WithOptions(options), 
				table, 
				new[] { new DateOnlyTable() { Date = new DateOnly(2021, 1, 1) } });
			
			Assert.That(table.Single().Date, Is.EqualTo(new DateOnly(2021, 1, 1)));
		}

		[Test]
		public void BulkCopyDateOnlyArrayBound(
			[IncludeDataSources(TestProvName.AllOracle)] string context
		)
		{
			// This makes use of array-bound parameters, which is a unique code-path in OracleBulkCopy (issue #4385)
			using var mode  = new OracleAlternativeBulkCopyMode(AlternativeBulkCopy.InsertInto);
			using var db    = new DataConnection(context);
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.MultipleRows };
			using var table = db.CreateLocalTable<DateOnlyTable>();
			
			db.DataProvider.BulkCopy(
				db.Options.WithOptions(options), 
				table, 
				new[] { new DateOnlyTable() { Date = new DateOnly(2021, 1, 1) } });
			
			Assert.That(table.Single().Date, Is.EqualTo(new DateOnly(2021, 1, 1)));
		}
#endif

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		private async IAsyncEnumerable<SimpleBulkCopyTable> AsyncEnumerableData(int start, int count)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			for (var i = 0; i < count; i++)
				yield return new SimpleBulkCopyTable() { Id = start + i };
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		private async IAsyncEnumerable<IdentitySimpleBulkCopyTable> IdentityAsyncEnumerableData(int start, int count)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			for (var i = 0; i < count; i++)
				yield return new IdentitySimpleBulkCopyTable() { Id = start + i };
		}

		#region CloseAfterUse
		private sealed class TestDataContextInterceptor : DataContextInterceptor
		{
			public int Closed      { get; private set; }
			public int ClosedAsync { get; private set; }
			public int ClosedCtx      { get; private set; }
			public int ClosedAsyncCtx { get; private set; }

			public override void OnClosed(DataContextEventData eventData)
			{
				if (eventData.Context is DataConnection)
					Closed++;
				else
					ClosedCtx++;
			}

			public override Task OnClosedAsync(DataContextEventData eventData)
			{
				if (eventData.Context is DataConnection)
					ClosedAsync++;
				else
					ClosedAsyncCtx++;

				return Task.CompletedTask;
			}
		}

		[Test]
		public void BulkCopyWithCloseAfterUseDataContext(
			[DataSources(false)] string context,
			[Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataContext(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };

			((IDataContext)db).Close();
			DataProvider.MySqlTests.EnableNativeBulk(db.GetDataConnection(), context);
			db.CloseAfterUse = true;
			db.AddInterceptor(interceptor);
			db.DataProvider.BulkCopy(db.Options.WithOptions(options), table, new[] { new SimpleBulkCopyTable() { Id = 1 } });

			if (context.IsAnyOf(ProviderName.ClickHouseClient) && copyType is BulkCopyType.ProviderSpecific)
			{
				// provider-specific bulk copy doesn't support sync API
				Assert.That(interceptor.Closed        , Is.EqualTo(0));
				Assert.That(interceptor.ClosedAsync   , Is.EqualTo(1));
				Assert.That(interceptor.ClosedCtx     , Is.EqualTo(0));
				Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(1).Or.EqualTo(0));
			}
			else
			{
				Assert.That(interceptor.Closed        , Is.EqualTo(1));
				Assert.That(interceptor.ClosedAsync   , Is.EqualTo(0));
				Assert.That(interceptor.ClosedCtx     , Is.EqualTo(1).Or.EqualTo(0));
				Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(0));
			}
		}

		[Test]
		public async Task BulkCopyAsyncWithCloseAfterUseDataContext(
			[DataSources(false)] string context,
			[Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataContext(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };

			((IDataContext)db).Close();
			DataProvider.MySqlTests.EnableNativeBulk(db.GetDataConnection(), context);
			db.CloseAfterUse = true;
			db.AddInterceptor(interceptor);
			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, new[] { new SimpleBulkCopyTable() { Id = 1 } }, default);

			if (context.IsAnyOf(TestProvName.AllInformix, ProviderName.DB2, ProviderName.Sybase, TestProvName.AllOracle)
				&& copyType is BulkCopyType.ProviderSpecific)
			{
				// provider-specific bulk copy doesn't support async API
				Assert.That(interceptor.Closed        , Is.EqualTo(1));
				Assert.That(interceptor.ClosedAsync   , Is.EqualTo(0));
				Assert.That(interceptor.ClosedCtx     , Is.EqualTo(1).Or.EqualTo(0));
				Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(0));
			}
			else
			{
				Assert.That(interceptor.Closed        , Is.EqualTo(0));
				Assert.That(interceptor.ClosedAsync   , Is.EqualTo(1));
				Assert.That(interceptor.ClosedCtx     , Is.EqualTo(0));
				Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(1).Or.EqualTo(0));
			}
		}

		[Test]
		public async Task BulkCopyAsyncEnumerableWithCloseAfterUseDataContext(
			[DataSources(false)] string context,
			[Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataContext(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };

			((IDataContext)db).Close();
			DataProvider.MySqlTests.EnableNativeBulk(db.GetDataConnection(), context);
			db.CloseAfterUse = true;
			db.AddInterceptor(interceptor);
			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, AsyncEnumerableData(2, 1), default);

			if (context.IsAnyOf(TestProvName.AllInformix, ProviderName.DB2, ProviderName.Sybase, TestProvName.AllOracle)
				&& copyType is BulkCopyType.ProviderSpecific)
			{
				// provider-specific bulk copy doesn't support async API
				Assert.That(interceptor.Closed        , Is.EqualTo(1));
				Assert.That(interceptor.ClosedAsync   , Is.EqualTo(0));
				Assert.That(interceptor.ClosedCtx     , Is.EqualTo(1).Or.EqualTo(0));
				Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(0));
			}
			else
			{
				Assert.That(interceptor.Closed        , Is.EqualTo(0));
				Assert.That(interceptor.ClosedAsync   , Is.EqualTo(1));
				Assert.That(interceptor.ClosedCtx     , Is.EqualTo(0));
				Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(1).Or.EqualTo(0));
			}
		}

		[Test]
		public void BulkCopyWithCloseAfterUseDataConnection(
			[DataSources(false)] string context,
			[Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataConnection(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };

			((IDataContext)db).Close();
			DataProvider.MySqlTests.EnableNativeBulk(db, context);
			((IDataContext)db).CloseAfterUse = true;
			db.AddInterceptor(interceptor);

			db.DataProvider.BulkCopy(db.Options.WithOptions(options), table, new[] { new SimpleBulkCopyTable() { Id = 1 } });

			if (context.IsAnyOf(ProviderName.ClickHouseClient) && copyType is BulkCopyType.ProviderSpecific)
			{
				// provider-specific bulk copy doesn't support sync API
				Assert.That(interceptor.Closed     , Is.EqualTo(0));
				Assert.That(interceptor.ClosedAsync, Is.EqualTo(1));
			}
			else
			{
				Assert.That(interceptor.Closed     , Is.EqualTo(1));
				Assert.That(interceptor.ClosedAsync, Is.EqualTo(0));
			}
		}

		[Test]
		public async Task BulkCopyAsyncWithCloseAfterUseDataConnection(
			[DataSources(false)] string context,
			[Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataConnection(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };

			((IDataContext)db).Close();
			DataProvider.MySqlTests.EnableNativeBulk(db, context);
			((IDataContext)db).CloseAfterUse = true;
			db.AddInterceptor(interceptor);

			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, new[] { new SimpleBulkCopyTable() { Id = 1 } }, default);

			if (context.IsAnyOf(TestProvName.AllInformix, ProviderName.DB2, ProviderName.Sybase, TestProvName.AllOracle)
				&& copyType is BulkCopyType.ProviderSpecific)
			{
				// provider-specific bulk copy doesn't support async API
				Assert.That(interceptor.Closed     , Is.EqualTo(1));
				Assert.That(interceptor.ClosedAsync, Is.EqualTo(0));
			}
			else
			{
				Assert.That(interceptor.Closed     , Is.EqualTo(0));
				Assert.That(interceptor.ClosedAsync, Is.EqualTo(1));
			}
		}

		[Test]
		public async Task BulkCopyAsyncEnumerableWithCloseAfterUseDataConnection(
			[DataSources(false)] string context,
			[Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataConnection(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };

			((IDataContext)db).Close();
			DataProvider.MySqlTests.EnableNativeBulk(db, context);
			((IDataContext)db).CloseAfterUse = true;
			db.AddInterceptor(interceptor);

			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, AsyncEnumerableData(2, 1), default);

			if (context.IsAnyOf(TestProvName.AllInformix, ProviderName.DB2, ProviderName.Sybase, TestProvName.AllOracle)
				&& copyType is BulkCopyType.ProviderSpecific)
			{
				// provider-specific bulk copy doesn't support async API
				Assert.That(interceptor.Closed     , Is.EqualTo(1));
				Assert.That(interceptor.ClosedAsync, Is.EqualTo(0));
			}
			else
			{
				Assert.That(interceptor.Closed     , Is.EqualTo(0));
				Assert.That(interceptor.ClosedAsync, Is.EqualTo(1));
			}
		}

		[Test]
		public void BulkCopyWithCloseAfterUseDataContextKeepIdentity(
			[IncludeDataSources(TestProvName.AllSqlServer, ProviderName.SqlCe)] string context,
			[Values(BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataContext(context);
			using var table = db.CreateLocalTable<IdentitySimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType, KeepIdentity = true };

			((IDataContext)db).Close();
			db.CloseAfterUse = true;
			db.AddInterceptor(interceptor);
			db.DataProvider.BulkCopy(db.Options.WithOptions(options), table, new[] { new IdentitySimpleBulkCopyTable() { Id = 1 } });

			Assert.That(interceptor.Closed        , Is.EqualTo(1));
			Assert.That(interceptor.ClosedAsync   , Is.EqualTo(0));
			Assert.That(interceptor.ClosedCtx     , Is.EqualTo(1).Or.EqualTo(0));
			Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(0));
		}

		[Test]
		public async Task BulkCopyAsyncWithCloseAfterUseDataContextKeepIdentity(
			[IncludeDataSources(TestProvName.AllSqlServer, ProviderName.SqlCe)] string context,
			[Values(BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataContext(context);
			using var table = db.CreateLocalTable<IdentitySimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType, KeepIdentity = true };

			((IDataContext)db).Close();
			db.CloseAfterUse = true;
			db.AddInterceptor(interceptor);
			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, new[] { new IdentitySimpleBulkCopyTable() { Id = 1 } }, default);

			Assert.That(interceptor.Closed        , Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsync   , Is.EqualTo(1));
			Assert.That(interceptor.ClosedCtx     , Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(1).Or.EqualTo(0));
		}

		[Test]
		public async Task BulkCopyAsyncEnumerableWithCloseAfterUseDataContextKeepIdentity(
			[IncludeDataSources(TestProvName.AllSqlServer, ProviderName.SqlCe)] string context,
			[Values(BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataContext(context);
			using var table = db.CreateLocalTable<IdentitySimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType, KeepIdentity = true };

			((IDataContext)db).Close();
			db.CloseAfterUse = true;
			db.AddInterceptor(interceptor);
			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, IdentityAsyncEnumerableData(2, 1), default);

			Assert.That(interceptor.Closed        , Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsync   , Is.EqualTo(1));
			Assert.That(interceptor.ClosedCtx     , Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(1).Or.EqualTo(0));
		}

		[Test]
		public void BulkCopyWithCloseAfterUseDataConnectionKeepIdentity(
			[IncludeDataSources(TestProvName.AllSqlServer, ProviderName.SqlCe)] string context,
			[Values(BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataConnection(context);
			using var table = db.CreateLocalTable<IdentitySimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType, KeepIdentity = true };

			((IDataContext)db).Close();
			((IDataContext)db).CloseAfterUse = true;
			db.AddInterceptor(interceptor);

			db.DataProvider.BulkCopy(db.Options.WithOptions(options), table, new[] { new IdentitySimpleBulkCopyTable() { Id = 1 } });

			Assert.That(interceptor.Closed     , Is.EqualTo(1));
			Assert.That(interceptor.ClosedAsync, Is.EqualTo(0));
		}

		[Test]
		public async Task BulkCopyAsyncWithCloseAfterUseDataConnectionKeepIdentity(
			[IncludeDataSources(TestProvName.AllSqlServer, ProviderName.SqlCe)] string context,
			[Values(BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataConnection(context);
			using var table = db.CreateLocalTable<IdentitySimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType, KeepIdentity = true };

			((IDataContext)db).Close();
			((IDataContext)db).CloseAfterUse = true;
			db.AddInterceptor(interceptor);

			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, new[] { new IdentitySimpleBulkCopyTable() { Id = 1 } }, default);

			Assert.That(interceptor.Closed     , Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsync, Is.EqualTo(1));
		}

		[Test]
		public async Task BulkCopyAsyncEnumerableWithCloseAfterUseDataConnectionKeepIdentity(
			[IncludeDataSources(TestProvName.AllSqlServer, ProviderName.SqlCe)] string context,
			[Values(BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataConnection(context);
			using var table = db.CreateLocalTable<IdentitySimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType, KeepIdentity = true };

			((IDataContext)db).Close();
			((IDataContext)db).CloseAfterUse = true;
			db.AddInterceptor(interceptor);

			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, IdentityAsyncEnumerableData(2, 1), default);

			Assert.That(interceptor.Closed     , Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsync, Is.EqualTo(1));
		}

		[Test]
		public void BulkCopyWithCloseAfterUseDataContextOracleAlternative(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(AlternativeBulkCopy.InsertDual, AlternativeBulkCopy.InsertInto)] AlternativeBulkCopy alternateCopyType)
		{
			using var mode  = new OracleAlternativeBulkCopyMode(alternateCopyType);
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataContext(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.MultipleRows };

			((IDataContext)db).Close();
			db.CloseAfterUse = true;
			db.AddInterceptor(interceptor);
			db.DataProvider.BulkCopy(db.Options.WithOptions(options), table, new[] { new SimpleBulkCopyTable() { Id = 1 } });

			Assert.That(interceptor.Closed, Is.EqualTo(1));
			Assert.That(interceptor.ClosedAsync, Is.EqualTo(0));
			Assert.That(interceptor.ClosedCtx, Is.EqualTo(1).Or.EqualTo(0));
			Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(0));
		}

		[Test]
		public async Task BulkCopyAsyncWithCloseAfterUseDataContextOracleAlternative(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(AlternativeBulkCopy.InsertDual, AlternativeBulkCopy.InsertInto)] AlternativeBulkCopy alternateCopyType)
		{
			using var mode  = new OracleAlternativeBulkCopyMode(alternateCopyType);
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataContext(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.MultipleRows };

			((IDataContext)db).Close();
			db.CloseAfterUse = true;
			db.AddInterceptor(interceptor);
			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, new[] { new SimpleBulkCopyTable() { Id = 1 } }, default);

			Assert.That(interceptor.Closed, Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsync, Is.EqualTo(1));
			Assert.That(interceptor.ClosedCtx, Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(1).Or.EqualTo(0));
		}

		[Test]
		public async Task BulkCopyAsyncEnumerableWithCloseAfterUseDataContextOracleAlternative(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(AlternativeBulkCopy.InsertDual, AlternativeBulkCopy.InsertInto)] AlternativeBulkCopy alternateCopyType)
		{
			using var mode  = new OracleAlternativeBulkCopyMode(alternateCopyType);
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataContext(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.MultipleRows };

			((IDataContext)db).Close();
			db.CloseAfterUse = true;
			db.AddInterceptor(interceptor);
			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, AsyncEnumerableData(2, 1), default);

			Assert.That(interceptor.Closed, Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsync, Is.EqualTo(1));
			Assert.That(interceptor.ClosedCtx, Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsyncCtx, Is.EqualTo(1).Or.EqualTo(0));
		}

		[Test]
		public void BulkCopyWithCloseAfterUseDataConnectionOracleAlternative(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(AlternativeBulkCopy.InsertDual, AlternativeBulkCopy.InsertInto)] AlternativeBulkCopy alternateCopyType)
		{
			using var mode  = new OracleAlternativeBulkCopyMode(alternateCopyType);
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataConnection(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.MultipleRows };

			((IDataContext)db).Close();
			((IDataContext)db).CloseAfterUse = true;
			db.AddInterceptor(interceptor);

			db.DataProvider.BulkCopy(db.Options.WithOptions(options), table, new[] { new SimpleBulkCopyTable() { Id = 1 } });

			Assert.That(interceptor.Closed, Is.EqualTo(1));
			Assert.That(interceptor.ClosedAsync, Is.EqualTo(0));
		}

		[Test]
		public async Task BulkCopyAsyncWithCloseAfterUseDataConnectionOracleAlternative(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(AlternativeBulkCopy.InsertDual, AlternativeBulkCopy.InsertInto)] AlternativeBulkCopy alternateCopyType)
		{
			using var mode  = new OracleAlternativeBulkCopyMode(alternateCopyType);
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataConnection(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.MultipleRows };

			((IDataContext)db).Close();
			((IDataContext)db).CloseAfterUse = true;
			db.AddInterceptor(interceptor);

			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, new[] { new SimpleBulkCopyTable() { Id = 1 } }, default);

			Assert.That(interceptor.Closed, Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsync, Is.EqualTo(1));
		}

		[Test]
		public async Task BulkCopyAsyncEnumerableWithCloseAfterUseDataConnectionOracleAlternative(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(AlternativeBulkCopy.InsertDual, AlternativeBulkCopy.InsertInto)] AlternativeBulkCopy alternateCopyType)
		{
			using var mode  = new OracleAlternativeBulkCopyMode(alternateCopyType);
			var interceptor = new TestDataContextInterceptor();
			using var db    = new DataConnection(context);
			using var table = db.CreateLocalTable<SimpleBulkCopyTable>();
			var options     = GetDefaultBulkCopyOptions(context) with { BulkCopyType = BulkCopyType.MultipleRows };

			((IDataContext)db).Close();
			((IDataContext)db).CloseAfterUse = true;
			db.AddInterceptor(interceptor);

			await db.DataProvider.BulkCopyAsync(db.Options.WithOptions(options), table, AsyncEnumerableData(2, 1), default);

			Assert.That(interceptor.Closed, Is.EqualTo(0));
			Assert.That(interceptor.ClosedAsync, Is.EqualTo(1));
		}
		#endregion

		[Table("TPHTable")]
		[InheritanceMapping(Code = 1, Type = typeof(Inherited1))]
		[InheritanceMapping(Code = 2, Type = typeof(Inherited2))]
		[InheritanceMapping(Code = 3, Type = typeof(Inherited3))]
		abstract class BaseClass
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public abstract int Discriminator { get; }
		}

		sealed class Inherited1 : BaseClass
		{
			public override int Discriminator => 1;

			[Column(Length = 50)]
			public string? Value1 { get; set; }
		}

		sealed class Inherited2 : BaseClass
		{
			public override int Discriminator => 2;

			[Column(Length = 50)]
			public string? Value2 { get; set; }
		}

		sealed class Inherited3 : BaseClass
		{
			public override int Discriminator => 3;

			[Column(Length = 50)]
			public string? Value3 { get; set; }

			public bool? NullableBool { get; set; }
		}

		[Test]
		public void BulkCopyTPH(
			[DataSources(false)] string context,
			[Values] BulkCopyType copyType)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<Inherited3>()
				.Property(e => e.NullableBool)
				.HasDataType(DataType.VarChar)
				.HasLength(1)
				.HasConversion(b => b.HasValue ? b.Value ? "Y" : "N" : null, s => s != null ? s == "Y" : null, true)
				.Build();

			var data = new BaseClass[]
			{
				new Inherited1 { Id = 1, Value1 = "Str1" },
				new Inherited2 { Id = 2, Value2 = "Str2" },
				new Inherited3 { Id = 3, Value3 = "Str3", NullableBool = true },
			};

			using (var db = new DataConnection(context, ms))
			using (var table = db.CreateLocalTable<BaseClass>())
			{
				var options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };
				table.BulkCopy(options, data);

				var items = table.OrderBy(_ => _.Id).ToArray();

				items[0].Id.Should().Be(1);
				items[0].Discriminator.Should().Be(1);
				((Inherited1)items[0]).Value1.Should().Be("Str1");

				items[1].Id.Should().Be(2);
				items[1].Discriminator.Should().Be(2);
				((Inherited2)items[1]).Value2.Should().Be("Str2");

				items[2].Id.Should().Be(3);
				items[2].Discriminator.Should().Be(3);
				((Inherited3)items[2]).Value3.Should().Be("Str3");

				table.Single(x => x is Inherited1).Should().BeOfType(typeof(Inherited1));
				table.Single(x => x is Inherited2).Should().BeOfType(typeof(Inherited2));
				table.Single(x => x is Inherited3).Should().BeOfType(typeof(Inherited3));

				table.Single(x => ((Inherited1)x).Value1 == "Str1").Should().BeOfType(typeof(Inherited1));
				table.Single(x => ((Inherited2)x).Value2 == "Str2").Should().BeOfType(typeof(Inherited2));
				table.Single(x => ((Inherited3)x).Value3 == "Str3").Should().BeOfType(typeof(Inherited3));
			}
		}

		[Table("TPHTableDefault")]
		[InheritanceMapping(Code = 1, Type = typeof(InheritedDefault1))]
		[InheritanceMapping(Code = 2, Type = typeof(InheritedDefault2))]
		[InheritanceMapping(Code = 3, Type = typeof(InheritedDefault3))]
		abstract class BaseDefaultDiscriminator
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public int Discriminator { get; set; }
		}

		sealed class InheritedDefault1 : BaseDefaultDiscriminator
		{
			[Column(Length = 50)]
			public string? Value1 { get; set; }
		}

		sealed class InheritedDefault2 : BaseDefaultDiscriminator
		{
			[Column(Length = 50)]
			public string? Value2 { get; set; }
		}

		sealed class InheritedDefault3 : BaseDefaultDiscriminator
		{
			[Column(Length = 50)]
			public string? Value3 { get; set; }
		}

		[Test]
		public void BulkCopyTPHDefault(
			[IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context,
			[Values] BulkCopyType copyType)
		{
			var data = new BaseDefaultDiscriminator[]
			{
				new InheritedDefault1 { Id = 1, Value1 = "Str1" },
				new InheritedDefault2 { Id = 2, Value2 = "Str2" },
				new InheritedDefault3 { Id = 3, Value3 = "Str3" },
			};

			using (var db = new DataConnection(context))
			using (var table = db.CreateLocalTable<BaseDefaultDiscriminator>())
			{
				var options = GetDefaultBulkCopyOptions(context) with { BulkCopyType = copyType };
				table.BulkCopy(options, data);

				var items = table.OrderBy(_ => _.Id).ToArray();

				items[0].Id.Should().Be(1);
				items[0].Discriminator.Should().Be(1);
				((InheritedDefault1)items[0]).Value1.Should().Be("Str1");

				items[1].Id.Should().Be(2);
				items[1].Discriminator.Should().Be(2);
				((InheritedDefault2)items[1]).Value2.Should().Be("Str2");

				items[2].Id.Should().Be(3);
				items[2].Discriminator.Should().Be(3);
				((InheritedDefault3)items[2]).Value3.Should().Be("Str3");

				table.Single(x => x is InheritedDefault1).Should().BeOfType(typeof(InheritedDefault1));
				table.Single(x => x is InheritedDefault2).Should().BeOfType(typeof(InheritedDefault2));
				table.Single(x => x is InheritedDefault3).Should().BeOfType(typeof(InheritedDefault3));

				table.Single(x => ((InheritedDefault1)x).Value1 == "Str1").Should().BeOfType(typeof(InheritedDefault1));
				table.Single(x => ((InheritedDefault2)x).Value2 == "Str2").Should().BeOfType(typeof(InheritedDefault2));
				table.Single(x => ((InheritedDefault3)x).Value3 == "Str3").Should().BeOfType(typeof(InheritedDefault3));
			}
		}
	}
}
