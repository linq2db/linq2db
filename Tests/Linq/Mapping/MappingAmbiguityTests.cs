using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.xUpdate;

namespace Tests.Mapping
{
	public class MappingAmbiguityTests : TestBase
	{
		[Table]
		public class TestTable
		{
			[PrimaryKey] public int ID { get; set; }

			// mapped field and property with same name different cases and types
			[Column]            public int     Field1 { get; set; }
			[Column("field11")] public string? field1;

			// mapped property and unmapped field with same name different cases and types
			[Column]    public int     Field2 { get; set; }
			[NotColumn] public string? field2;

			// mapped field and unmapped property with same name different cases and types
			[Column]    public int     Field3 { get; set; }
			[NotColumn] public string? field3;

			// mapped and unmapped property with same name different cases and types
			[Column]    public int     Field4 { get; set; }
			[NotColumn] public string? field4 { get; set; }

			// mapped and unmapped field with same name different cases and types
			[Column]    public int     Field5;
			[NotColumn] public string? field5;
		}

		[Test]
		public void TestCreate([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable<TestTable>())
			{
				var sql = db.LastQuery!;

				Assert.That(sql.Replace("\r", ""), Is.EqualTo(@"CREATE TABLE IF NOT EXISTS [TestTable]
(
	[ID]      INTEGER       NOT NULL,
	[Field1]  INTEGER       NOT NULL,
	[Field2]  INTEGER       NOT NULL,
	[Field3]  INTEGER       NOT NULL,
	[Field4]  INTEGER       NOT NULL,
	[field11] NVarChar(255)     NULL,
	[Field5]  INTEGER       NOT NULL,

	CONSTRAINT [PK_TestTable] PRIMARY KEY ([ID])
)
".Replace("\r", "")));
			}
		}

		[Test]
		public void TestDefaultInsertUpdateMerge([MergeTests.MergeDataContextSource(true, TestProvName.AllSybase, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<TestTable>())
			{
				var res = db.GetTable<TestTable>()
					.Merge()
					.UsingTarget()
					.OnTargetKey()
					.InsertWhenNotMatched()
					.UpdateWhenMatched()
					.Merge();

				if (context.IsAnyOf(TestProvName.AllOracleNative))
					Assert.That(res, Is.EqualTo(-1));
				else
					Assert.That(res, Is.EqualTo(0));
			}
		}
	}
}
