using System;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	using Linq;

	public partial class DataConnection
	{
		IQueryRunner IDataContextEx.GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters)
		{
			ThrowOnDisposed();
			return new QueryRunner(query, queryNumber, this, expression, parameters);
		}

		internal class QueryRunner : QueryRunnerBase
		{
			public QueryRunner(Query query, int queryNumber, DataConnection dataConnection, Expression expression, object[] parameters)
				: base(query, queryNumber, dataConnection, expression, parameters)
			{
				_dataConnection = dataConnection;
			}

			readonly DataConnection _dataConnection;
			readonly DateTime       _startedOn = DateTime.Now;

			Expression _mapperExpression;

			public override Expression  MapperExpression
			{
				get { return _mapperExpression; }
				set
				{
					_mapperExpression = value;

					if (value != null && Common.Configuration.Linq.TraceMapperExpression &&
						TraceSwitch.TraceInfo && _dataConnection.OnTraceConnection != null)
					{
						_dataConnection.OnTraceConnection(new TraceInfo(TraceInfoStep.MapperCreated)
						{
							TraceLevel       = TraceLevel.Info,
							DataConnection   = _dataConnection,
							MapperExpression = MapperExpression,
						});
					}
				}
			}

			protected override void SetQuery()
			{
				throw new NotImplementedException();
			}

			public override void Dispose()
			{
				if (TraceSwitch.TraceInfo && _dataConnection.OnTraceConnection != null)
				{
					_dataConnection.OnTraceConnection(new TraceInfo(TraceInfoStep.Completed)
					{
						TraceLevel       = TraceLevel.Info,
						DataConnection   = _dataConnection,
						MapperExpression = MapperExpression,
						ExecutionTime    = DateTime.Now - _startedOn,
						RecordsAffected  = RowsCount,
					});
				}
			}

			void SetCommand()
			{
				var queryContext = Query.Queries[QueryNumber];

				var preparedQuery = _dataConnection.GetCommand(queryContext);

				_dataConnection.GetParameters(queryContext, preparedQuery);

				_dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[0], null, preparedQuery.QueryHints);

				if (preparedQuery.Parameters != null)
					foreach (var p in preparedQuery.Parameters)
						_dataConnection.Command.Parameters.Add(p);
			}

			public override int ExecuteNonQuery()
			{
				SetCommand();
				return _dataConnection.ExecuteNonQuery();
			}

			public override object ExecuteScalar()
			{
				SetCommand();
				return _dataConnection.ExecuteScalar();
			}

			public override IDataReader ExecuteReader()
			{
				SetCommand();
				return _dataConnection.ExecuteReader();
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken, TaskCreationOptions options)
			{
				var queryContext = Query.Queries[QueryNumber];

				var preparedQuery = _dataConnection.GetCommand(queryContext);

				_dataConnection.GetParameters(queryContext, preparedQuery);

				await _dataConnection.InitCommandAsync(CommandType.Text, preparedQuery.Commands[0], null, preparedQuery.QueryHints, cancellationToken);

				if (preparedQuery.Parameters != null)
					foreach (var p in preparedQuery.Parameters)
						_dataConnection.Command.Parameters.Add(p);

				var dataReader = await _dataConnection.SetCommand(preparedQuery.Commands[0], queryContext.GetParameters()).ExecuteReaderAsync(cancellationToken);

				dataReader.SkipAction = SkipAction;
				dataReader.TakeAction = TakeAction;

				return dataReader;
			}
		}
	}
}
