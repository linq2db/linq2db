using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System.Linq;
using Tests.Model;

namespace Tests.xUpdate
{

	[TestFixture]
	public class BulkCopyTests : TestBase
	{
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		[Table("alltypes", Configuration = ProviderName.PostgreSQL)]
		[Table("AllTypes")]
		public class TestTable1
		{
			[Identity]
			public int ID { get; set; }

			[Column("intDataType")]
			[Column("INTDATATYPE", Configuration = ProviderName.DB2)]
			[Column("intdatatype", Configuration = ProviderName.PostgreSQL)]
			public int Value { get; set; }

			[Column("bitDataType", Configuration = ProviderName.Sybase)]
			public bool ThisIsSYBASE;
		}

		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		[Table("alltypes", Configuration = ProviderName.PostgreSQL)]
		[Table("AllTypes")]
		public class TestTable2
		{
			[Identity, Column(SkipOnInsert = true)]
			public int ID { get; set; }

			[Column("intDataType")]
			[Column("INTDATATYPE", Configuration = ProviderName.DB2)]
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
			using (var db = new TestDataConnection(context))

			using (db.BeginTransaction())
			{
				var lastId = db.InsertWithInt32Identity(new TestTable2());
				db.GetTable<TestTable2>().Delete();

				var options = new BulkCopyOptions()
				{
					KeepIdentity = keepIdentity,
					BulkCopyType = copyType
				};

				// RowByRow right now uses DataConnection.Insert which doesn't support identity insert
				// Access provider use RowByRow mode allways
				if ((copyType == BulkCopyType.RowByRow || context == ProviderName.Access) && keepIdentity == true)
				{
					var ex = Assert.Catch(perform);
					Assert.IsInstanceOf<LinqToDBException>(ex);
					Assert.AreEqual("BulkCopyOptions.KeepIdentity = true is not supported by BulkCopyType.RowByRow mode", ex.Message);
					return;
				}

				perform();

				var data = db.GetTable<TestTable2>().OrderBy(_ => _.ID).ToArray();

				Assert.AreEqual(2, data.Length);

				Assert.AreEqual(lastId + (keepIdentity == true ? 10 : 1), data[0].ID);
				Assert.AreEqual(200, data[0].Value);
				Assert.AreEqual(lastId + (keepIdentity == true ? 20 : 2), data[1].ID);
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
		}

		[Test, Combinatorial]
		public void KeepIdentity_SkipOnInsertFalse(
			[DataSources(false)]string context,
			[Values(null, true, false)]bool? keepIdentity,
			[Values] BulkCopyType copyType)
		{
			using (var db = new TestDataConnection(context))

			using (db.BeginTransaction())
			{
				var lastId = db.InsertWithInt32Identity(new TestTable1());
				db.GetTable<TestTable1>().Delete();

				var options = new BulkCopyOptions()
				{
					KeepIdentity = keepIdentity,
					BulkCopyType = copyType
				};

				// RowByRow right now uses DataConnection.Insert which doesn't support identity insert
				// Access provider use RowByRow mode allways
				if ((copyType == BulkCopyType.RowByRow || context == ProviderName.Access) && keepIdentity == true)
				{
					var ex = Assert.Catch(perform);
					Assert.IsInstanceOf<LinqToDBException>(ex);
					Assert.AreEqual("BulkCopyOptions.KeepIdentity = true is not supported by BulkCopyType.RowByRow mode", ex.Message);
					return;
				}

				perform();

				var data = db.GetTable<TestTable1>().OrderBy(_ => _.ID).ToArray();

				Assert.AreEqual(2, data.Length);

				Assert.AreEqual(lastId + (keepIdentity == true ? 10 : 1), data[0].ID);
				Assert.AreEqual(200, data[0].Value);
				Assert.AreEqual(lastId + (keepIdentity == true ? 20 : 2), data[1].ID);
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
		}
	}
}
