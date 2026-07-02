#nullable disable
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5666Tests : TestBase
	{
		enum Enum1
		{
			Ok,
			Fail,
		}

		class ReceiptBaseTest
		{
			[Column("RECEIPT_NO")]  public string ReceiptNo  { get; set; }
			[Column("SERV_CATG")]   public Enum1  Status     { get; set; }
			[Column("PROPERTY_ID")] public int?   PropertyId { get; set; }

			[Association(ThisKey = nameof(PropertyId), OtherKey = nameof(EnumTest.Id))]
			public EnumTest EnumTest { get; }
		}

		class EnumTest
		{
			public int Id { get; set; }
		}

		class EnumNullTable
		{
			[Column] public int    Id     { get; set; }
			[Column] public Enum1? Status { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5666")]
		public void Issue5666Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db        = GetDataContext(context);
			using var receipts  = db.CreateLocalTable(new[]
			{
				new ReceiptBaseTest { ReceiptNo = "R1", Status = Enum1.Ok,   PropertyId = 100 },
				new ReceiptBaseTest { ReceiptNo = "R2", Status = Enum1.Fail, PropertyId = 100 },
				new ReceiptBaseTest { ReceiptNo = "R3", Status = Enum1.Ok,   PropertyId = 999 },
			});
			using var lookup    = db.CreateLocalTable(new[]
			{
				new EnumTest { Id = 100 },
			});

			var query =
				from i in db.GetTable<ReceiptBaseTest>()
				where i.EnumTest.Id == 100
				select new { i.ReceiptNo };

			// R1 (Status = Ok = 0) and R2 (Status = Fail) both reference Property 100, so both must
			// be returned. The bug injects "SERV_CATG <> 0" into the join, silently dropping R1.
			var result = query.ToList();
			Assert.That(result.Select(x => x.ReceiptNo), Is.EquivalentTo(new[] { "R1", "R2" }));

			// The non-nullable enum column is never referenced by the query; it must not leak into the SQL.
			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Not.Contain("SERV_CATG"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5666")]
		public void Issue5666NullComparison([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool notEqual)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new[]
			{
				new EnumNullTable { Id = 1, Status = Enum1.Ok }, // nullable-enum vs null goes through the generic null path (IS [NOT] NULL); the "<> 0" regression itself is guarded by Issue5666Test
				new EnumNullTable { Id = 2, Status = null },
			});

			var query = notEqual
				? db.GetTable<EnumNullTable>().Where(x => x.Status != null)
				: db.GetTable<EnumNullTable>().Where(x => x.Status == null);

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Contain(notEqual ? "IS NOT NULL" : "IS NULL"));

			var result = query.Select(x => x.Id).ToList();
			Assert.That(result, Is.EquivalentTo(notEqual ? new[] { 1 } : new[] { 2 }));
		}
	}
}
