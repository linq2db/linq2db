using System;
using System.Data;
using System.Data.Common;
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

		// IT : QueryRunner - DataConnection
		//
		internal class QueryRunner : QueryRunnerBase
		{
			public QueryRunner(Query query, int queryNumber, DataConnection dataConnection, Expression expression, object[] parameters)
				: base(query, queryNumber, dataConnection, expression, parameters)
			{
				_dataConnection = dataConnection;
			}

			readonly DataConnection _dataConnection;
			readonly DateTime       _startedOn = DateTime.Now;

			bool _isAsync;

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
							IsAsync          = _isAsync,
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
						IsAsync          = _isAsync,
					});
				}

				base.Dispose();
			}

			void SetCommand()
			{
				SetCommand(true);

				_dataConnection.InitCommand(CommandType.Text, _preparedQuery.Commands[0], null, QueryHints);

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
				SetCommand(true);

				_dataConnection.InitCommand(CommandType.Text, _preparedQuery.Commands[0], null, QueryHints);

				if (_preparedQuery.Parameters != null)
					foreach (var p in _preparedQuery.Parameters)
						_dataConnection.Command.Parameters.Add(p);

				return _dataConnection.ExecuteReader();
			}

#if !NOASYNC

			class DataReaderAsync : IDataReaderAsync
			{
				public DataReaderAsync(DataConnection dataConnection, Func<int> skipAction, Func<int> takeAction)
				{
					_dataConnection = dataConnection;
					_skipAction     = skipAction;
					_takeAction     = takeAction;
				}

				readonly DataConnection _dataConnection;
				readonly Func<int>      _skipAction;
				readonly Func<int>      _takeAction;

				async Task IDataReaderAsync.QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Func<T,bool> action, CancellationToken cancellationToken)
				{
					using (var reader = await _dataConnection.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken))
					{
						var skip = _skipAction == null ? 0 : _skipAction();

						while (skip-- > 0 && await reader.ReadAsync(cancellationToken))
							{}

						var take = _takeAction == null ? int.MaxValue : _takeAction();

						while (take-- > 0 && await reader.ReadAsync(cancellationToken))
							if (!action(objectReader(reader)))
								return;
					}
				}

				/*
				async Task IDataReaderAsync.QueryElementAsync<T>(Func<IDataReader,T> objectReader, Action<T> action, CancellationToken cancellationToken)
				{
					using (var reader = await _dataConnection.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken))
						if (await reader.ReadAsync(cancellationToken))
							action(objectReader(reader));
				}
				*/
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken, TaskCreationOptions options)
			{
				_isAsync = true;

				base.SetCommand(true);

				await _dataConnection.InitCommandAsync(CommandType.Text, _preparedQuery.Commands[0], null, QueryHints, cancellationToken);

				if (_preparedQuery.Parameters != null)
					foreach (var p in _preparedQuery.Parameters)
						_dataConnection.Command.Parameters.Add(p);

				var dataReader = new DataReaderAsync(_dataConnection, SkipAction, TakeAction);

				return dataReader;
			}

#endif
		}
	}
}
