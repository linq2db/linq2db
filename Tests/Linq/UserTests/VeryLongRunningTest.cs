using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using NUnit.Framework;

#pragma warning disable CS8629
#pragma warning disable CS8604

namespace Tests.UserTests.VeryLongRunning
{
	static class ConstID
	{
		public const int C1 =  555;
	}

	[Table(Name="#tbl0101")]
	class Table1 : IInterface1
	{
		[Column(DataType=DataType.Int32),      PrimaryKey, Identity] public int       Column1 { get; set; } // int
		[Column(DataType=DataType.Int32),               NotNull    ] public int       Column2 { get; set; } // int
		[Column(DataType=DataType.Int32),               NotNull    ] public Enum10    Column3 { get; set; } // int
		[Column(DataType=DataType.VarChar, Length=255), NotNull    ] public string?   Column4 { get; set; } // varchar(255)
		[Column(DataType=DataType.Date),                   Nullable] public DateTime? Column5 { get; set; } // date
		[Column(DataType=DataType.Date),                   Nullable] public DateTime? Column6 { get; set; } // date
		[Column(DataType=DataType.Int32),               NotNull    ] public int       Column7 { get; set; } // int
		[Column(DataType=DataType.Boolean),             NotNull    ] public bool      Column8 { get; set; } // bit

		[ColumnAlias(nameof(Column4))]
		public Enum7? Column13
		{
			get => ConvertTo<Enum7?>.From(Column4);
			set => Column4 = ConvertTo<string>.From(value);
		}

		[ColumnAlias(nameof(Column4))]
		public Enum9? Column14
		{
			get => ConvertTo<Enum9?>.From(Column4);
			set => Column4 = ConvertTo<string>.From(value);
		}
	}

	[Table(Name="#tbl0202")]
	class Table2 : IInterface3
	{
		[Column(DataType=DataType.Int32),                PrimaryKey(1), NotNull] public int       Column1 { get; set; } // int
		[Column(DataType=DataType.Byte),                 PrimaryKey(2), NotNull] public Enum8    Column2 { get; set; } // tinyint
		[Column(DataType=DataType.NVarChar, Length=100), PrimaryKey(3), NotNull] public string    Column3 { get; set; } = null!; // nvarchar(100)
		[Column(DataType=DataType.Date),                 PrimaryKey(4), NotNull] public DateTime  Column4 { get; set; } // date
		[Column(DataType=DataType.Date),                    Nullable           ] public DateTime? Column5 { get; set; } // date
	}

	enum Enum1
	{
		[MapValue("E")] Field1,
		[MapValue("F")] Field2,
		[MapValue("B")] Field3,
	}

	[Table(Name="#tbl3333")]
	class Table3
	{
		[Column(DataType=DataType.Int32),   PrimaryKey, NotNull] public int    Column1 { get; set; }          // int
		[Column(DataType=DataType.NVarChar, Length=50), NotNull] public string Column2 { get; set; } = null!; // nvarchar(50)
		[Column(DataType=DataType.Char,     Length=1),  NotNull] public Enum1  Column3 { get; set; }          // char(1)
	}

	[Table(Name="#tbl4444")]
	class Table4
	{
		[Column(DataType=DataType.Date),  PrimaryKey(1), NotNull] public DateTime Column1 { get; set; } // date
		[Column(DataType=DataType.Int32), PrimaryKey(2), NotNull] public int      Column2 { get; set; } // int
	}

	[Table(Name="#tbl5555")]
	class Table5 : IInterface3
	{
		[Column(DataType=DataType.Int64),                PrimaryKey(1), NotNull] public long      Column1 { get; set; } // bigint
		[Column(DataType=DataType.NVarChar, Length=200), PrimaryKey(2), NotNull] public string    Column2 { get; set; } = null!; // nvarchar(200)
		[Column(DataType=DataType.Date),                 PrimaryKey(3), NotNull] public DateTime  Column4 { get; set; } // date
		[Column(DataType=DataType.Date),                    Nullable           ] public DateTime? Column5 { get; set; } // date
	}

	[Table(Name="#tbl6666")]
	class Table6 : IInterface3
	{
		[Column(DataType=DataType.Int32), PrimaryKey(1), NotNull] public int       Column1 { get; set; } // int
		[Column(DataType=DataType.Date),  PrimaryKey(2), NotNull] public DateTime  Column4 { get; set; } // date
		[Column(DataType=DataType.Date),     Nullable           ] public DateTime? Column5 { get; set; } // date
		[Column(DataType=DataType.Int64), PrimaryKey(3), NotNull] public long      Column6 { get; set; } // bigint
		[Column(DataType=DataType.Boolean),              NotNull] public bool      Column7 { get; set; } // bit
	}

	interface IInterface1
	{
		DateTime? Column5 { get; }
		DateTime? Column6 { get; }
	}

	enum Enum6
	{
		[MapValue(1)] Field1 = 1,
		[MapValue(2)] Field2 = 2,
	}

	enum Enum7
	{
		[MapValue("P")] Physical = 1,
		[MapValue("C")] Cash     = 2,
	}

	[Table(Name="#tbl7777")]
	class Table7
	{
		[Column(DataType=DataType.Date),             PrimaryKey(1), NotNull] public DateTime Column1 { get; set; } // date
		[Column(DataType=DataType.Int32),            PrimaryKey(2), NotNull] public int      Column2 { get; set; } // int
		[Column(DataType=DataType.Int32),                           NotNull] public int      Column3 { get; set; } // int
		[Column(DataType=DataType.Int32),                           NotNull] public int      Column4 { get; set; } // int
		[Column(DataType=DataType.Decimal, Precision=28, Scale=6), Nullable] public decimal  Column5 { get; set; } // decimal(28, 6)
		[Column(DataType=DataType.Decimal, Precision=28, Scale=6), Nullable] public decimal? Column6 { get; set; } // decimal(28, 6)
		[Column(DataType=DataType.VarChar, Length=20),             Nullable] public string?  Column7 { get; set; } // varchar(20)
	}

	[Table("#tbl8888")]
	class Table8
	{
		[Column, NotNull] public DateTime Column1;
		[Column, NotNull] public int      Column2;

		[Column(CanBeNull = true, DataType = DataType.Decimal, Precision = 24, Scale = 6)]
		public decimal? Column3;

		[Column(CanBeNull = true, DataType = DataType.VarChar, Length = 20)]
		public string Column4 = null!;
	}

	class Info1
	{
		public Table9  Prop1 = null!;
		public Table10 Prop2 = null!;
		public long        Prop3;
	}

	interface IInterface3
	{
		DateTime  Column4  { get; set; }
		DateTime? Column5 { get; set; }
	}

	enum Enum8
	{
		[MapValue(1)] Field1 = 1,
		[MapValue(2)] Field2 = 2,
		[MapValue(3)] Field3 = 3,
	}

	enum Enum9
	{
		[MapValue("A")] Field1,
		[MapValue("D")] Field2,
	}

	[Table(Name="#tbl9999")]
	class Table9 : IInterface3
	{
		[Column(DataType=DataType.Int32),              PrimaryKey(1), NotNull] public int       Column1  { get; set; } // int
		[Column(DataType=DataType.Date),               PrimaryKey(2), NotNull] public DateTime  Column4  { get; set; } // date
		[Column(DataType=DataType.Date),                             Nullable] public DateTime? Column5  { get; set; } // date
		[Column(DataType=DataType.Int32),                            Nullable] public int?      Column2  { get; set; } // int
		[Column(DataType=DataType.NVarChar, Length=20),              Nullable] public string?   Column3  { get; set; } // nvarchar(20)
		[Column(DataType=DataType.Decimal,  Precision=38, Scale=12), Nullable] public decimal?  Column6  { get; set; } // decimal(38, 12)
		[Column(DataType=DataType.Decimal,  Precision=38, Scale=12), Nullable] public decimal?  Column7  { get; set; } // decimal(38, 12)
		[Column(DataType=DataType.Char,     Length=1),               Nullable] public Enum7?    Column8  { get; set; } // char(1)
		[Column(DataType=DataType.Char,     Length=1),               Nullable] public Enum9?    Column9  { get; set; } // char(1)
		[Column(DataType=DataType.Decimal,  Precision=38, Scale=12), Nullable] public decimal?  Column10 { get; set; } // decimal(38, 12)
	}

	[Table(Name="#tbl1010")]
	class Table10
	{
		[Column(DataType=DataType.Date),                           NotNull ] public DateTime  Column1 { get; set; } // date
		[Column(DataType=DataType.VarChar, Length=20),             Nullable] public string?   Column2 { get; set; } // varchar(20)
		[Column(DataType=DataType.VarChar, Length=20),             Nullable] public string?   Column3 { get; set; } // varchar(20)
		[Column(DataType=DataType.VarChar, Length=20),             Nullable] public string?   Column4 { get; set; } // varchar(20)
		[Column(DataType=DataType.VarChar, Length=20),             Nullable] public string?   Column5 { get; set; } // varchar(20)
		[Column(DataType=DataType.Date),                           Nullable] public DateTime? Column6 { get; set; } // date
		[Column(DataType=DataType.Decimal, Precision=24, Scale=6), Nullable] public decimal?  Column7 { get; set; } // decimal(24, 6)
		[Column(DataType=DataType.VarChar, Length=20),             Nullable] public string?   Column8 { get; set; } // varchar(20)
	}

	class Table11
	{
		public Table9 Table9 { get; set; } = null!;

		public decimal? Column1
		{
			get => Table9.Column6;
			set => Table9.Column6 = value;
		}

		public decimal? Column2
		{
			get => Table9.Column10;
			set => Table9.Column10 = value;
		}

		public decimal? Column3
		{
			get => Table9.Column7;
			set => Table9.Column7 = value;
		}

		public decimal? Column4
		{
			get => Table9.Column7;
			set => Table9.Column7 = value;
		}

		public Enum7? Column5
		{
			get => Table9.Column8;
			set => Table9.Column8 = value;
		}

		public Enum9? Column6
		{
			get => Table9.Column9;
			set => Table9.Column9 = value;
		}

		public static implicit operator Table9(Table11 h)
		{
			return h.Table9;
		}
	}

	[Table(Name="#tbl1212")]
	class Table12 : IInterface3
	{
		[Column(DataType=DataType.Int32),                PrimaryKey(1), NotNull] public int       Column1 { get; set; } // int
		[Column(DataType=DataType.Int64),                PrimaryKey(2), NotNull] public long      Column2 { get; set; } // bigint
		[Column(DataType=DataType.Byte),                 PrimaryKey(3), NotNull] public Enum8     Column3 { get; set; } // tinyint
		[Column(DataType=DataType.NVarChar, Length=100), PrimaryKey(4), NotNull] public string    Column6 { get; set; } = null!; // nvarchar(100)
		[Column(DataType=DataType.Date),                 PrimaryKey(5), NotNull] public DateTime  Column4 { get; set; } // date
		[Column(DataType=DataType.Date),                    Nullable           ] public DateTime? Column5 { get; set; } // date
	}

	[Table(Name="#tbl1313")]
	class Table13 : IInterface3
	{
		[Column(DataType=DataType.Int64), PrimaryKey(1), NotNull] public long      Column1 { get; set; } // bigint
		[Column(DataType=DataType.Date),  PrimaryKey(2), NotNull] public DateTime  Column4 { get; set; } // date
		[Column(DataType=DataType.Date),     Nullable           ] public DateTime? Column5 { get; set; } // date
		[Column(DataType=DataType.VarChar, Length=3),    NotNull] public string?   Column2 { get; set; } // varchar(3)
	}

	[Table("#tbl1414")]
	class Table14
	{
		[PrimaryKey] public int Column1;
	}

	enum Enum10
	{
		[MapValue(1)] Field1 = 1,
		[MapValue(2)] Field2 = 2,
		[MapValue(3)] Field3 = 3,
		[MapValue(4)] Field4 = 4,
		[MapValue(5)] Field5 = 5,
	}

	[Table(Name="#tbl1515")]
	class Table15 : IInterface3
	{
		[Column(DataType=DataType.Int32), PrimaryKey,  NotNull] public int       Column1 { get; set; } // int
		[Column(DataType=DataType.Int32),              NotNull] public Enum10    Column2 { get; set; } // int
		[Column(DataType=DataType.Int32),    Nullable         ] public Enum6?    Column3 { get; set; } // int
		[Column(DataType=DataType.VarChar, Length=50), NotNull] public string?   Column6 { get; set; } // varchar(50)
		[Column(DataType=DataType.Date),               NotNull] public DateTime  Column4 { get; set; } // date
		[Column(DataType=DataType.Date),              Nullable] public DateTime? Column5 { get; set; } // date

		[ColumnAlias(nameof(Column6))]
		public Enum7? Column7
		{
			get
			{
				if (int.TryParse(Column6, out var value))
				{
					return ConvertTo<Enum7?>.From(value);
				}

				return ConvertTo<Enum7?>.From(Column6);
			}
			set => Column6 = ConvertTo<string>.From(value);
		}
	}

	[TestFixture]
	public class Tests : TestBase
	{
		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var task = Task.Run(() => TestImpl(context));

			var completedInTime = task.Wait(TimeSpan.FromSeconds(30));

			if (!completedInTime)
				Assert.Fail("Test exceeded timeout of 30 seconds");
		}

		[Test, Explicit("For debugging.")]
		public void DebugTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			TestImpl(context);
		}

		void TestImpl(string context)
		{
			var date = new DateTime(2018, 11, 30);

			using var db    = GetDataContext(context);
			using var tbl1  = db.CreateTempTable<Table1>();
			using var tbl2  = db.CreateTempTable<Table2>();
			using var tbl3  = db.CreateTempTable<Table3>();
			using var tbl4  = db.CreateTempTable<Table4>();
			using var tbl5  = db.CreateTempTable<Table5>();
			using var tbl6  = db.CreateTempTable<Table6>();
			using var tbl7  = db.CreateTempTable<Table7>();
			using var tbl9  = db.CreateTempTable<Table9>();
			using var tbl10 = db.CreateTempTable<Table10>();
			using var tbl12 = db.CreateTempTable<Table12>();
			using var tbl13 = db.CreateTempTable<Table13>();
			using var tbl15 = db.CreateTempTable<Table15>();

			var cobDates = new[] { new DateTime(2018, 11, 23), new DateTime(2018, 11, 27) };

			foreach (var cob in cobDates)
			{
				using (var tmp = db.CreateTempTable<Table14>())
				{
					var q =
						from i in db.Extension1(date, db.Extension6(date))
						join t in tmp on i.Table9.Column1 equals t.Column1
						select new
						{
							t.Column1, i.Column5
						};

					_ = q.ToDictionary(i => i.Column1, i => i.Column5);
				}

				Update1(db, cob, db.GetTable<Table7>());
				Update2(db, cob, db.GetTable<Table7>());
			}
		}

		static void Update1(IDataContext db, DateTime date, IQueryable<Table7> q1)
		{
			using var tmp123 = db.CreateTempTable("#tmp001",
				(
					from pc in q1.Select(pc => pc.Column4).Distinct().AsSubQuery()
					join ex in db.Extension8(date) on pc equals ex.Column1
					select new
					{
						ex.Column6
					}
				)
				.Distinct());

			var q =
				from mp in db.GetTable<Table10>()
				join e in
						from e in db.Extension5(date)
						join tex1 in tmp123 on e.Column1 equals tex1.Column6
						select e
					on mp.Column5 equals e.Column2 into j
				from e in j.DefaultIfEmpty()
				join be in
						from be in db.Extension9(date)
						join tex2 in tmp123 on be.Column1 equals tex2.Column6
						select be
					on mp.Column5 equals be.Column2 into j2
				from be in j2.DefaultIfEmpty()
				where
					mp.Column1 == date &&
					mp.Column2 != null &&
					mp.Column7 != 0    &&
					(e.Column2 != null || be.Column2 != null)
				select new Info1
				{
					Prop2 = mp,
					Prop3 = e.Column2 != null ? e.Column1 : be.Column1,
				};

			Update3(db, q, date, q1);
		}

		static void Update3(IDataContext db, IQueryable<Info1> q1, DateTime date, IQueryable<Table7> items)
		{
			using var mpt = db.CreateTempTable("#tmp002", q1.Select(t => new
			{
				t.Prop2.Column2,
				t.Prop2.Column6,
				t.Prop2.Column7,
				t.Prop2.Column8,
				t.Prop3,
			}),
				fm => fm
					.Property(mp => mp.Column2)
						.IsNullable(false)
						.HasLength(20)
					.Property(mp => mp.Column7)
						.HasPrecision(24)
						.HasScale(6)
					.Property(t => t.Column8)
						.HasLength(20));

			using var mpq = db.CreateTempTable("#tmp003",
				from mp in
					from mp in mpt
					group mp by new { mp.Column2, mp.Prop3 } into gr
					select new
					{
						gr.Key,
						Max = gr.Max(g => g.Column6)
					}
				from lp in
				(
					from lp in mpt
					where
						lp.Column2 == mp.Key.Column2 &&
						lp.Prop3   == mp.Key.Prop3   &&
						lp.Column6 == mp.Max
					select lp
				)
				.Take(1)
				.AsSqlServer()
					.OptionRecompile()
				select new
				{
					mp.Key.Column2,
					lp.Prop3,
					Column3 = lp.Column7,
					Column4 = lp.Column8
				},
				fm => fm
					.Property(t => t.Column2)
						.IsNullable(false)
						.IsPrimaryKey()
						.HasLength(20)
					.Property(t => t.Prop3)
						.IsPrimaryKey()
					.Property(mp => mp.Column3)
						.HasPrecision(24)
						.HasScale(6)
					.Property(t => t.Column4)
						.HasLength(20));

			using var tmp223 = db.CreateTempTable("#tmp004",
				mpq.Select(t => new { t.Column2 }).Distinct(),
				emp => emp.Property(t => t.Column2).IsNullable(false));

			using var tmp8 = db.CreateTempTable<Table8>();

			(
				from pc in db.GetTable<Table7>()
				join tmpc in items
					on     new { pc.Column1,   pc.Column2   }
					equals new { tmpc.Column1, tmpc.Column2 }
				join p  in db.GetTable<Table4>()
					on     new { pc.Column1, pc.Column3 }
					equals new {  p.Column1, Column3 = p.Column2 }
				join i in
				(
					from i in db.GetTable<Table9>()
						.Where(id => id.Extension7(date))
					join it in db.GetTable<Table3>()
						on i.Column2 equals it.Column1
					join pme in tmp223
						on i.Column3 equals pme.Column2
					where
						it.Column3.In(Enum1.Field1, Enum1.Field3)
					select new
					{
						i.Column1, PmeID = i.Column3
					}
				)
				.AsSubQuery()
				on pc.Column4 equals i.Column1
				join ie in db.Extension8(date)
					on     new {  i.Column1, Column7 = true }
					equals new { ie.Column1, ie.Column7 }
				join mp in mpq
					on     new { A = Sql.AsNotNull(i. PmeID),   B = ie.Column6 }
					equals new { A = Sql.AsNotNull(mp.Column2), B = mp.Prop3   }
				where
					pc.Column1 == date &&
					p. Column1  == date
				select new
				{
					pc,
					mp,
				}
			)
			.AsSqlServer()
				.OptionRecompile()
			.Insert(tmp8, mp => new Table8
			{
				Column1 = mp.pc.Column1,
				Column2 = mp.pc.Column2,
				Column3 = mp.mp.Column3,
				Column4 = mp.mp.Column4
			});

			(int Precision, int Scale) precAndScale = (28, 6);

			(
				from pc in db.GetTable<Table7>()
				join mp in tmp8
					on     new { pc.Column1, pc.Column2 }
					equals new { mp.Column1, mp.Column2 }
				where pc.Column1 == date
				select new
				{
					pc,
					mp,
					Column1 = VeryLongRunningTestExtensions.TryCastToDecimal(
						pc.Column5 * mp.Column3,
						precAndScale.Precision,
						precAndScale.Scale)
				}
			)
			.Update(
				pc => pc.pc,
				pc => new Table7
				{
					Column6 = pc.Column1,
					Column7 = pc.Column1 != null ? pc.mp.Column4 : null
				});
		}

		static void Update2(IDataContext db, DateTime date, IQueryable<Table7> items)
		{
			using var tmp12 = db.CreateTempTable("#tmp005", items.Select(pc => new { pc.Column4 }).Distinct());

			var q =
				from mp in db.GetTable<Table10>()
				join iei in db.Extension4(date)
					on mp.Column4 equals iei.Column6.Replace(' ', '-')
				join ins in tmp12
					on iei.Column1 equals ins.Column4
				join i in db.Extension6(date)
					on iei.Column1 equals i.Column1
				join ie in db.Extension8(date)
					on     new { iei.Column1, iei.Column2 }
					equals new {  ie.Column1,  Column2 = ie.Column6 }
				where
					mp.Column1  == date &&
					iei.Column3 == Enum8.Field3 &&
					ie.Column7  == true
				select new
				{
					i,
					mp,
					ie.Column6,
				};

			Update3(db, q.Where(r => r.mp.Column2 != null && r.i.Column2 == (int)Enum6.Field1).Select(r => new Info1 { Prop1 = r.i, Prop2 = r.mp, Prop3 = r.Column6 }), date, items);

			q =
				from mp in db.GetTable<Table10>()
				join iei in db.Extension4(date)
					on     new { A = mp.Column4,                    B = Enum8.Field2 }
					equals new { A = iei.Column6.Replace(' ', '-'), B = iei.Column3  }
				join ins in tmp12
					on iei.Column1 equals ins.Column4
				join i in db.Extension6(date)
					on iei.Column1 equals i.Column1
				join ie in db.Extension8(date)
					on     new { iei.Column1, A = iei.Column2, B = true }
					equals new { ie.Column1,  A = ie.Column6,  B = ie.Column7 }
				where
					i.Column2  == (int)Enum6.Field2 &&
					mp.Column1 == date &&
					mp.Column3 != null &&
					mp.Column7 != 0
				select new
				{
					i,
					mp,
					ie.Column6,
				};

			q = q.Where(r => r.i.Column2 == (int)Enum6.Field2 && r.mp.Column3 != null);

			Update4(db, q.Select(r => new Info1 { Prop1 = r.i, Prop2 = r.mp, Prop3 = r.Column6 }), date, items, false);

			q =
				from mp in db.GetTable<Table10>()
				join iei in db.Extension4(date)
					on     new { A = mp.Column4,                    B = Enum8.Field3 }
					equals new { A = iei.Column6.Replace(' ', '-'), B = iei.Column3  }
				join ins in tmp12
					on iei.Column1 equals ins.Column4
				join i in db.Extension6(date)
					on iei.Column1 equals i.Column1
				join ie in db.Extension8(date)
					on     new { iei.Column1, A = iei.Column2, B = true }
					equals new { ie.Column1,  A = ie.Column6,  B = ie.Column7 }
				where
					i.Column2 == (int)Enum6.Field2 &&
					mp.Column1 == date &&
					mp.Column3 != null &&
					mp.Column7 != 0
				select new
				{
					i,
					mp,
					ie.Column6,
				};

			q = q.Where(r => r.i.Column2 == (int)Enum6.Field2 && r.mp.Column3 != null);

			Update4(db, q.Select(r => new Info1 { Prop1 = r.i, Prop2 = r.mp, Prop3 = r.Column6 }), date, items, false);

			q =
				from mp in db.GetTable<Table10>()
				join ii in db.Extension10(date)
					on     new { mp.Column3, A = Enum8.Field1 }
					equals new { ii.Column3, A = ii.Column2   }
				join ins in tmp12
					on ii.Column1 equals ins.Column4
				join i in db.Extension6(date)
					on ii.Column1 equals i.Column1
				join be in db.Extension9(date) on mp.Column5 equals be.Column2
				join ie in db.Extension8(date)
					on     new { ii.Column1, A = be.Column1, B = true }
					equals new { ie.Column1, A = ie.Column6, B = ie.Column7 }
				where
					i.Column2  == (int)Enum6.Field2 &&
					mp.Column1 == date &&
					mp.Column3 != null &&
					mp.Column7 != 0
				select new
				{
					i,
					mp,
					ie.Column6,
				};

			q = q.Where(r => r.i.Column2 == (int)Enum6.Field2 && r.mp.Column3 != null);

			Update4(db, q.Select(r => new Info1 { Prop1 = r.i, Prop2 = r.mp, Prop3 = r.Column6 }), date, items, false);
		}

		static void Update4(IDataContext db, IQueryable<Info1> q1, DateTime date, IQueryable<Table7> items, bool flag)
		{
			using var mpt = db.CreateTempTable("#tmp006", q1.Select(t => new
				{
					t.Prop2.Column3,
					t.Prop2.Column6,
					t.Prop2.Column7,
					t.Prop2.Column8,
					t.Prop3,
				}),
				fm => fm
					.Property(mp => mp.Column3)
						.IsNullable(false)
						.HasLength(20)
					.Property(mp => mp.Column7)
						.HasPrecision(24)
						.HasScale(6)
					.Property(t => t.Column8)
						.HasLength(20));

			using var mpq = db.CreateTempTable("#tmp007",
				from mp in
					from mp in mpt
					group mp by new { mp.Column3, mp.Prop3 } into gr
					select new
					{
						gr.Key,
						Max = gr.Max(g => g.Column6)
					}
				from lp in
				(
					from lp in mpt
					where
						lp.Column3 == mp.Key.Column3 &&
						lp.Prop3   == mp.Key.Prop3 &&
						lp.Column6 == mp.Max
					select lp
				)
				.Take(1)
				.AsSqlServer()
					.OptionRecompile()
				select new
				{
					mp.Key.Column3,
					lp.Prop3,
					lp.Column7,
					lp.Column8
				},
				fm => fm
					.Property(t => t.Column3)
						.IsNullable(false)
						.IsPrimaryKey()
						.HasLength(20)
					.Property(t => t.Prop3)
						.IsPrimaryKey()
					.Property(mp => mp.Column7)
						.HasPrecision(24)
						.HasScale(6)
					.Property(t => t.Column8)
						.HasLength(20));

			using var pmetmp = db.CreateTempTable("#tmp008",
				mpq.Select(t => new { ISIN = t.Column3 }).Distinct(),
				emp => emp.Property(t => t.ISIN).IsNullable(false));

			using var tmp8 = db.CreateTempTable<Table8>();

			var tempTableQuery =
				from pc in db.GetTable<Table7>()
				join tmpc in items
					on     new { pc.Column1,     pc.Column2 }
					equals new { tmpc.Column1, tmpc.Column2 }
				join p in db.GetTable<Table4>()
					on     new { pc.Column1, A = pc.Column3 }
					equals new { p.Column1,  A = p.Column2  }
				join i in
				(
					from i in db.GetTable<Table9>()
						.Where(id => id.Extension7(date))
					join it in db.GetTable<Table3>()
						on i.Column2 equals it.Column1
					join ii in db.Extension10(date)
						on     new {  i.Column1, A = Enum8.Field1 }
						equals new { ii.Column1, A = ii.Column2   }
					join pme in pmetmp
						on ii.Column3 equals pme.ISIN
					where
						it.Column3.In(Enum1.Field1, Enum1.Field3)
					select new
					{
						i.Column1,
						ii.Column3
					}
				)
				.AsSubQuery()
				.AsSqlServer()
					.OptionRecompile()
				on pc.Column4 equals i.Column1
				join ie in db.Extension8(date)
					on     new {  i.Column1, A = true }
					equals new { ie.Column1, A = ie.Column7 }
				join mp in mpq
					on     new { A = Sql.AsNotNull(i.Column3),  B = ie.Column6 }
					equals new { A = Sql.AsNotNull(mp.Column3), B = mp.Prop3 }
				where
					pc.Column1 == date &&
					p. Column1 == date
				select new
				{
					pc,
					mp,
				};

			if (!flag)
				tempTableQuery = tempTableQuery.Where(r => r.pc.Column6 == null);

			tempTableQuery.Insert(tmp8, mp => new Table8
			{
				Column1 = mp.pc.Column1,
				Column2 = mp.pc.Column2,
				Column3 = mp.mp.Column7,
				Column4 = mp.mp.Column8
			});

			(int Precision, int Scale) precAndScale = (28, 6);

			var updateQuery =
				from pc in db.GetTable<Table7>()
				join mp in tmp8
					on     new { pc.Column1, pc.Column2 }
					equals new { mp.Column1, mp.Column2 }
				where pc.Column1 == date
				select new
				{
					pc,
					mp,
					Column1 = VeryLongRunningTestExtensions.TryCastToDecimal(
						pc.Column5 * mp.Column3,
						precAndScale.Precision,
						precAndScale.Scale)
				};

			if (!flag)
				updateQuery = updateQuery.Where(r => r.pc.Column6 == null);

			updateQuery.Update(
				pc => pc.pc,
				pc => new Table7
				{
					Column6 = pc.Column1,
					Column7 = pc.Column1 != null ? pc.mp.Column4 : null
				});
		}
	}

	static class VeryLongRunningTestExtensions
	{
		[ExpressionMethod(nameof(Extension1Impl))]
		public static IQueryable<Table11> Extension1(this IDataContext db, DateTime date, IQueryable<Table9> items)
		{
			return Extension1Impl().Compile()(db, date, items);
		}

		public static Expression<Func<IDataContext,DateTime,IQueryable<Table9>,IQueryable<Table11>>> Extension1Impl()
		{
			return (db, dt, items) =>
				from i  in items
				join t1 in (
					from rd in db.GetTable<Table15>().Extension2(dt)
					where
						rd.Column3 != null
					group rd by rd.Column3 into g
					select new
					{
						Column1 = (int?)g.Key,
						Column2 = g.Min(x => x.Column2 == Enum10.Field3 ? x.Column6 : null),
						Column3 = g.Min(x => x.Column2 == Enum10.Field2 ? x.Column6 : null),
						Column4 = g.Min(x => x.Column2 == Enum10.Field1 ? x.Column7 : null)
					})
					on i.Column2 equals t1.Column1 into j
				from rd in j.DefaultIfEmpty()
				join t2 in (
					from rd in db.GetTable<Table15>().Extension2(dt)
					where
						rd.Column3 == null
					group rd by rd.Column3 into g
					select new
					{
						Column1 = (int?)g.Key,
						Column2 = g.Min(x => x.Column2 == Enum10.Field3 ? x.Column6 : null),
						Column3 = g.Min(x => x.Column2 == Enum10.Field2 ? x.Column6 : null),
						Column4 = g.Min(x => x.Column2 == Enum10.Field1 ? x.Column7 : null)
					})
					on true equals true into j3
				from rd2 in j3.DefaultIfEmpty()
				join t3 in (
					from r in db.GetTable<Table1>()
					where
						r.Extension3(dt) &&
						r.Column7 == ConstID.C1 &&
						r.Column8
					group r by r.Column2 into g
					select new
					{
						Column1 = g.Key,
						Column2 = g.Min(x => x.Column3 == Enum10.Field3 ? x.Column4  : null),
						Column3 = g.Min(x => x.Column3 == Enum10.Field2 ? x.Column4  : null),
						Column4 = g.Min(x => x.Column3 == Enum10.Field1 ? x.Column13 : null),
						Column5 = g.Min(x => x.Column3 == Enum10.Field4 ? x.Column14 : null),
						Column6 = g.Min(x => x.Column3 == Enum10.Field5 ? x.Column4  : null)
					})
					on i.Column1 equals t3.Column1 into j2
					from r in j2.DefaultIfEmpty()
				select new Table11
				{
					Table9  = i,
					Column1 = r.Column3 != null
						? Convert.ToDecimal(r.Column3)
						: i.Column6 == null && (rd.Column3 != null || rd2.Column3 != null)
							? rd.Column3 != null ? Convert.ToDecimal(rd.Column3) : Convert.ToDecimal(rd2.Column3)
							: i.Column6,
					Column3 = r.Column2 != null
						? Convert.ToDecimal(r.Column2)
						: i.Column7 == null && (rd.Column2 != null || rd2.Column2 != null)
							? rd.Column2 != null ? Convert.ToDecimal(rd.Column2) : Convert.ToDecimal(rd2.Column2)
							: i.Column7,
					Column4 = r.Column2 != null
						? Convert.ToDecimal(r.Column2)
						: i.Column7,
					Column5 = r.Column4 != null
						? r.Column4.Value
						: i.Column8 == null && (rd.Column4 != null || rd2.Column4 != null)
							? rd.Column4 != null ? rd.Column4.Value : rd2.Column4.Value
							: i.Column8,
					Column6 = r.Column5 ?? i.Column9,
					Column2 = r.Column6 != null ? Convert.ToDecimal(r.Column6) : i.Column10
				};
		}

		static class ExprCache
		{
			public static T Run<T>(ref T? var, Func<Expression<T>> expr)
				where T : class
			{
				return var ??= expr().Compile();
			}
		}

		[ExpressionMethod(nameof(Extension2Impl))]
		public static IQueryable<T> Extension2<T>(this ITable<T> table, DateTime date)
			where T : IInterface3
		{
			return table.Where(id => id.Extension7(date));
		}

		static Expression<Func<ITable<T>,DateTime,IQueryable<T>>> Extension2Impl<T>()
			where T : IInterface3
		{
			return (t, dt) => t.Where(id => id.Extension7(dt));
		}

		[ExpressionMethod(nameof(Extension3Impl))]
		public static bool Extension3(this IInterface1 obj, DateTime date)
		{
			return ExprCache.Run(ref _extension3Impl, Extension3Impl)(obj, date);
		}

		static            Func<IInterface1,DateTime,bool>? _extension3Impl;
		static Expression<Func<IInterface1,DateTime,bool>> Extension3Impl()
		{
			return (t, dt) => t.Column5 <= dt && (t.Column6 == null || t.Column6 > dt);
		}

		[Sql.Expression("TRY_CAST({0} AS DECIMAL({1}, {2}))", ServerSideOnly = true, InlineParameters = true)]
		public static decimal? TryCastToDecimal(object value, int precision, int scale)
		{
			throw new NotImplementedException();
		}

		[ExpressionMethod(nameof(Extension4Impl))]
		public static IQueryable<Table12> Extension4(this IDataContext dataContext, DateTime date)
		{
			return dataContext.GetTable<Table12>().Where(id => id.Extension7(date));
		}

		static Expression<Func<IDataContext,DateTime,IQueryable<Table12>>> Extension4Impl()
		{
			return (db,dt) => db.GetTable<Table12>().Where(id => id.Extension7(dt));
		}

		[ExpressionMethod(nameof(Extension5Impl))]
		public static IQueryable<Table13> Extension5(this IDataContext dataContext, DateTime date)
		{
			return dataContext.GetTable<Table13>().Where(id => id.Extension7(date));
		}

		static Expression<Func<IDataContext,DateTime,IQueryable<Table13>>> Extension5Impl()
		{
			return (db,dt) => db.GetTable<Table13>().Where(id => id.Extension7(dt));
		}

		[ExpressionMethod(nameof(Extension6Impl))]
		public static IQueryable<Table9> Extension6(this IDataContext db, DateTime date)
		{
			return db.GetTable<Table9>().Where(id => id.Extension7(date));
		}

		static Expression<Func<IDataContext,DateTime,IQueryable<Table9>>> Extension6Impl()
		{
			return (db,dt) => db.GetTable<Table9>().Where(id => id.Extension7(dt));
		}

		[ExpressionMethod(nameof(Extension7Impl))]
		public static bool Extension7(this IInterface3 obj, DateTime date)
		{
			return ExprCache.Run(ref _extension7Impl, Extension7Impl)(obj, date);
		}

		static            Func<IInterface3,DateTime,bool>? _extension7Impl;
		static Expression<Func<IInterface3,DateTime,bool>> Extension7Impl()
		{
			return (t, dt) => t.Column4 <= dt && (t.Column5 == null || t.Column5 > dt);
		}

		[ExpressionMethod(nameof(Extension8Impl))]
		public static IQueryable<Table6> Extension8(this IDataContext db, DateTime date)
		{
			return db.GetTable<Table6>().Where(id => id.Extension7(date));
		}

		static Expression<Func<DataContext,DateTime,IQueryable<Table6>>> Extension8Impl()
		{
			return (db,dt) => db.GetTable<Table6>().Where(id => id.Extension7(dt));
		}

		[ExpressionMethod(nameof(Extension9Impl))]
		public static IQueryable<Table5> Extension9(this IDataContext db, DateTime date)
		{
			return Extension9Impl().Compile()(db, date);
		}

		static Expression<Func<IDataContext,DateTime, IQueryable<Table5>>> Extension9Impl()
		{
			return (db, dt) => db.GetTable<Table5>().Where(rd => rd.Extension7(dt));
		}

		[ExpressionMethod(nameof(Extension10Impl))]
		public static IQueryable<Table2> Extension10(this IDataContext db, DateTime date)
		{
			return db.GetTable<Table2>().Where(id => id.Extension7(date));
		}

		static Expression<Func<DataContext,DateTime,IQueryable<Table2>>> Extension10Impl()
		{
			return (db,dt) => db.GetTable<Table2>().Where(id => id.Extension7(dt));
		}
	}
}
