using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using LinqToDB.DataProvider.Informix;
	using Model;

	[TestFixture]
	[Order(10000)]
	public class BulkCopyTests : TestBase
	{
		// TODO: update Sybase.sql to use proper type for identity. not it uses INT for most of tables, which
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
		public void KeepIdentity_SkipOnInsertTrue(
			[DataSources(false)]string context,
			[Values(null, true, false)]bool? keepIdentity,
			[Values] BulkCopyType copyType)
		{
			if (context == ProviderName.OracleNative && copyType == BulkCopyType.ProviderSpecific)
				Assert.Inconclusive("Oracle BulkCopy doesn't support identity triggers");

			List<TestTable1> list = null;

			// don't use transactions as some providers will fallback to non-provider-specific implementation then
			using (var db = new TestDataConnection(context))
			//using (db.BeginTransaction())
			{
				var lastId = db.InsertWithInt32Identity(new TestTable2());
				try
				{
					list = db.GetTable<TestTable1>().ToList();
					var options = new BulkCopyOptions()
					{
						KeepIdentity = keepIdentity,
						BulkCopyType = copyType
					};

					if (!Execute(db, context, perform, keepIdentity, copyType))
						return;

					var data = db.GetTable<TestTable2>().Where(_ => _.ID > lastId).OrderBy(_ => _.ID).ToArray();

					Assert.AreEqual(2, data.Length);

					// oracle supports identity insert only starting from version 12c, which is not used yet for tests
					var useGenerated = keepIdentity != true
						|| context == ProviderName.OracleNative
						|| context == ProviderName.OracleManaged;

					Assert.AreEqual(lastId + (!useGenerated ? 10 : 1), data[0].ID);
					Assert.AreEqual(200, data[0].Value);
					Assert.AreEqual(lastId + (!useGenerated ? 20 : 2), data[1].ID);
					Assert.AreEqual(300, data[1].Value);

					void perform()
					{
						db.BulkCopy(
							options,
							new[]
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
							});
					}
				}
				finally
				{
					// cleanup
					db.GetTable<TestTable2>().Delete(_ => _.ID >= lastId);
					if (list != null)
						foreach (var item in list)
							db.Insert(item);
				}
			}
		}

		//[ActiveIssue("Sybase: Bulk insert failed. Null value is not allowed in not null column.", Configuration = ProviderName.Sybase)]
		[ActiveIssue("Unsupported column datatype for BulkCopyType.ProviderSpecific", Configurations = new[] { ProviderName.OracleNative , ProviderName.Sybase } )]
		[Test]
		public void KeepIdentity_SkipOnInsertFalse(
			[DataSources(false)]string context,
			[Values(null, true, false)]bool? keepIdentity,
			[Values] BulkCopyType copyType)
		{
			List<TestTable1> list = null;

			// don't use transactions as some providers will fallback to non-provider-specific implementation then
			using (var db = new TestDataConnection(context))
			//using (db.BeginTransaction())
			{
				var lastId = db.InsertWithInt32Identity(new TestTable1());
				try
				{
					list = db.GetTable<TestTable1>().ToList();
					db.GetTable<TestTable1>().Delete();

					var options = new BulkCopyOptions()
					{
						KeepIdentity = keepIdentity,
						BulkCopyType = copyType
					};

					if (!Execute(db, context, perform, keepIdentity, copyType))
						return;

					var data = db.GetTable<TestTable1>().Where(_ => _.ID > lastId).OrderBy(_ => _.ID).ToArray();

					Assert.AreEqual(2, data.Length);

					// oracle supports identity insert only starting from version 12c, which is not used yet for tests
					var useGenerated = keepIdentity != true
						|| context == ProviderName.OracleNative
						|| context == ProviderName.OracleManaged;

					Assert.AreEqual(lastId + (!useGenerated ? 10 : 1), data[0].ID);
					Assert.AreEqual(200, data[0].Value);
					Assert.AreEqual(lastId + (!useGenerated ? 20 : 2), data[1].ID);
					Assert.AreEqual(300, data[1].Value);

					void perform()
					{
						db.BulkCopy(
							options,
							new[]
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
							});
					}
				}
				finally
				{
					// cleanup
					db.GetTable<TestTable2>().Delete(_ => _.ID >= lastId);
					if (list != null)
						foreach (var item in list)
							db.Insert(item);
				}
			}
		}

		private bool Execute(DataConnection db, string context, Action perform, bool? keepIdentity, BulkCopyType copyType)
		{
			if ((context == ProviderName.Firebird || context == TestProvName.Firebird3)
				&& keepIdentity == true
				&& (copyType    == BulkCopyType.Default
					|| copyType == BulkCopyType.MultipleRows
					|| copyType == BulkCopyType.ProviderSpecific))
			{
				var ex = Assert.Catch(() => perform());
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
					|| notSupported
					|| (context.StartsWith(ProviderName.SapHana)
						&& (copyType == BulkCopyType.MultipleRows || copyType == BulkCopyType.Default))
					|| (context == ProviderName.SapHanaOdbc && copyType == BulkCopyType.ProviderSpecific))
				&& keepIdentity == true)
			{
				var ex = Assert.Catch(() => perform());
				Assert.IsInstanceOf<LinqToDBException>(ex);
				Assert.AreEqual("BulkCopyOptions.KeepIdentity = true is not supported by BulkCopyType.RowByRow mode", ex.Message);
				return false;
			}

			perform();
			return true;
		}

		[Test]
		public void ReuseOptionTest([DataSources(false)]string context)
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
