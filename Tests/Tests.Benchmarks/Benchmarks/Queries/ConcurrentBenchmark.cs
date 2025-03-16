using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;

using BenchmarkDotNet.Attributes;

using LinqToDB.Benchmarks.Mappings;
using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;

namespace LinqToDB.Benchmarks.Queries
{
	public class ConcurrentBenchmark
	{
		private Thread[]                                             _threads           = null!;
		private EventWaitHandle                                      _startJob          = new ManualResetEvent(false);
		private EventWaitHandle                                      _endJob            = new ManualResetEvent(false);
		private Action<DataConnection, long?>                        _job               = null!;
		private const int                                            _iterations        = 2;
		private long?                                                _userId            = 100500;
		private DataConnection[]                                     _db                = null!;
		private DbConnection                                        _cn                = null!;
		private static Func<DataConnection, long?, IQueryable<User>> _compiled          = null!;
		private volatile int                                         _doneCount;

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

			_compiled = CompiledQuery.Compile<DataConnection, long?, IQueryable<User>>(
				(db, userId) => from c in db.GetTable<User>()
						  where userId == null || c.Id == userId
						  select c);

			var threadCount = ThreadCount;
			_threads        = new Thread[threadCount];
			_db             = new DataConnection[threadCount];

			for (var i = 0; i < _threads.Length; i++)
			{
				_db[i]                   = new DataConnection(new DataOptions().UseConnection(PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95), _cn));
				_threads[i]              = new Thread(ThreadWorker);
				_threads[i].IsBackground = true; // we don't stop threads explicitly
				_threads[i].Start(i);
			}
		}

		void ThreadWorker(object? state)
		{
			var idx = (int)state!;
			while (_startJob.WaitOne(Timeout.Infinite))
			{
				_job(_db[idx], _userId);
				Interlocked.Increment(ref _doneCount);
				_endJob.WaitOne(Timeout.Infinite);
			}
		}

		void RunConcurrent(Action<DataConnection, long?> action)
		{
			Interlocked.Exchange(ref _doneCount, 0);
			_job = action;
			_endJob.Reset();
			_startJob.Set();
			while (_doneCount != ThreadCount)
			{
				Thread.Sleep(1);
			}

			_startJob.Reset();
			_endJob.Set();
		}

		[ParamsSource(nameof(ThreadCountDataProvider))]
		public int ThreadCount { get; set; }

		public IEnumerable<int> ThreadCountDataProvider => new[] {16, 32, 64};

		[Benchmark]
		public void Linq()
		{
			RunConcurrent(static (db, userId) =>
			{
				for (var i = 0; i < _iterations; i++)
				{
					var query = from c in db.GetTable<User>()
								where userId == null || c.Id == userId
								select c;

					foreach (var record in query)
					{ }
				}
			});
		}

		[Benchmark(Baseline = true)]
		public void Compiled()
		{
			RunConcurrent(static (db, userId) =>
			{
				for (var i = 0; i < _iterations; i++)
				{
					foreach (var record in _compiled(db, userId))
					{ }
				}
			});
		}
	}
}
