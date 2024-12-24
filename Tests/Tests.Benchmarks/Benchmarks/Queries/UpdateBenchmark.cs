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
	public class UpdateBenchmark
	{
		private const int      _iterations = 2;
		private DataConnection _db     = null!;
		private DbConnection   _cn     = null!;
		private Func<DataConnection, Workflow, int> _compiledLinqSet    = null!;
		private Func<DataConnection, Workflow, int> _compiledLinqObject = null!;

		private readonly Workflow _record = new ()
		{
			Id            = 1,
			RowVersion    = 2,
			Status        = StatusEnum.One,
			Result        = $"Result:{3}",
			Error         = $"Error:{4}",
			Steps         = $"Steps:{5}",
			StartTime     = DateTimeOffset.Now,
			UpdateTime    = DateTimeOffset.Now,
			ProcessedTime = DateTimeOffset.Now,
			CompleteTime  = DateTimeOffset.Now
		};

		[GlobalSetup]
		public void Setup()
		{
			_cn = new MockDbConnection(new QueryResult() { Return = 1 }, ConnectionState.Open);
			_db = new DataConnection(PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95), _cn);

			_compiledLinqSet = CompiledQuery.Compile<DataConnection, Workflow, int>(
				(db, record) => db.GetTable<Workflow>()
					.Where(x => x.Id == record.Id && x.RowVersion == record.RowVersion)
					.Set(x => x.Status, record.Status)
					.Set(x => x.Result, record.Result)
					.Set(x => x.Error, record.Error)
					.Set(x => x.Steps, record.Steps)
					.Set(x => x.UpdateTime, record.UpdateTime)
					.Set(x => x.RowVersion, x => x.RowVersion + 1)
					.Set(x => x.StartTime, record.StartTime)
					.Set(x => x.ProcessedTime, record.ProcessedTime)
					.Set(x => x.CompleteTime, record.CompleteTime)
					.Update());
			_compiledLinqObject = CompiledQuery.Compile<DataConnection, Workflow, int>(
				(db, record) => db.GetTable<Workflow>()
					.Where(x => x.Id == _record.Id && x.RowVersion == _record.RowVersion)
					.Update(x => new()
					{
						Status        = record.Status,
						Result        = record.Result,
						Error         = record.Error,
						Steps         = record.Steps,
						UpdateTime    = record.UpdateTime,
						RowVersion    = x.RowVersion + 1,
						StartTime     = record.StartTime,
						ProcessedTime = record.ProcessedTime,
						CompleteTime  = record.CompleteTime
					}));
		}

		[Benchmark]
		public int LinqSet()
		{
			int cnt = 0;

			for (var i = 0; i < _iterations; i++)
			{
				cnt = _db.GetTable<Workflow>()
					.Where(x => x.Id == _record.Id && x.RowVersion == _record.RowVersion)
					.Set(x => x.Status       , _record.Status)
					.Set(x => x.Result       , _record.Result)
					.Set(x => x.Error        , _record.Error)
					.Set(x => x.Steps        , _record.Steps)
					.Set(x => x.UpdateTime   , _record.UpdateTime)
					.Set(x => x.RowVersion   , x => x.RowVersion + 1)
					.Set(x => x.StartTime    , _record.StartTime)
					.Set(x => x.ProcessedTime, _record.ProcessedTime)
					.Set(x => x.CompleteTime , _record.CompleteTime)
					.Update();
			}

			return cnt;
		}

		[Benchmark]
		public int LinqObject()
		{
			int cnt = 0;

			for (var i = 0; i < _iterations; i++)
			{
				cnt = _db.GetTable<Workflow>()
					.Where(x => x.Id == _record.Id && x.RowVersion == _record.RowVersion)
					.Update(x => new()
					{
						Status        = _record.Status,
						Result        = _record.Result,
						Error         = _record.Error,
						Steps         = _record.Steps,
						UpdateTime    = _record.UpdateTime,
						RowVersion    = x.RowVersion + 1,
						StartTime     = _record.StartTime,
						ProcessedTime = _record.ProcessedTime,
						CompleteTime  = _record.CompleteTime
					});
			}

			return cnt;
		}

		// a bit different query: update by id only
		[Benchmark]
		public int Object()
		{
			int cnt = 0;

			for (var i = 0; i < _iterations; i++)
			{
				cnt = _db
					.Update(new Workflow()
					{
						Status        = _record.Status,
						Result        = _record.Result,
						Error         = _record.Error,
						Steps         = _record.Steps,
						UpdateTime    = _record.UpdateTime,
						RowVersion    = _record.RowVersion + 1,
						StartTime     = _record.StartTime,
						ProcessedTime = _record.ProcessedTime,
						CompleteTime  = _record.CompleteTime
					});
			}

			return cnt;
		}

		[Benchmark]
		public int CompiledLinqSet()
		{
			int cnt = 0;

			for (var i = 0; i < _iterations; i++)
			{
				cnt = _compiledLinqSet(_db, _record);
			}

			return cnt;
		}

		[Benchmark]
		public int CompiledLinqObject()
		{
			int cnt = 0;

			for (var i = 0; i < _iterations; i++)
			{
				cnt = _compiledLinqObject(_db, _record);
			}

			return cnt;
		}

		[Benchmark(Baseline = true)]
		public int RawAdoNet()
		{
			int cnt = 0;

			using (var cmd = _cn.CreateCommand())
			{
				cmd.CommandText = $"UPDATE workflow w SET w.status = :status, w.result = :result, w.error = :error, w.steps = :steps, w.update_time = :update_time, w.row_version = :row_version + 1, w. start_time = :start_time, w.processed_time = :processed_time, w.complete_time = :complete_time WHERE w.id = :id AND w.row_version = :rowversion";

				cmd.Parameters.Add(new MockDbParameter(":status"        , _record.Status));
				cmd.Parameters.Add(new MockDbParameter(":result"        , _record.Result));
				cmd.Parameters.Add(new MockDbParameter(":error"         , _record.Error));
				cmd.Parameters.Add(new MockDbParameter(":steps"         , _record.Steps));
				cmd.Parameters.Add(new MockDbParameter(":update_time"   , _record.UpdateTime));
				cmd.Parameters.Add(new MockDbParameter(":row_version"   , _record.RowVersion));
				cmd.Parameters.Add(new MockDbParameter(":start_time"    , _record.StartTime));
				cmd.Parameters.Add(new MockDbParameter(":processed_time", _record.ProcessedTime));
				cmd.Parameters.Add(new MockDbParameter(":complete_time" , _record.CompleteTime));
				cmd.Parameters.Add(new MockDbParameter(":id"            , _record.Id));
				cnt = cmd.ExecuteNonQuery();
			}

			return cnt;
		}
	}
}
