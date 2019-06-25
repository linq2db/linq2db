using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class Issue1760Tests : TestBase
	{
		[Table(Schema="dbo", Name="b_table2")]
		public partial class BTable2
		{
			[Column("id"),   PrimaryKey, Identity] public int    Id   { get; set; } // int
			[Column("col1"), Nullable            ] public string Col1 { get; set; } // varchar(50)
			[Column("col2"), Nullable            ] public string Col2 { get; set; } // varchar(50)
			[Column("col3"), Nullable            ] public string Col3 { get; set; } // varchar(50)
		}

		[Table(Schema="dbo", Name="commonTable")]
		public partial class CommonTable
		{
			[Column("id"), PrimaryKey, Identity] public int Id { get; set; } // int
		}

		[Table(Schema="dbo", Name="c_table2")]
		public partial class CTable2
		{
			[Column("id"),          PrimaryKey, Identity] public int    Id        { get; set; } // int
			[Column("col1"),        Nullable            ] public string Col1      { get; set; } // varchar(50)
			[Column("c_table3_id"), Nullable            ] public int?   CTable3Id { get; set; } // int
		}

		[Table(Schema="dbo", Name="table1")]
		public partial class Table1
		{
			[Column("id"),            PrimaryKey,  Identity] public int  Id            { get; set; } // int
			[Column("id_tbl2"),          Nullable          ] public int? IdTbl2        { get; set; } // int
			[Column("id_tbl3"),          Nullable          ] public int? IdTbl3        { get; set; } // int
			[Column("commonTableId"), NotNull              ] public int  CommonTableId { get; set; } // int
			[Column("c_tb1l_Id"),        Nullable          ] public int? CTb1lId       { get; set; } // int
		}

		[Table(Schema="dbo", Name="table2")]
		public partial class Table2
		{
			[Column("id"),      PrimaryKey, Identity] public int    Id      { get; set; } // int
			[Column("textCol"), Nullable            ] public string TextCol { get; set; } // varchar(50)
			[Column("col3"),    Nullable            ] public int?   Col3    { get; set; } // int
		}

		[Table(Schema="dbo", Name="table3")]
		public partial class Table3
		{
			[Column("id"),  PrimaryKey, Identity] public int  Id  { get; set; } // int
			[Column("col"), Nullable            ] public int? Col { get; set; } // int
		}


		class SomeClass
		{
			[PrimaryKey]
			public int Id { get; set; }
		}

		class SomeOtherClass
		{
			[PrimaryKey]
			public int Id { get; set; }
		}

		[Test]
		public void OriginalTestSimplified([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<BTable2>())
			using (db.CreateLocalTable<CommonTable>())
			using (db.CreateLocalTable<CTable2>())
			using (db.CreateLocalTable<Table1>())
			using (db.CreateLocalTable<Table2>())
			using (db.CreateLocalTable<Table3>())
			{
				int id = 0;
				var part1 = db.GetTable<Table1>()
					.GroupBy(w => w.CTb1lId)
					.Select(w => new
					{
						Col3 = w.Key,
						maxCol = w.Max(s => s.CTb1lId)
					})
					.LeftJoin(db.GetTable<Table3>(),
						(allE, tbl3) => allE.maxCol == tbl3.Id,
						(allG, tbl3) => new {allG.Col3, tbl3.Col})
					.LeftJoin(db.GetTable<BTable2>(),
						(allM, btbl) => allM.Col == btbl.Id,
						(allF, btbl) => new {all = allF, btbl})
					;
				var general = db.GetTable<Table1>()
					.Where(w => w.CommonTableId == id)
					.LeftJoin(db.GetTable<Table2>(),
						(t1, bt1) => t1.CTb1lId == bt1.Id,
						(t1, bt1) => new {t1, bt1})
					.LeftJoin(part1,
						(allA, ctb) => allA.bt1.Col3 == ctb.btbl.Id,
						(allB, ctb) => new {allB.t1, allB.bt1, ctb})
					.LeftJoin(db.GetTable<CTable2>(),
						(allC, ctb2) => allC.bt1.TextCol == ctb2.Col1,
						(allD, ctb2) => new {allD.t1, allD.bt1, allD.ctb, ctb2})
					.GroupBy(s => new
					{
						s.ctb,
					})
					.Select(s => new {s.Key});
				var ss = general.ToList();

			}
		}


		[Test]
		public void OriginalTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<BTable2>())
			using (db.CreateLocalTable<CommonTable>())
			using (db.CreateLocalTable<CTable2>())
			using (db.CreateLocalTable<Table1>())
			using (db.CreateLocalTable<Table2>())
			using (db.CreateLocalTable<Table3>())
			{
				int id = 0;
				var part1 = db.GetTable<Table1>()
					.Where(w => w.CommonTableId == id)
					.InnerJoin(db.GetTable<Table2>(),
						(tbl1, tbl2) => tbl1.IdTbl2 == tbl2.Id,
						(tbl1, tbl2) => new {tbl1, tbl2})
					.Where(w => w.tbl2.Col3.HasValue)
					.InnerJoin(db.GetTable<Table3>(),
						(all, tbl3) => all.tbl1.IdTbl3 == tbl3.Id,
						(all, tbl3) => new {all.tbl1, all.tbl2, tbl3})
					.GroupBy(w => w.tbl2.Col3)
					.Select(w => new
					{
						Col3 = w.Key,
						maxCol = w.Max(s => s.tbl3.Id)
					})
					.LeftJoin(db.GetTable<Table3>(),
						(allE, tbl3) => allE.maxCol == tbl3.Id,
						(allG, tbl3) => new {allG.Col3, tbl3.Col})
					.LeftJoin(db.GetTable<BTable2>(),
						(allM, btbl) => allM.Col == btbl.Id,
						(allF, btbl) => new {all = allF, btbl})
					.Select(w => new
					{
						c1 = w.all.Col3 ?? 0,
						b1 = w.btbl.Col1 ?? string.Empty,
						b2 = w.btbl.Col2 ?? string.Empty,
						b3 = w.btbl.Col3 ?? string.Empty,
					});
				var general = db.GetTable<Table1>()
					.Where(w => w.CommonTableId == id)
					.LeftJoin(db.GetTable<Table2>(),
						(t1, bt1) => t1.CTb1lId == bt1.Id,
						(t1, bt1) => new {t1, bt1})
					.LeftJoin(part1,
						(allA, ctb) => allA.bt1.Col3 == ctb.c1,
						(allB, ctb) => new {allB.t1, allB.bt1, ctb})
					.LeftJoin(db.GetTable<CTable2>(),
						(allC, ctb2) => allC.bt1.TextCol == ctb2.Col1,
						(allD, ctb2) => new {allD.t1, allD.bt1, allD.ctb, ctb2})
					.GroupBy(s => new
					{
						s.ctb.b1,
						s.ctb.b2,
						s.ctb.b3,
						s.bt1.TextCol
					})
					.Select(s => new { s.Key.TextCol, s.Key.b1, s.Key.b2, s.Key.b3});
				var ss = general.ToList();

			}
		}
	}
}
