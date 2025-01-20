using System;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3342Tests : TestBase
	{
		[Table("pgtimestamptest")]
		public class PgTimestampTest
		{
			[Column("id")] public int Id { get; set; }
			[Column("updatedon")] public DateTime UpdatedOn { get; set; }
		}

		[Test]
		public void TestPgTimestamp([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();
			ms.AddScalarType(typeof(DateTimeOffset), DataType.DateTimeOffset);
			ms.AddScalarType(typeof(DateTimeOffset?), DataType.DateTimeOffset);

			using (var db = GetDataContext(context, ms))
			using (var tb = db.CreateLocalTable<PgTimestampTest>())
			{
				var dc = ((DataConnection)db);
				var sql = "ALTER TABLE pgtimestamptest ALTER COLUMN updatedon TYPE timestamptz;";
				dc.Execute(sql);

				var obj = new PgTimestampTest() { Id = 0, UpdatedOn = DateTime.UtcNow };

				var ret =
					dc.BulkCopy(new BulkCopyOptions
					{
						CheckConstraints = false,
						BulkCopyType = BulkCopyType.ProviderSpecific,
						MaxBatchSize = 5000,
						UseInternalTransaction = false,
						NotifyAfter = 2000,
						BulkCopyTimeout = 0,
						TableName = "pgtimestamptest",
					}, new [] {obj });
			}
		}
	}
}
