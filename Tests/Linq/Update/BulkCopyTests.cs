using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using LinqToDB.DataProvider.Informix;
	using Model;
	using System.Collections.Generic;
	using System.Threading.Tasks;

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
			[Identity]
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
			[Identity, Column(SkipOnInsert = true)]
			public int ID { get; set; }

			[Column("intDataType")]
			[Column("Value", Configuration = ProviderName.DB2)]
			[Column("Value", Configuration = ProviderName.Sybase)]
			public int Value { get; set; }
		}

		[ActiveIssue("Sybase: Bulk insert failed. Null value is not allowed in not null column.", Configuration = ProviderName.Sybase)]
		[Test]
		public async Task KeepIdentity_SkipOnInsertTrue(
			[DataSources(false)]string context,
			[Values(null, true, false)]bool? keepIdentity,
			[Values] BulkCopyType copyType,
#if NET472
			[Values(0, 1)] int asyncMode) // 0 == sync, 1 == async
#else
			[Values(0, 1, 2)] int asyncMode) // 0 == sync, 1 == async, 2 == async with IAsyncEnumerable
#endif
		{
			if ((context == ProviderName.OracleNative || context == TestProvName.Oracle11Native) && copyType == BulkCopyType.ProviderSpecific)
				Assert.Inconclusive("Oracle BulkCopy doesn't support identity triggers");

			// don't use transactions as some providers will fallback to non-provider-specific implementation then
			using (new DisableBaseline("Non-stable identity values"))
			using (var db = new TestDataConnection(context))
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
						|| context.Contains("Oracle");

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
#if !NET472
							await db.BulkCopyAsync(
								options,
								AsAsyncEnumerable(values));
#endif
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

		[ActiveIssue("Unsupported column datatype for BulkCopyType.ProviderSpecific", Configurations = new[] { TestProvName.AllOracleNative , ProviderName.Sybase } )]
		[Test]
		public async Task KeepIdentity_SkipOnInsertFalse(
			[DataSources(false)]        string       context,
			[Values(null, true, false)] bool?        keepIdentity,
			[Values]                    BulkCopyType copyType,
#if NET472
			[Values(0, 1)]              int          asyncMode) // 0 == sync, 1 == async
#else
			[Values(0, 1, 2)]           int          asyncMode) // 0 == sync, 1 == async, 2 == async with IAsyncEnumerable
#endif
		{
			// don't use transactions as some providers will fallback to non-provider-specific implementation then
			using (new DisableBaseline("Non-stable identity values"))
			using (var db = new TestDataConnection(context))
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
						|| context.Contains("Oracle");

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
#if !NET472
							await db.BulkCopyAsync(
								options,
								AsAsyncEnumerable(values));
#endif
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

#if !NET472
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
#endif

		private async Task<bool> ExecuteAsync(DataConnection db, string context, Func<Task> perform, bool? keepIdentity, BulkCopyType copyType)
		{
			if ((context == ProviderName.Firebird || context == TestProvName.Firebird3)
				&& keepIdentity == true
				&& (copyType    == BulkCopyType.Default
					|| copyType == BulkCopyType.MultipleRows
					|| copyType == BulkCopyType.ProviderSpecific))
			{
				var ex = Assert.CatchAsync(async () => await perform());
				Assert.IsInstanceOf<LinqToDBException>(ex);
				Assert.AreEqual("BulkCopyOptions.KeepIdentity = true is not supported by Firebird provider. If you use generators with triggers, you should disable triggers during BulkCopy execution manually.", ex.Message);
				return false;
			}

			bool notSupported = false;
			if (context.Contains(ProviderName.Informix))
			{
				notSupported = !((InformixDataProvider)db.DataProvider).Adapter.IsIDSProvider
					|| copyType == BulkCopyType.MultipleRows;
			}

			// RowByRow right now uses DataConnection.Insert which doesn't support identity insert
			if ((copyType       == BulkCopyType.RowByRow
					|| context  == ProviderName.Access
					|| context  == ProviderName.AccessOdbc
					|| notSupported
					|| (context.StartsWith(ProviderName.SapHana)
						&& (copyType == BulkCopyType.MultipleRows || copyType == BulkCopyType.Default))
					|| (context == ProviderName.SapHanaOdbc && copyType == BulkCopyType.ProviderSpecific))
				&& keepIdentity == true)
			{
				var ex = Assert.CatchAsync(async () => await perform());
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
			using (var db = new TestDataConnection(context))
			{
				db.BeginTransaction();

				var options = new BulkCopyOptions();

				db.Parent.BulkCopy(options, new [] { new Parent { ParentID = 111001 } });
				db.Child. BulkCopy(options, new [] { new Child  { ParentID = 111001 } });
			}
		}
	}
}
