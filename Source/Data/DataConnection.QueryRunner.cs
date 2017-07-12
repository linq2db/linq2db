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

			PreparedQuery _preparedQuery;

			protected override void SetQuery()
			{
				var queryContext  = Query.Queries[QueryNumber];

				_preparedQuery = _dataConnection.GetCommand(queryContext);

				_dataConnection.GetParameters(queryContext, _preparedQuery);
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
				SetCommand(true);

				_dataConnection.InitCommand(CommandType.Text, _preparedQuery.Commands[0], null, _preparedQuery.QueryHints);

				if (_preparedQuery.Parameters != null)
					foreach (var p in _preparedQuery.Parameters)
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
				base.SetCommand(true);

				await _dataConnection.InitCommandAsync(CommandType.Text, _preparedQuery.Commands[0], null, _preparedQuery.QueryHints, cancellationToken);

				if (_preparedQuery.Parameters != null)
					foreach (var p in _preparedQuery.Parameters)
						_dataConnection.Command.Parameters.Add(p);

				var dataReader = await _dataConnection.SetCommand(_preparedQuery.Commands[0]).ExecuteReaderAsync(cancellationToken);

				dataReader.SkipAction = SkipAction;
				dataReader.TakeAction = TakeAction;

				return dataReader;
			}
		}
	}
}
