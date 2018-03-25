using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using Tests.Model;

namespace Tests.xUpdate
{

	[TestFixture]
	public class BulkCopyTests : TestBase
	{
		[Table("KeepIdentityTest", Configuration = ProviderName.DB2)]
		[Table("alltypes", Configuration = ProviderName.PostgreSQL)]
		[Table("AllTypes")]
		public class TestTable1
		{
			[Identity]
			[Column("id", Configuration = ProviderName.PostgreSQL)]
			public int ID { get; set; }

			[Column("intDataType")]
			[Column("Value", Configuration = ProviderName.DB2)]
			[Column("intdatatype", Configuration = ProviderName.PostgreSQL)]
			public int Value { get; set; }

			[Column("bitDataType", Configuration = ProviderName.Sybase)]
			public bool ThisIsSYBASE;
		}

		[Table("KeepIdentityTest", Configuration = ProviderName.DB2)]
		[Table("alltypes", Configuration = ProviderName.PostgreSQL)]
		[Table("AllTypes")]
		public class TestTable2
		{
			[Identity, Column(SkipOnInsert = true)]
			[Column("id", Configuration = ProviderName.PostgreSQL)]
			public int ID { get; set; }

			[Column("intDataType")]
			[Column("Value", Configuration = ProviderName.DB2)]
			[Column("intdatatype", Configuration = ProviderName.PostgreSQL)]
			public int Value { get; set; }

			[Column("bitDataType", Configuration = ProviderName.Sybase)]
			public bool ThisIsSYBASE;
		}

		[Test, Combinatorial]
		public void KeepIdentity_SkipOnInsertTrue(
			[DataSources(false)]string context,
			[Values(null, true, false)]bool? keepIdentity,
			[Values] BulkCopyType copyType)
		{
			// don't use transactions as some providers will fallback to non-provider-specific implementation then
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

					if (!Execute(context, perform, keepIdentity, copyType))
						return;

					var data = db.GetTable<TestTable2>().Where(_ => _.ID > lastId).OrderBy(_ => _.ID).ToArray();

					Assert.AreEqual(2, data.Length);

					// oracle supports identity insert only starting from version 12c, which is not used yet for tests
					var useGenerated = keepIdentity != true
						|| context == ProviderName.Oracle
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
				}
			}
		}

		[Test, Combinatorial]
		public void KeepIdentity_SkipOnInsertFalse(
			[DataSources(false)]string context,
			[Values(null, true, false)]bool? keepIdentity,
			[Values] BulkCopyType copyType)
		{
			// don't use transactions as some providers will fallback to non-provider-specific implementation then
			using (var db = new TestDataConnection(context))
			{
				var lastId = db.InsertWithInt32Identity(new TestTable1());
				try
				{
					db.GetTable<TestTable1>().Delete();

					var options = new BulkCopyOptions()
					{
						KeepIdentity = keepIdentity,
						BulkCopyType = copyType
					};

					if (!Execute(context, perform, keepIdentity, copyType))
						return;

					var data = db.GetTable<TestTable1>().Where(_ => _.ID > lastId).OrderBy(_ => _.ID).ToArray();

					Assert.AreEqual(2, data.Length);

					// oracle supports identity insert only starting from version 12c, which is not used yet for tests
					var useGenerated = keepIdentity != true
						|| context == ProviderName.Oracle
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
				}
			}
		}

		private bool Execute(string context, Action perform, bool? keepIdentity, BulkCopyType copyType)
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

			// RowByRow right now uses DataConnection.Insert which doesn't support identity insert
			if ((copyType       == BulkCopyType.RowByRow
					|| context  == ProviderName.Access
					|| context  == ProviderName.Informix
					|| (context == ProviderName.SapHana
						&& (copyType == BulkCopyType.MultipleRows || copyType == BulkCopyType.Default)))
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
	}
}
