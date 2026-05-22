using System;
using System.Data;
using System.Data.Common;
using System.Linq;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Queries
{
	public class Issue3268Benchmark
	{
		private const int      _iterations = 2;
		private DataConnection _db     = null!;
		private DbConnection   _cn     = null!;
		private Func<DataConnection, int, int> _compiled         = null!;
		private Func<DataConnection, int, int> _compiledNullable = null!;

		[GlobalSetup]
		public void Setup()
		{
			_cn = new MockDbConnection(new QueryResult() { Return = 1 }, ConnectionState.Open);
			_db = new DataConnection(new DataOptions().UseConnection(SqlServerTools.GetDataProvider(SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient), _cn));

			_compiled = CompiledQuery.Compile<DataConnection, int, int>((ctx, i) =>
				ctx.GetTable<MyPOCO>()
					.Where(p => p.Code == "A" + i && p.Currency == "SUR")
					.Set(p => p.Weight, i * 10)
					.Set(p => p.Currency, "SUR")
					.Set(p => p.Value, i * i + 2)
					.Update());

			_compiledNullable = CompiledQuery.Compile<DataConnection, int, int>((ctx, i) =>
				ctx.GetTable<MyPOCON>()
					.Where(p => p.Code == "A" + i && p.Currency == "SUR")
					.Set(p => p.Weight, i * 10)
					.Set(p => p.Currency, "SUR")
					.Set(p => p.Value, i * i + 2)
					.Update());
		}

		[Benchmark]
		public void Update_Nullable()
		{
			for (var i = 0; i < _iterations; i++)
			{
				_db.GetTable<MyPOCON>()
					.Where(p => p.Code == "A" + i && p.Currency == "SUR")
					.Set(p => p.Weight, i * 10)
					.Set(p => p.Currency, "SUR")
					.Set(p => p.Value, i * i + 2)
					.Update();
			}
		}

		[Benchmark]
		public void Update_Nullable_Full()
		{
			for (var i = 0; i < _iterations; i++)
			{
				using var db = new DataConnection(new DataOptions().UseConnection(SqlServerTools.GetDataProvider(SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient), _cn));
				db.GetTable<MyPOCON>()
					.Where(p => p.Code == "A" + i && p.Currency == "SUR")
					.Set(p => p.Weight, i * 10)
					.Set(p => p.Currency, "SUR")
					.Set(p => p.Value, i * i + 2)
					.Update();
			}
		}

		[Benchmark]
		public void Compiled_Update_Nullable()
		{
			for (var i = 0; i < _iterations; i++)
			{
				_compiledNullable(_db, i);
			}
		}

		[Benchmark]
		public void Compiled_Update_Nullable_Full()
		{
			for (var i = 0; i < _iterations; i++)
			{
				using var db = new DataConnection(new DataOptions().UseConnection(SqlServerTools.GetDataProvider(SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient), _cn));
				_compiledNullable(db, i);
			}
		}

		[Benchmark]
		public void Update()
		{
			for (var i = 0; i < _iterations; i++)
			{
				_db.GetTable<MyPOCO>()
					.Where(p => p.Code == "A" + i && p.Currency == "SUR")
					.Set(p => p.Weight, i * 10)
					.Set(p => p.Currency, "SUR")
					.Set(p => p.Value, i * i + 2)
					.Update();
			}
		}

		[Benchmark]
		public void Update_Full()
		{
			for (var i = 0; i < _iterations; i++)
			{
				using var db = new DataConnection(new DataOptions().UseConnection(SqlServerTools.GetDataProvider(SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient), _cn));
				db.GetTable<MyPOCO>()
					.Where(p => p.Code == "A" + i && p.Currency == "SUR")
					.Set(p => p.Weight, i * 10)
					.Set(p => p.Currency, "SUR")
					.Set(p => p.Value, i * i + 2)
					.Update();
			}
		}

		[Benchmark(Baseline = true)]
		public void Compiled_Update()
		{
			for (var i = 0; i < _iterations; i++)
			{
				_compiled(_db, i);
			}
		}

		[Benchmark]
		public void Compiled_Update_Full()
		{
			for (var i = 0; i < _iterations; i++)
			{
				using var db = new DataConnection(new DataOptions().UseConnection(SqlServerTools.GetDataProvider(SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient), _cn));
				_compiled(db, i);
			}
		}

		[Table]
		sealed class MyPOCON
		{
			[Column] public string?  Code     { get; set; }
			[Column] public string?  Currency { get; set; }
			[Column] public decimal  Value    { get; set; }
			[Column] public decimal? Weight   { get; set; }
		}

		[Table]
		sealed class MyPOCO
		{
			[Column] public string? Code     { get; set; }
			[Column] public string? Currency { get; set; }
			[Column] public decimal Value    { get; set; }
			[Column] public decimal Weight   { get; set; }
		}
	}
}
