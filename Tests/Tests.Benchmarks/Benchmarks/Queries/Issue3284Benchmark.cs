using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using LinqToDB.Benchmarks.Mappings;
using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Queries
{
	public class Issue3284Benchmark
	{
		private const int      _iterations = 2;
		private IDbConnection  _cn     = null!;

		private static readonly Func<DataConnection, TestTableNotNull, int> compiledQuery
			= CompiledQuery.Compile((DataConnection ctx, TestTableNotNull record) =>
				ctx.GetTable<TestTableNotNull>()
					.Where(i => i.Id == record.Id)
					.Set(i => i.Column01, record.Column01)
					.Set(i => i.Column02, record.Column02)
					.Set(i => i.Column03, record.Column03)
					.Set(i => i.Column04, record.Column04)
					.Set(i => i.Column05, record.Column05)
					.Set(i => i.Column06, record.Column06)
					.Set(i => i.Column07, record.Column07)
					.Set(i => i.Column08, record.Column08)
					.Set(i => i.Column09, record.Column09)
					.Set(i => i.Column10, record.Column10)
					.Set(i => i.Column11, record.Column11)
					.Set(i => i.Column12, record.Column12)
					.Set(i => i.Column13, record.Column13)
					.Set(i => i.Column14, record.Column14)
					.Set(i => i.Column15, record.Column15)
					.Set(i => i.Column16, record.Column16)
					.Set(i => i.Column17, record.Column17)
					.Set(i => i.Column18, record.Column18)
					.Set(i => i.Column19, record.Column19)
					.Set(i => i.Column20, record.Column20)
					.Set(i => i.Column21, record.Column21)
					.Set(i => i.Column22, record.Column22)
					.Set(i => i.Column23, record.Column23)
					.Set(i => i.Column24, record.Column24)
					.Set(i => i.Column25, record.Column25)
					.Set(i => i.Column26, record.Column26)
					.Set(i => i.Column27, record.Column27)
					.Set(i => i.Column28, record.Column28)
					.Set(i => i.Column29, record.Column29)
					.Set(i => i.Column30, record.Column30)
					.Set(i => i.Column31, record.Column31)
					.Set(i => i.Column32, record.Column32)
					.Set(i => i.Column33, record.Column33)
					.Set(i => i.Column34, record.Column34)
					.Set(i => i.Column35, record.Column35)
					.Set(i => i.Column36, record.Column36)
					.Set(i => i.Column37, record.Column37)
					.Set(i => i.Column38, record.Column38)
					.Set(i => i.Column39, record.Column39)
					.Set(i => i.Column40, record.Column40)
					.Set(i => i.Column41, record.Column41)
					.Set(i => i.Column42, record.Column42)
					.Set(i => i.Column43, record.Column43)
					.Set(i => i.Column44, record.Column44)
					.Set(i => i.Column45, record.Column45)
					.Set(i => i.Column46, record.Column46)
					.Set(i => i.Column47, record.Column47)
					.Set(i => i.Column48, record.Column48)
					.Set(i => i.Column49, record.Column49)
					.Set(i => i.Column50, record.Column50)
					.Set(i => i.Column51, record.Column51)
					.Set(i => i.Column52, record.Column52)
					.Set(i => i.Column53, record.Column53)
					.Set(i => i.Column54, record.Column54)
					.Set(i => i.Column55, record.Column55)
					.Set(i => i.Column56, record.Column56)
					.Set(i => i.Column57, record.Column57)
					.Set(i => i.Column58, record.Column58)
					.Set(i => i.Column59, record.Column59)
					.Set(i => i.Column60, record.Column60)
					.Set(i => i.Column61, record.Column61)
					.Set(i => i.Column62, record.Column62)
					.Set(i => i.Column63, record.Column63)
					.Set(i => i.Column64, record.Column64)
					.Set(i => i.Column65, record.Column65)
					.Set(i => i.Column66, record.Column66)
					.Set(i => i.Column67, record.Column67)
					.Set(i => i.Column68, record.Column68)
					.Set(i => i.Column69, record.Column69)
					.Update());

		[GlobalSetup]
		public void Setup()
		{
			_cn = new MockDbConnection(new QueryResult() { Return = 1 }, ConnectionState.Open);
		}

		[Benchmark(Baseline = true)]
		public void Compiled()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				compiledQuery(db, Values.BuildTestRecord());
			}
		}

		[Benchmark]
		public void String()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNotNull>()
					.Where(p => p.Id == record.Id)
					.Set(i => i.Column01, record.Column01)
					.Set(i => i.Column02, record.Column02)
					.Set(i => i.Column03, record.Column03)
					.Set(i => i.Column04, record.Column04)
					.Set(i => i.Column05, record.Column05)
					.Set(i => i.Column06, record.Column06)
					.Set(i => i.Column07, record.Column07)
					.Set(i => i.Column08, record.Column08)
					.Set(i => i.Column09, record.Column09)
					.Set(i => i.Column10, record.Column10)
					.Set(i => i.Column11, record.Column11)
					.Set(i => i.Column12, record.Column12)
					.Set(i => i.Column13, record.Column13)
					.Set(i => i.Column14, record.Column14)
					.Set(i => i.Column15, record.Column15)
					.Set(i => i.Column16, record.Column16)
					.Set(i => i.Column17, record.Column17)
					.Set(i => i.Column18, record.Column18)
					.Set(i => i.Column19, record.Column19)
					.Update();
			}
		}

		[Benchmark]
		public void String_Nullable()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNullable>()
					.Where(p => p.Id == record.Id)
					.Set(i => i.Column01, record.Column01)
					.Set(i => i.Column02, record.Column02)
					.Set(i => i.Column03, record.Column03)
					.Set(i => i.Column04, record.Column04)
					.Set(i => i.Column05, record.Column05)
					.Set(i => i.Column06, record.Column06)
					.Set(i => i.Column07, record.Column07)
					.Set(i => i.Column08, record.Column08)
					.Set(i => i.Column09, record.Column09)
					.Set(i => i.Column10, record.Column10)
					.Set(i => i.Column11, record.Column11)
					.Set(i => i.Column12, record.Column12)
					.Set(i => i.Column13, record.Column13)
					.Set(i => i.Column14, record.Column14)
					.Set(i => i.Column15, record.Column15)
					.Set(i => i.Column16, record.Column16)
					.Set(i => i.Column17, record.Column17)
					.Set(i => i.Column18, record.Column18)
					.Set(i => i.Column19, record.Column19)
					.Update();
			}
		}

		[Benchmark]
		public void Int()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNotNull>()
					.Where(p => p.Id == record.Id)
					// int
					.Set(i => i.Column20, record.Column20)
					.Set(i => i.Column21, record.Column21)
					.Set(i => i.Column22, record.Column22)
					.Set(i => i.Column23, record.Column23)
					.Set(i => i.Column24, record.Column24)
					.Set(i => i.Column25, record.Column25)
					.Set(i => i.Column26, record.Column26)
					.Set(i => i.Column27, record.Column27)
					.Set(i => i.Column28, record.Column28)
					.Set(i => i.Column29, record.Column29)
					.Update();
			}
		}

		[Benchmark]
		public void Int_Nullable()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNullable>()
					.Where(p => p.Id == record.Id)
					// int?
					.Set(i => i.Column20, record.Column20)
					.Set(i => i.Column21, record.Column21)
					.Set(i => i.Column22, record.Column22)
					.Set(i => i.Column23, record.Column23)
					.Set(i => i.Column24, record.Column24)
					.Set(i => i.Column25, record.Column25)
					.Set(i => i.Column26, record.Column26)
					.Set(i => i.Column27, record.Column27)
					.Set(i => i.Column28, record.Column28)
					.Set(i => i.Column29, record.Column29)
					.Update();
			}
		}

		[Benchmark]
		public void DateTime()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNotNull>()
					.Where(p => p.Id == record.Id)
					// DateTime
					.Set(i => i.Column30, record.Column30)
					.Set(i => i.Column31, record.Column31)
					.Set(i => i.Column32, record.Column32)
					.Set(i => i.Column33, record.Column33)
					.Set(i => i.Column34, record.Column34)
					.Set(i => i.Column35, record.Column35)
					.Set(i => i.Column36, record.Column36)
					.Set(i => i.Column37, record.Column37)
					.Set(i => i.Column38, record.Column38)
					.Set(i => i.Column39, record.Column39)
					.Update();
			}
		}

		[Benchmark]
		public void DateTime_Nullable()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNullable>()
					.Where(p => p.Id == record.Id)
					// DateTime?
					.Set(i => i.Column30, record.Column30)
					.Set(i => i.Column31, record.Column31)
					.Set(i => i.Column32, record.Column32)
					.Set(i => i.Column33, record.Column33)
					.Set(i => i.Column34, record.Column34)
					.Set(i => i.Column35, record.Column35)
					.Set(i => i.Column36, record.Column36)
					.Set(i => i.Column37, record.Column37)
					.Set(i => i.Column38, record.Column38)
					.Set(i => i.Column39, record.Column39)
					.Update();
			}
		}

		[Benchmark]
		public void Bool()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNotNull>()
					.Where(p => p.Id == record.Id)
					// bool
					.Set(i => i.Column40, record.Column40)
					.Set(i => i.Column41, record.Column41)
					.Set(i => i.Column42, record.Column42)
					.Set(i => i.Column43, record.Column43)
					.Set(i => i.Column44, record.Column44)
					.Set(i => i.Column45, record.Column45)
					.Set(i => i.Column46, record.Column46)
					.Set(i => i.Column47, record.Column47)
					.Set(i => i.Column48, record.Column48)
					.Set(i => i.Column49, record.Column49)
					.Update();
			}
		}

		[Benchmark]
		public void Bool_Nullable()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNullable>()
					.Where(p => p.Id == record.Id)
					// bool?
					.Set(i => i.Column40, record.Column40)
					.Set(i => i.Column41, record.Column41)
					.Set(i => i.Column42, record.Column42)
					.Set(i => i.Column43, record.Column43)
					.Set(i => i.Column44, record.Column44)
					.Set(i => i.Column45, record.Column45)
					.Set(i => i.Column46, record.Column46)
					.Set(i => i.Column47, record.Column47)
					.Set(i => i.Column48, record.Column48)
					.Set(i => i.Column49, record.Column49)
					.Update();
			}
		}

		[Benchmark]
		public void Decimal()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNotNull>()
					.Where(p => p.Id == record.Id)
					// decimal
					.Set(i => i.Column50, record.Column50)
					.Set(i => i.Column51, record.Column51)
					.Set(i => i.Column52, record.Column52)
					.Set(i => i.Column53, record.Column53)
					.Set(i => i.Column54, record.Column54)
					.Set(i => i.Column55, record.Column55)
					.Set(i => i.Column56, record.Column56)
					.Set(i => i.Column57, record.Column57)
					.Set(i => i.Column58, record.Column58)
					.Set(i => i.Column59, record.Column59)
					.Update();
			}
		}

		[Benchmark]
		public void Decimal_Nullable()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNullable>()
					.Where(p => p.Id == record.Id)
					// decimal?
					.Set(i => i.Column50, record.Column50)
					.Set(i => i.Column51, record.Column51)
					.Set(i => i.Column52, record.Column52)
					.Set(i => i.Column53, record.Column53)
					.Set(i => i.Column54, record.Column54)
					.Set(i => i.Column55, record.Column55)
					.Set(i => i.Column56, record.Column56)
					.Set(i => i.Column57, record.Column57)
					.Set(i => i.Column58, record.Column58)
					.Set(i => i.Column59, record.Column59)
					.Update();
			}
		}

		[Benchmark]
		public void Float()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNotNull>()
					.Where(p => p.Id == record.Id)
					// float
					.Set(i => i.Column60, record.Column60)
					.Set(i => i.Column61, record.Column61)
					.Set(i => i.Column62, record.Column62)
					.Set(i => i.Column63, record.Column63)
					.Set(i => i.Column64, record.Column64)
					.Set(i => i.Column65, record.Column65)
					.Set(i => i.Column66, record.Column66)
					.Set(i => i.Column67, record.Column67)
					.Set(i => i.Column68, record.Column68)
					.Set(i => i.Column69, record.Column69)
					.Update();
			}
		}

		[Benchmark]
		public void Float_Nullable()
		{
			using (var db = new DataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), _cn))
			{
				var record = Values.BuildTestRecord();
				db.GetTable<TestTableNullable>()
					.Where(p => p.Id == record.Id)
					// float?
					.Set(i => i.Column60, record.Column60)
					.Set(i => i.Column61, record.Column61)
					.Set(i => i.Column62, record.Column62)
					.Set(i => i.Column63, record.Column63)
					.Set(i => i.Column64, record.Column64)
					.Set(i => i.Column65, record.Column65)
					.Set(i => i.Column66, record.Column66)
					.Set(i => i.Column67, record.Column67)
					.Set(i => i.Column68, record.Column68)
					.Set(i => i.Column69, record.Column69)
					.Update();
			}
		}

		[Table(Name = "TestTable")]
		public class TestTableNotNull
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(CanBeNull = false)] public string   Column01 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column02 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column03 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column04 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column05 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column06 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column07 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column08 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column09 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column10 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column11 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column12 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column13 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column14 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column15 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column16 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column17 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column18 { get; set; } = null!;
			[Column(CanBeNull = false)] public string   Column19 { get; set; } = null!;
			[Column                   ] public int      Column20 { get; set; }
			[Column                   ] public int      Column21 { get; set; }
			[Column                   ] public int      Column22 { get; set; }
			[Column                   ] public int      Column23 { get; set; }
			[Column                   ] public int      Column24 { get; set; }
			[Column                   ] public int      Column25 { get; set; }
			[Column                   ] public int      Column26 { get; set; }
			[Column                   ] public int      Column27 { get; set; }
			[Column                   ] public int      Column28 { get; set; }
			[Column                   ] public int      Column29 { get; set; }
			[Column                   ] public DateTime Column30 { get; set; }
			[Column                   ] public DateTime Column31 { get; set; }
			[Column                   ] public DateTime Column32 { get; set; }
			[Column                   ] public DateTime Column33 { get; set; }
			[Column                   ] public DateTime Column34 { get; set; }
			[Column                   ] public DateTime Column35 { get; set; }
			[Column                   ] public DateTime Column36 { get; set; }
			[Column                   ] public DateTime Column37 { get; set; }
			[Column                   ] public DateTime Column38 { get; set; }
			[Column                   ] public DateTime Column39 { get; set; }
			[Column                   ] public bool     Column40 { get; set; }
			[Column                   ] public bool     Column41 { get; set; }
			[Column                   ] public bool     Column42 { get; set; }
			[Column                   ] public bool     Column43 { get; set; }
			[Column                   ] public bool     Column44 { get; set; }
			[Column                   ] public bool     Column45 { get; set; }
			[Column                   ] public bool     Column46 { get; set; }
			[Column                   ] public bool     Column47 { get; set; }
			[Column                   ] public bool     Column48 { get; set; }
			[Column                   ] public bool     Column49 { get; set; }
			[Column                   ] public decimal  Column50 { get; set; }
			[Column                   ] public decimal  Column51 { get; set; }
			[Column                   ] public decimal  Column52 { get; set; }
			[Column                   ] public decimal  Column53 { get; set; }
			[Column                   ] public decimal  Column54 { get; set; }
			[Column                   ] public decimal  Column55 { get; set; }
			[Column                   ] public decimal  Column56 { get; set; }
			[Column                   ] public decimal  Column57 { get; set; }
			[Column                   ] public decimal  Column58 { get; set; }
			[Column                   ] public decimal  Column59 { get; set; }
			[Column                   ] public float    Column60 { get; set; }
			[Column                   ] public float    Column61 { get; set; }
			[Column                   ] public float    Column62 { get; set; }
			[Column                   ] public float    Column63 { get; set; }
			[Column                   ] public float    Column64 { get; set; }
			[Column                   ] public float    Column65 { get; set; }
			[Column                   ] public float    Column66 { get; set; }
			[Column                   ] public float    Column67 { get; set; }
			[Column                   ] public float    Column68 { get; set; }
			[Column                   ] public float    Column69 { get; set; }
		}

		[Table(Name = "TestTable")]
		public class TestTableNullable
		{
			[PrimaryKey] public int Id { get; set; }

			[Column] public string?   Column01 { get; set; }
			[Column] public string?   Column02 { get; set; }
			[Column] public string?   Column03 { get; set; }
			[Column] public string?   Column04 { get; set; }
			[Column] public string?   Column05 { get; set; }
			[Column] public string?   Column06 { get; set; }
			[Column] public string?   Column07 { get; set; }
			[Column] public string?   Column08 { get; set; }
			[Column] public string?   Column09 { get; set; }
			[Column] public string?   Column10 { get; set; }
			[Column] public string?   Column11 { get; set; }
			[Column] public string?   Column12 { get; set; }
			[Column] public string?   Column13 { get; set; }
			[Column] public string?   Column14 { get; set; }
			[Column] public string?   Column15 { get; set; }
			[Column] public string?   Column16 { get; set; }
			[Column] public string?   Column17 { get; set; }
			[Column] public string?   Column18 { get; set; }
			[Column] public string?   Column19 { get; set; }
			[Column] public int?      Column20 { get; set; }
			[Column] public int?      Column21 { get; set; }
			[Column] public int?      Column22 { get; set; }
			[Column] public int?      Column23 { get; set; }
			[Column] public int?      Column24 { get; set; }
			[Column] public int?      Column25 { get; set; }
			[Column] public int?      Column26 { get; set; }
			[Column] public int?      Column27 { get; set; }
			[Column] public int?      Column28 { get; set; }
			[Column] public int?      Column29 { get; set; }
			[Column] public DateTime? Column30 { get; set; }
			[Column] public DateTime? Column31 { get; set; }
			[Column] public DateTime? Column32 { get; set; }
			[Column] public DateTime? Column33 { get; set; }
			[Column] public DateTime? Column34 { get; set; }
			[Column] public DateTime? Column35 { get; set; }
			[Column] public DateTime? Column36 { get; set; }
			[Column] public DateTime? Column37 { get; set; }
			[Column] public DateTime? Column38 { get; set; }
			[Column] public DateTime? Column39 { get; set; }
			[Column] public bool?     Column40 { get; set; }
			[Column] public bool?     Column41 { get; set; }
			[Column] public bool?     Column42 { get; set; }
			[Column] public bool?     Column43 { get; set; }
			[Column] public bool?     Column44 { get; set; }
			[Column] public bool?     Column45 { get; set; }
			[Column] public bool?     Column46 { get; set; }
			[Column] public bool?     Column47 { get; set; }
			[Column] public bool?     Column48 { get; set; }
			[Column] public bool?     Column49 { get; set; }
			[Column] public decimal?  Column50 { get; set; }
			[Column] public decimal?  Column51 { get; set; }
			[Column] public decimal?  Column52 { get; set; }
			[Column] public decimal?  Column53 { get; set; }
			[Column] public decimal?  Column54 { get; set; }
			[Column] public decimal?  Column55 { get; set; }
			[Column] public decimal?  Column56 { get; set; }
			[Column] public decimal?  Column57 { get; set; }
			[Column] public decimal?  Column58 { get; set; }
			[Column] public decimal?  Column59 { get; set; }
			[Column] public float?    Column60 { get; set; }
			[Column] public float?    Column61 { get; set; }
			[Column] public float?    Column62 { get; set; }
			[Column] public float?    Column63 { get; set; }
			[Column] public float?    Column64 { get; set; }
			[Column] public float?    Column65 { get; set; }
			[Column] public float?    Column66 { get; set; }
			[Column] public float?    Column67 { get; set; }
			[Column] public float?    Column68 { get; set; }
			[Column] public float?    Column69 { get; set; }
		}

		private static class Values
		{
			public static TestTableNotNull BuildTestRecord()
			{
				var x   = Rnd.Next();
				var now = System.DateTime.Now;

				return new TestTableNotNull()
				{
					Id       = 1,
					Column01 = "value for c01 " + x,
					Column02 = "value for c02 " + x,
					Column03 = "value for c03 " + x,
					Column04 = "value for c04 " + x,
					Column05 = "value for c05 " + x,
					Column06 = "value for c06 " + x,
					Column07 = "value for c07 " + x,
					Column08 = "value for c08 " + x,
					Column09 = "value for c09 " + x,
					Column10 = "value for c10 " + x,
					Column11 = "value for c11 " + x,
					Column12 = "value for c12 " + x,
					Column13 = "value for c13 " + x,
					Column14 = "value for c14 " + x,
					Column15 = "value for c15 " + x,
					Column16 = "value for c16 " + x,
					Column17 = "value for c17 " + x,
					Column18 = "value for c18 " + x,
					Column19 = "value for c19 " + x,
					Column20 = 100 + x,
					Column21 = 100 + x,
					Column22 = 100 + x,
					Column23 = 100 + x,
					Column24 = 100 + x,
					Column25 = 100 + x,
					Column26 = 100 + x,
					Column27 = 100 + x,
					Column28 = 100 + x,
					Column29 = 100 + x,
					Column30 = now.AddDays(1),
					Column31 = now.AddDays(2),
					Column32 = now.AddDays(3),
					Column33 = now.AddDays(4),
					Column34 = now.AddDays(5),
					Column35 = now.AddDays(6),
					Column36 = now.AddDays(7),
					Column37 = now.AddDays(8),
					Column38 = now.AddDays(9),
					Column39 = now.AddDays(10),
					Column40 = true,
					Column41 = false,
					Column42 = true,
					Column43 = false,
					Column44 = true,
					Column45 = false,
					Column46 = true,
					Column47 = false,
					Column48 = true,
					Column49 = false,
					Column50 = 100.1m + x,
					Column51 = 100.1m + x,
					Column52 = 100.1m + x,
					Column53 = 100.1m + x,
					Column54 = 100.1m + x,
					Column55 = 100.1m + x,
					Column56 = 100.1m + x,
					Column57 = 100.1m + x,
					Column58 = 100.1m + x,
					Column59 = 100.1m + x,
				};
			}

			private static Random Rnd = new(Environment.TickCount);
		}
	}
}
