using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Queries
{
	public class Issue3253Benchmark
	{
		private const int      _iterations = 2;
		private DataConnection _db     = null!;
		private DbConnection   _cn     = null!;
		private Func<DataConnection, string, string, string, decimal, decimal, int> _compiledInsert = null!;

		[GlobalSetup]
		public void Setup()
		{
			_cn = new MockDbConnection(new QueryResult() { Return = 1 }, ConnectionState.Open);
			_db = new DataConnection(new DataOptions().UseConnection(SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft), _cn));

			_compiledInsert = CompiledQuery.Compile<DataConnection, string, string, string, decimal, decimal, int>((ctx, key, value1, value2, value3, value4) =>
				ctx.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, key)
					.Value(i => i.COLUMN2, value1)
					.Value(i => i.COLUMN12, value2)
					.Value(i => i.COLUMN15, value3)
					.Value(i => i.COLUMN16, value4)
					.Insert());
		}

		[Benchmark]
		public void Small_UpdateStatement_With_Variable_Parameters()
		{
			for (var i = 0; i < _iterations;)
			{
				var query = _db.GetTable<TESTTABLE>()
					.Where(i => i.COLUMN1 == "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Set(i => i.COLUMN15, i)
					.Set(i => i.COLUMN16, ++i)
					.Set(i => i.COLUMN2, "VALUE")
					.Set(i => i.COLUMN12, "N")
					.Update();
			}
		}

		[Benchmark]
		public async Task Small_UpdateStatement_With_Variable_Parameters_Async()
		{
			for (var i = 0; i < _iterations;)
			{
				var query = await _db.GetTable<TESTTABLE>()
					.Where(i => i.COLUMN1 == "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Set(i => i.COLUMN15, i)
					.Set(i => i.COLUMN16, ++i)
					.Set(i => i.COLUMN2, "VALUE")
					.Set(i => i.COLUMN12, "N")
					.UpdateAsync();
			}
		}

		[Benchmark]
		public void Small_UpdateStatement_With_Static_Parameters()
		{
			for (var i = 0; i < _iterations; i++)
			{
				var query = _db.GetTable<TESTTABLE>()
					.Where(i => i.COLUMN1 == "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Set(i => i.COLUMN2, "VALUE")
					.Set(i => i.COLUMN12, "N")
					.Set(i => i.COLUMN15, 654645)
					.Set(i => i.COLUMN16, 4547667897689)
					.Update();
			}
		}

		[Benchmark]
		public async Task Small_UpdateStatement_With_Static_Parameters_Async()
		{
			for (var i = 0; i < _iterations; i++)
			{
				var query = await _db.GetTable<TESTTABLE>()
					.Where(i => i.COLUMN1 == "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Set(i => i.COLUMN2, "VALUE")
					.Set(i => i.COLUMN12, "N")
					.Set(i => i.COLUMN15, 654645)
					.Set(i => i.COLUMN16, 4547667897689)
					.UpdateAsync();
			}
		}

		[Benchmark]
		public void Large_UpdateStatement_With_Variable_Parameters()
		{
			for (var i = 0; i < _iterations;)
			{
				var query = _db.GetTable<TESTTABLE>()
					.Where(i => i.COLUMN1 == "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Set(i => i.COLUMN2, "VALUE")
					.Set(i => i.COLUMN3, "VALUE2")
					.Set(i => i.COLUMN4, "VALUE3")
					.Set(i => i.COLUMN5, "VALUE4")
					.Set(i => i.COLUMN6, "VALUE5")
					.Set(i => i.COLUMN7, "")
					.Set(i => i.COLUMN8, "")
					.Set(i => i.COLUMN9, "VALUE6")
					.Set(i => i.COLUMN10, "VALUE7")
					.Set(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Set(i => i.COLUMN12, "N")
					.Set(i => i.COLUMN15, i)
					.Set(i => i.COLUMN16, ++i)
					.Update();
			}
		}

		[Benchmark]
		public async Task Large_UpdateStatement_With_Variable_Parameters_Async()
		{
			for (var i = 0; i < _iterations;)
			{
				var query = await _db.GetTable<TESTTABLE>()
					.Where(i => i.COLUMN1 == "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Set(i => i.COLUMN2, "VALUE")
					.Set(i => i.COLUMN3, "VALUE2")
					.Set(i => i.COLUMN4, "VALUE3")
					.Set(i => i.COLUMN5, "VALUE4")
					.Set(i => i.COLUMN6, "VALUE5")
					.Set(i => i.COLUMN7, "")
					.Set(i => i.COLUMN8, "")
					.Set(i => i.COLUMN9, "VALUE6")
					.Set(i => i.COLUMN10, "VALUE7")
					.Set(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Set(i => i.COLUMN12, "N")
					.Set(i => i.COLUMN15, i)
					.Set(i => i.COLUMN16, ++i)
					.UpdateAsync();
			}
		}

		[Benchmark]
		public void Large_UpdateStatement_With_Static_Parameters()
		{
			for (var i = 0; i < _iterations; i++)
			{
				var query = _db.GetTable<TESTTABLE>()
					.Where(i => i.COLUMN1 == "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Set(i => i.COLUMN2, "VALUE")
					.Set(i => i.COLUMN3, "VALUE2")
					.Set(i => i.COLUMN4, "VALUE3")
					.Set(i => i.COLUMN5, "VALUE4")
					.Set(i => i.COLUMN6, "VALUE5")
					.Set(i => i.COLUMN7, "")
					.Set(i => i.COLUMN8, "")
					.Set(i => i.COLUMN9, "VALUE6")
					.Set(i => i.COLUMN10, "VALUE7")
					.Set(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Set(i => i.COLUMN12, "N")
					.Set(i => i.COLUMN15, 3)
					.Set(i => i.COLUMN16, 4)
					.Update();
			}
		}

		[Benchmark]
		public async Task Large_UpdateStatement_With_Static_Parameters_Async()
		{
			for (var i = 0; i < _iterations; i++)
			{
				var query = await _db.GetTable<TESTTABLE>()
					.Where(i => i.COLUMN1 == "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Set(i => i.COLUMN2, "VALUE")
					.Set(i => i.COLUMN3, "VALUE2")
					.Set(i => i.COLUMN4, "VALUE3")
					.Set(i => i.COLUMN5, "VALUE4")
					.Set(i => i.COLUMN6, "VALUE5")
					.Set(i => i.COLUMN7, "")
					.Set(i => i.COLUMN8, "")
					.Set(i => i.COLUMN9, "VALUE6")
					.Set(i => i.COLUMN10, "VALUE7")
					.Set(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Set(i => i.COLUMN12, "N")
					.Set(i => i.COLUMN15, 3)
					.Set(i => i.COLUMN16, 4)
					.UpdateAsync();
			}
		}

		[Benchmark]
		public void Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches()
		{
			for (var i = 0; i < _iterations;)
			{
				var query = _db.GetTable<TESTTABLE>()
					.Where(i => i.COLUMN1 == "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Set(i => i.COLUMN2, "VALUE")
					.Set(i => i.COLUMN3, "VALUE2")
					.Set(i => i.COLUMN4, "VALUE3")
					.Set(i => i.COLUMN5, "VALUE4")
					.Set(i => i.COLUMN6, "VALUE5")
					.Set(i => i.COLUMN7, "")
					.Set(i => i.COLUMN8, "")
					.Set(i => i.COLUMN9, "VALUE6")
					.Set(i => i.COLUMN10, "VALUE7")
					.Set(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Set(i => i.COLUMN12, "N")
					.Set(i => i.COLUMN15, i)
					.Set(i => i.COLUMN16, ++i)
					.Update();

				Query.ClearCaches();
			}
		}

		[Benchmark]
		public async Task Large_UpdateStatement_With_Variable_Parameters_With_ClearCaches_Async()
		{
			for (var i = 0; i < _iterations;)
			{
				var query = await _db.GetTable<TESTTABLE>()
					.Where(i => i.COLUMN1 == "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Set(i => i.COLUMN2, "VALUE")
					.Set(i => i.COLUMN3, "VALUE2")
					.Set(i => i.COLUMN4, "VALUE3")
					.Set(i => i.COLUMN5, "VALUE4")
					.Set(i => i.COLUMN6, "VALUE5")
					.Set(i => i.COLUMN7, "")
					.Set(i => i.COLUMN8, "")
					.Set(i => i.COLUMN9, "VALUE6")
					.Set(i => i.COLUMN10, "VALUE7")
					.Set(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Set(i => i.COLUMN12, "N")
					.Set(i => i.COLUMN15, i)
					.Set(i => i.COLUMN16, ++i)
					.UpdateAsync();

				Query.ClearCaches();
			}
		}

		[Benchmark(Baseline = true)]
		public void RawAdoNet()
		{
			using (var cmd = _cn.CreateCommand())
			{
				cmd.CommandText = $"UPDATE TESTTABLE w SET w.COLUMN2 = :col2, w.COLUMN12 = :col12, w.COLUMN15 = :col15, w.COLUMN16 = :col16 WHERE w.COLUMN1 = :col1";

				cmd.Parameters.Add(new MockDbParameter(":col1", "61a018e7-6e43-44b7-ad53-5a55e626fbbe"));
				cmd.Parameters.Add(new MockDbParameter(":col2", "VALUE"));
				cmd.Parameters.Add(new MockDbParameter(":col12", "N"));
				cmd.Parameters.Add(new MockDbParameter(":col15", 654645));
				cmd.Parameters.Add(new MockDbParameter(":col16", 4547667897689));
				cmd.ExecuteNonQuery();
			}
		}

		[Benchmark]
		public void Small_InsertStatement_With_Variable_Parameters()
		{
			for (var i = 0; i < _iterations;)
			{
				_db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN15, i)
					.Value(i => i.COLUMN16, ++i)
					.Value(i => i.COLUMN1, "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, "VALUE")
					.Value(i => i.COLUMN12, "N")
					.Insert();
			}
		}

		[Benchmark]
		public async Task Small_InsertStatement_With_Variable_Parameters_Async()
		{
			for (var i = 0; i < _iterations;)
			{
				await _db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN15, i)
					.Value(i => i.COLUMN16, ++i)
					.Value(i => i.COLUMN1, "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, "VALUE")
					.Value(i => i.COLUMN12, "N")
					.InsertAsync();
			}
		}

		[Benchmark]
		public void Small_InsertStatement_With_Static_Parameters()
		{
			for (var i = 0; i < _iterations; i++)
			{
				_db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, "VALUE")
					.Value(i => i.COLUMN12, "N")
					.Value(i => i.COLUMN15, 654645)
					.Value(i => i.COLUMN16, 4547667897689)
					.Insert();
			}
		}

		[Benchmark]
		public async Task Small_InsertStatement_With_Static_Parameters_Async()
		{
			for (var i = 0; i < _iterations; i++)
			{
				await _db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, "VALUE")
					.Value(i => i.COLUMN12, "N")
					.Value(i => i.COLUMN15, 654645)
					.Value(i => i.COLUMN16, 4547667897689)
					.InsertAsync();
			}
		}

		[Benchmark]
		public void Large_InsertStatement_With_Variable_Parameters()
		{
			for (var i = 0; i < _iterations;)
			{
				_db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, "VALUE")
					.Value(i => i.COLUMN3, "VALUE2")
					.Value(i => i.COLUMN4, "VALUE3")
					.Value(i => i.COLUMN5, "VALUE4")
					.Value(i => i.COLUMN6, "VALUE5")
					.Value(i => i.COLUMN7, "")
					.Value(i => i.COLUMN8, "")
					.Value(i => i.COLUMN9, "VALUE6")
					.Value(i => i.COLUMN10, "VALUE7")
					.Value(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Value(i => i.COLUMN12, "N")
					.Value(i => i.COLUMN15, i)
					.Value(i => i.COLUMN16, ++i)
					.Insert();
			}
		}

		[Benchmark]
		public async Task Large_InsertStatement_With_Variable_Parameters_Async()
		{
			for (var i = 0; i < _iterations;)
			{
				await _db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, "VALUE")
					.Value(i => i.COLUMN3, "VALUE2")
					.Value(i => i.COLUMN4, "VALUE3")
					.Value(i => i.COLUMN5, "VALUE4")
					.Value(i => i.COLUMN6, "VALUE5")
					.Value(i => i.COLUMN7, "")
					.Value(i => i.COLUMN8, "")
					.Value(i => i.COLUMN9, "VALUE6")
					.Value(i => i.COLUMN10, "VALUE7")
					.Value(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Value(i => i.COLUMN12, "N")
					.Value(i => i.COLUMN15, i)
					.Value(i => i.COLUMN16, ++i)
					.InsertAsync();
			}
		}

		[Benchmark]
		public void Large_InsertStatement_With_Static_Parameters()
		{
			for (var i = 0; i < _iterations; i++)
			{
				_db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, "VALUE")
					.Value(i => i.COLUMN3, "VALUE2")
					.Value(i => i.COLUMN4, "VALUE3")
					.Value(i => i.COLUMN5, "VALUE4")
					.Value(i => i.COLUMN6, "VALUE5")
					.Value(i => i.COLUMN7, "")
					.Value(i => i.COLUMN8, "")
					.Value(i => i.COLUMN9, "VALUE6")
					.Value(i => i.COLUMN10, "VALUE7")
					.Value(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Value(i => i.COLUMN12, "N")
					.Value(i => i.COLUMN15, 3)
					.Value(i => i.COLUMN16, 4)
					.Insert();
			}
		}

		[Benchmark]
		public async Task Large_InsertStatement_With_Static_Parameters_Async()
		{
			for (var i = 0; i < _iterations; i++)
			{
				await _db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, "VALUE")
					.Value(i => i.COLUMN3, "VALUE2")
					.Value(i => i.COLUMN4, "VALUE3")
					.Value(i => i.COLUMN5, "VALUE4")
					.Value(i => i.COLUMN6, "VALUE5")
					.Value(i => i.COLUMN7, "")
					.Value(i => i.COLUMN8, "")
					.Value(i => i.COLUMN9, "VALUE6")
					.Value(i => i.COLUMN10, "VALUE7")
					.Value(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Value(i => i.COLUMN12, "N")
					.Value(i => i.COLUMN15, 3)
					.Value(i => i.COLUMN16, 4)
					.InsertAsync();
			}
		}

		[Benchmark]
		public void Large_InsertStatement_With_Variable_Parameters_With_ClearCaches()
		{
			for (var i = 0; i < _iterations;)
			{
				_db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, "VALUE")
					.Value(i => i.COLUMN3, "VALUE2")
					.Value(i => i.COLUMN4, "VALUE3")
					.Value(i => i.COLUMN5, "VALUE4")
					.Value(i => i.COLUMN6, "VALUE5")
					.Value(i => i.COLUMN7, "")
					.Value(i => i.COLUMN8, "")
					.Value(i => i.COLUMN9, "VALUE6")
					.Value(i => i.COLUMN10, "VALUE7")
					.Value(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Value(i => i.COLUMN12, "N")
					.Value(i => i.COLUMN15, i)
					.Value(i => i.COLUMN16, ++i)
					.Insert();

				Query.ClearCaches();
			}
		}

		[Benchmark]
		public async Task Large_InsertStatement_With_Variable_Parameters_With_ClearCaches_Async()
		{
			for (var i = 0; i < _iterations;)
			{
				await _db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, "VALUE")
					.Value(i => i.COLUMN3, "VALUE2")
					.Value(i => i.COLUMN4, "VALUE3")
					.Value(i => i.COLUMN5, "VALUE4")
					.Value(i => i.COLUMN6, "VALUE5")
					.Value(i => i.COLUMN7, "")
					.Value(i => i.COLUMN8, "")
					.Value(i => i.COLUMN9, "VALUE6")
					.Value(i => i.COLUMN10, "VALUE7")
					.Value(i => i.COLUMN11, "Microsoft Windows 10 Enterprise")
					.Value(i => i.COLUMN12, "N")
					.Value(i => i.COLUMN15, i)
					.Value(i => i.COLUMN16, ++i)
					.InsertAsync();

				Query.ClearCaches();
			}
		}

		[Benchmark]
		public void Large_Compiled_InsertStatement_With_Variable_Parameters()
		{
			for (var i = 0; i < _iterations; i++)
			{
				_compiledInsert(_db, "61a018e7-6e43-44b7-ad53-5a55e626fbbe", "VALUE", "N", 3479583475, 3845793);
			}
		}

		[Benchmark]
		public void Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload()
		{
			for (var i = 0; i < _iterations; i++)
			{
				_db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, () => "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, () => "VALUE")
					.Value(i => i.COLUMN3, () => "VALUE2")
					.Value(i => i.COLUMN4, () => "VALUE3")
					.Value(i => i.COLUMN5, () => "VALUE4")
					.Value(i => i.COLUMN6, () => "VALUE5")
					.Value(i => i.COLUMN7, () => "")
					.Value(i => i.COLUMN8, () => "")
					.Value(i => i.COLUMN9, () => "VALUE6")
					.Value(i => i.COLUMN10, () => "VALUE7")
					.Value(i => i.COLUMN11, () => "Microsoft Windows 10 Enterprise")
					.Value(i => i.COLUMN12, () => "N")
					.Value(i => i.COLUMN15, () => i)
					.Value(i => i.COLUMN16, () => i + 1)
					.Insert();
			}
		}

		[Benchmark]
		public async Task Large_InsertStatement_With_Variable_Parameters_Using_Expression_Overload_Async()
		{
			for (var i = 0; i < _iterations; i++)
			{
				await _db.GetTable<TESTTABLE>()
					.Value(i => i.COLUMN1, () => "61a018e7-6e43-44b7-ad53-5a55e626fbbe")
					.Value(i => i.COLUMN2, () => "VALUE")
					.Value(i => i.COLUMN3, () => "VALUE2")
					.Value(i => i.COLUMN4, () => "VALUE3")
					.Value(i => i.COLUMN5, () => "VALUE4")
					.Value(i => i.COLUMN6, () => "VALUE5")
					.Value(i => i.COLUMN7, () => "")
					.Value(i => i.COLUMN8, () => "")
					.Value(i => i.COLUMN9, () => "VALUE6")
					.Value(i => i.COLUMN10, () => "VALUE7")
					.Value(i => i.COLUMN11, () => "Microsoft Windows 10 Enterprise")
					.Value(i => i.COLUMN12, () => "N")
					.Value(i => i.COLUMN15, () => i)
					.Value(i => i.COLUMN16, () => i + 1)
					.InsertAsync();
			}
		}

		[Table("TESTTABLE")]
		public class TESTTABLE
		{
			[PrimaryKey] public string  COLUMN1  { get; set; } = null!;
			[Column    ] public string  COLUMN2  { get; set; } = null!;
			[Column    ] public string  COLUMN3  { get; set; } = null!;
			[Column    ] public string  COLUMN4  { get; set; } = null!;
			[Column    ] public string  COLUMN5  { get; set; } = null!;
			[Column    ] public string  COLUMN6  { get; set; } = null!;
			[Column    ] public string  COLUMN7  { get; set; } = null!;
			[Column    ] public string  COLUMN8  { get; set; } = null!;
			[Column    ] public string  COLUMN9  { get; set; } = null!;
			[Column    ] public string  COLUMN10 { get; set; } = null!;
			[Column    ] public string  COLUMN11 { get; set; } = null!;
			[Column    ] public string  COLUMN12 { get; set; } = null!;
			[Column    ] public decimal COLUMN15 { get; set; }
			[Column    ] public decimal COLUMN16 { get; set; }
		}
	}
}
