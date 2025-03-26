using System;
using System.Data;
using System.Data.Common;
using System.Linq;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.Mappings;
using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;

namespace LinqToDB.Benchmarks.Queries
{
	public class SelectBenchmark
	{
		private const int      _iterations = 2;
		private long           _userId = 100500;
		private DataConnection _db     = null!;
		private DbConnection   _cn     = null!;
		private Func<DataConnection, long, IQueryable<User>> _compiled = null!;

		[GlobalSetup]
		public void Setup()
		{
			var schema = new DataTable();
			schema.Columns.Add("AllowDBNull", typeof(bool));
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(true);

			var result = new QueryResult()
			{
				Schema     = schema,

				Names      = new[] { "id", "name", "login_count" },
				FieldTypes = new[] { typeof(long), typeof(string), typeof(int) },
				DbTypes    = new[] { "int8", "varchar", "int4" },

				Data       = new object?[][] { new object?[] { 100500L, "Vasily Lohankin", 123 } },
			};

			_cn = new MockDbConnection(result, ConnectionState.Open);
			_db = new DataConnection(new DataOptions().UseConnection(PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95), _cn));

			_compiled = CompiledQuery.Compile<DataConnection, long, IQueryable<User>>(
				(db, userId) => from c in db.GetTable<User>()
						  where c.Id == userId
						  select c);
		}

		[Benchmark]
		public User Linq()
		{
			User user = null!;

			for (var i = 0; i < _iterations; i++)
			{
				var query = from c in _db.GetTable<User>()
							where c.Id == _userId
							select c;

				foreach (var record in query)
					user = record;
			}

			return user!;
		}

		[Benchmark]
		public User Compiled()
		{
			User user = null!;

			for (var i = 0; i < _iterations; i++)
			{
				foreach (var record in _compiled(_db, _userId))
					user = record;
			}

			return user!;
		}

		[Benchmark]
		public User FromSql_Interpolation()
		{
			User user = null!;

			for (var i = 0; i < _iterations; i++)
			{
				var query = _db.FromSql<User>($"SELECT id, name, login_count FROM public.user_tbl WHERE id = {DataParameter.Int64("id", _userId)}");

				foreach (var record in query)
					user = record;
			}

			return user!;
		}

		[Benchmark]
		public User FromSql_Formattable()
		{
			User user = null!;

			for (var i = 0; i < _iterations; i++)
			{
				var query = _db.FromSql<User>("SELECT id, name, login_count FROM public.user_tbl WHERE id = {0}", DataParameter.Int64("id", _userId));

				foreach (var record in query)
					user = record;
			}

			return user!;
		}

		[Benchmark]
		public User Query()
		{
			User user = null!;

			var query = _db.Query<User>("SELECT id, name, login_count FROM public.user_tbl WHERE id = :id", DataParameter.Int64("id", _userId));

			foreach (var record in query)
				user = record;

			return user;
		}

		[Benchmark]
		public User Execute()
		{
			return _db.Execute<User>("SELECT id, name, login_count FROM public.user_tbl WHERE id = :id", DataParameter.Int64("id", _userId));
		}

		// this test-case use most optimal approach without extra checks/loops intentionally
		// to show most optimal implementation numbers
		[Benchmark(Baseline = true)]
		public User RawAdoNet()
		{
			User user = new User();

			using (var cmd = _cn.CreateCommand())
			{
				cmd.CommandText = $"SELECT * FROM public.user_tbl WHERE id = {_userId}";
				using (var rd = cmd.ExecuteReader())
				{
					if (rd.Read())
					{
						user.Id          = rd.GetInt64(0);
						user.Name        = rd.GetString(1);
						user.Login_count = rd.GetInt32(2);
					}
				}
			}

			return user;
		}
	}
}
