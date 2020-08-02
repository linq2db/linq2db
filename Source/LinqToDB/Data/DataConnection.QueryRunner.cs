using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	using Linq;
	using Common;
	using SqlProvider;
	using SqlQuery;

	public partial class DataConnection
	{
		IQueryRunner IDataContext.GetQueryRunner(Query query, int queryNumber, Expression expression, object?[]? parameters, object?[]? preambles)
		{
			CheckAndThrowOnDisposed();
			return new QueryRunner(query, queryNumber, this, expression, parameters, preambles);
		}

		internal class QueryRunner : QueryRunnerBase
		{
			public QueryRunner(Query query, int queryNumber, DataConnection dataConnection, Expression expression, object?[]? parameters, object?[]? preambles)
				: base(query, queryNumber, dataConnection, expression, parameters, preambles)
			{
				_dataConnection = dataConnection;
				_executionScope = _dataConnection.DataProvider.ExecuteScope(_dataConnection);
			}

			readonly IDisposable?   _executionScope;
			readonly DataConnection _dataConnection;
			readonly DateTime       _startedOn = DateTime.UtcNow;
			readonly Stopwatch      _stopwatch = Stopwatch.StartNew();

			bool        _isAsync;
			Expression? _mapperExpression;

			public override Expression? MapperExpression
			{
				get => _mapperExpression;
				set
				{
					_mapperExpression = value;

					if (value != null && Configuration.Linq.TraceMapperExpression &&
					    _dataConnection.TraceSwitchConnection.TraceInfo)
					{
						_dataConnection.OnTraceConnection(new TraceInfo(_dataConnection, TraceInfoStep.MapperCreated)
						{
							TraceLevel       = TraceLevel.Info,
							MapperExpression = MapperExpression,
							StartTime        = _startedOn,
							ExecutionTime    = _stopwatch.Elapsed,
							IsAsync          = _isAsync,
						});
					}
				}
			}

			public override string GetSqlText()
			{
				SetCommand(false);

				var sqlProvider = _preparedQuery!.SqlProvider ?? _dataConnection.DataProvider.CreateSqlBuilder(_dataConnection.MappingSchema);

				var sb = new StringBuilder();

				sb.Append("-- ").Append(_dataConnection.ConfigurationString);

				if (_dataConnection.ConfigurationString != _dataConnection.DataProvider.Name)
					sb.Append(' ').Append(_dataConnection.DataProvider.Name);

				if (_dataConnection.DataProvider.Name != sqlProvider.Name)
					sb.Append(' ').Append(sqlProvider.Name);

				sb.AppendLine();

				sqlProvider.PrintParameters(sb, _preparedQuery.Parameters);

				var isFirst = true;

				foreach (var command in _preparedQuery.Commands)
				{
					sb.AppendLine(command);

					if (isFirst && _preparedQuery.QueryHints != null && _preparedQuery.QueryHints.Count > 0)
					{
						isFirst = false;

						while (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r')
							sb.Length--;

						sb.AppendLine();

						var sql = sb.ToString();

						var sqlBuilder = _dataConnection.DataProvider.CreateSqlBuilder(_dataConnection.MappingSchema);
						sql = sqlBuilder.ApplyQueryHints(sql, _preparedQuery.QueryHints);

						sb = new StringBuilder(sql);
					}
				}

				while (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r')
					sb.Length--;

				sb.AppendLine();

				return sb.ToString();
			}

			public override void Dispose()
			{
				if (_executionScope != null)
					_executionScope.Dispose();

				if (_dataConnection.TraceSwitchConnection.TraceInfo)
				{
					_dataConnection.OnTraceConnection(new TraceInfo(_dataConnection, TraceInfoStep.Completed)
					{
						TraceLevel       = TraceLevel.Info,
						Command          = _dataConnection.Command,
						MapperExpression = MapperExpression,
						StartTime        = _startedOn,
						ExecutionTime    = _stopwatch.Elapsed,
						RecordsAffected  = RowsCount,
						IsAsync          = _isAsync,
					});
				}

				base.Dispose();
			}

			public class PreparedQuery
			{
				public string[]                         Commands      = null!;
				public List<SqlParameter>               SqlParameters = null!;
				public IReadOnlyList<IDbDataParameter>? Parameters;
				public SqlStatement                     Statement     = null!;
				public ISqlBuilder                      SqlProvider   = null!;
				public List<string>?                    QueryHints;
			}

			PreparedQuery? _preparedQuery;

			static PreparedQuery GetCommand(DataConnection dataConnection, IQueryContext query, int startIndent = 0)
			{
				if (query.Context != null)
				{
					return new PreparedQuery
					{
						Commands      = (string[])query.Context,
						SqlParameters = query.Statement.Parameters,
						Statement     = query.Statement,
						QueryHints    = query.QueryHints,
					};
				}

				var sql = query.Statement;

				// custom query handling
				var newSql = dataConnection.ProcessQuery(sql);

				if (!ReferenceEquals(sql, newSql))
				{
					sql = newSql;
					sql.IsParameterDependent = true;
				}

				var sqlProvider = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema);

				sql = dataConnection.DataProvider.GetSqlOptimizer().OptimizeStatement(sql, dataConnection.MappingSchema, dataConnection.InlineParameters);

				var cc = sqlProvider.CommandCount(sql);
				var sb = new StringBuilder();

				var commands = new string[cc];

				for (var i = 0; i < cc; i++)
				{
					sb.Length = 0;

					sqlProvider.BuildSql(i, sql, sb, startIndent);
					commands[i] = sb.ToString();
				}

				if (!sql.IsParameterDependent)
				{
					query.Context = commands;

					query.Statement.Parameters.Clear();
					query.Statement.Parameters.AddRange(sqlProvider.ActualParameters);
				}

				return new PreparedQuery
				{
					Commands      = commands,
					SqlParameters = sqlProvider.ActualParameters,
					Statement     = sql,
					SqlProvider   = sqlProvider,
					QueryHints    = query.QueryHints,
				};
			}

			static void GetParameters(DataConnection dataConnection, PreparedQuery pq)
			{
				if (pq.SqlParameters.Count == 0)
					return;

				var parms = new List<IDbDataParameter>(pq.SqlParameters.Count);

				for (var i = 0; i < pq.SqlParameters.Count; i++)
				{
					var sqlp = pq.SqlParameters[i];

					if (sqlp.IsQueryParameter)
					{
						AddParameter(dataConnection, parms, sqlp.Name!, sqlp);
					}
				}

				pq.Parameters = parms;
			}

			static void AddParameter(DataConnection dataConnection, ICollection<IDbDataParameter> parms, string name, SqlParameter parm)
			{
				var p          = dataConnection.Command.CreateParameter();
				var dbDataType = parm.Type;
				var paramValue = parm.Value;

				if (dbDataType.DataType == DataType.Undefined)
				{
					dbDataType = dbDataType.WithDataType(
						dataConnection.MappingSchema.GetDataType(
							dbDataType.SystemType == typeof(object) && paramValue != null
								? paramValue.GetType()
								: dbDataType.SystemType).Type.DataType);
				}

				dataConnection.DataProvider.SetParameter(dataConnection, p, name, dbDataType, paramValue);

				parms.Add(p);
			}

			public static PreparedQuery SetQuery(DataConnection dataConnection, IQueryContext queryContext, int startIndent = 0)
			{
				var preparedQuery = GetCommand(dataConnection, queryContext, startIndent);

				GetParameters(dataConnection, preparedQuery);

				return preparedQuery;
			}

			protected override void SetQuery()
			{
				_preparedQuery = SetQuery(_dataConnection, Query.Queries[QueryNumber]);
			}

			void SetCommand()
			{
				SetCommand(true);

				var hasParameters = _preparedQuery!.Parameters?.Count > 0;

				_dataConnection.InitCommand(CommandType.Text, _preparedQuery.Commands[0], null, QueryHints, hasParameters);

				if (hasParameters)
					foreach (var p in _preparedQuery.Parameters!)
						_dataConnection.Command.Parameters.Add(p);
			}

			#region ExecuteNonQuery

			static int ExecuteNonQueryImpl(DataConnection dataConnection, PreparedQuery preparedQuery)
			{
				if (preparedQuery.Commands.Length == 1)
				{
					var hasParameters = preparedQuery.Parameters?.Count > 0;

					dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[0], null, preparedQuery.QueryHints, hasParameters);

					if (hasParameters)
						foreach (var p in preparedQuery.Parameters!)
							dataConnection.Command.Parameters.Add(p);

					return dataConnection.ExecuteNonQuery();
				}

				var rowsAffected = -1;

				for (var i = 0; i < preparedQuery.Commands.Length; i++)
				{
					var hasParameters = i == 0 && preparedQuery.Parameters?.Count > 0;

					dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[i], null, i == 0 ? preparedQuery.QueryHints : null, hasParameters);

					if (hasParameters)
						foreach (var p in preparedQuery.Parameters!)
							dataConnection.Command.Parameters.Add(p);

					if (i < preparedQuery.Commands.Length - 1 && preparedQuery.Commands[i].StartsWith("DROP"))
					{
						try
						{
							dataConnection.ExecuteNonQuery();
						}
						catch (Exception)
						{
						}
					}
					else
					{
						var n = dataConnection.ExecuteNonQuery();

						if (i == 0)
							rowsAffected = n;
					}
				}

				return rowsAffected;
			}

			public override int ExecuteNonQuery()
			{
				SetCommand(true);
				return ExecuteNonQueryImpl(_dataConnection, _preparedQuery!);
			}

			public static int ExecuteNonQuery(DataConnection dataConnection, IQueryContext context)
			{
				var preparedQuery = GetCommand(dataConnection, context);

				GetParameters(dataConnection, preparedQuery);

				return ExecuteNonQueryImpl(dataConnection, preparedQuery);
			}

			#endregion

			#region ExecuteScalar

			static object? ExecuteScalarImpl(DataConnection dataConnection, PreparedQuery preparedQuery)
			{
				IDbDataParameter? idParam = null;

				if (dataConnection.DataProvider.SqlProviderFlags.IsIdentityParameterRequired)
				{
					if (preparedQuery.Statement.NeedsIdentity())
					{
						idParam = dataConnection.Command.CreateParameter();

						idParam.ParameterName = "IDENTITY_PARAMETER";
						idParam.Direction     = ParameterDirection.Output;
						idParam.DbType        = DbType.Decimal;

						dataConnection.Command.Parameters.Add(idParam);
					}
				}

				if (preparedQuery.Commands.Length == 1)
				{
					if (idParam != null)
					{
						// This is because the firebird provider does not return any parameters via ExecuteReader
						// the rest of the providers must support this mode
						dataConnection.ExecuteNonQuery();

						return idParam.Value;
					}

					return dataConnection.ExecuteScalar();
				}

				dataConnection.ExecuteNonQuery();

				dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[1], null, null, false);

				return dataConnection.ExecuteScalar();
			}

			public static object? ExecuteScalar(DataConnection dataConnection, IQueryContext context)
			{
				var preparedQuery = GetCommand(dataConnection, context);

				GetParameters(dataConnection, preparedQuery);

				var hasParameters = preparedQuery.Parameters?.Count > 0;

				dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[0], null, preparedQuery.QueryHints, hasParameters);

				if (hasParameters)
					foreach (var p in preparedQuery.Parameters!)
						dataConnection.Command.Parameters.Add(p);

				return ExecuteScalarImpl(dataConnection, preparedQuery);
			}

			public override object? ExecuteScalar()
			{
				SetCommand();
				return ExecuteScalarImpl(_dataConnection, _preparedQuery!);
			}

			#endregion

			#region ExecuteReader

			public static IDataReader ExecuteReader(DataConnection dataConnection, IQueryContext context)
			{
				var preparedQuery = GetCommand(dataConnection, context);

				GetParameters(dataConnection, preparedQuery);

				var hasParameters = preparedQuery.Parameters?.Count > 0;

				dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[0], null, preparedQuery.QueryHints, hasParameters);

				if (hasParameters)
					foreach (var p in preparedQuery.Parameters!)
						dataConnection.Command.Parameters.Add(p);

				return dataConnection.ExecuteReader();
			}

			public override IDataReader ExecuteReader()
			{
				SetCommand(true);

				var hasParameters = _preparedQuery!.Parameters?.Count > 0;

				_dataConnection.InitCommand(CommandType.Text, _preparedQuery.Commands[0], null, QueryHints, hasParameters);

				if (hasParameters)
					foreach (var p in _preparedQuery.Parameters!)
						_dataConnection.Command.Parameters.Add(p);

				return _dataConnection.ExecuteReader();
			}

			#endregion

			class DataReaderAsync : IDataReaderAsync
			{
				public DataReaderAsync(DbDataReader dataReader)
				{
					_dataReader = dataReader;
				}

				readonly DbDataReader _dataReader;

				public IDataReader DataReader => _dataReader;

				public Task<bool> ReadAsync(CancellationToken cancellationToken)
				{
					return _dataReader.ReadAsync(cancellationToken);
				}

				public void Dispose()
				{
					// call interface method, because at least MySQL provider incorrectly override
					// methods for .net core 1x
					DataReader.Dispose();
				}

#if NETSTANDARD2_1PLUS
				public ValueTask DisposeAsync()
				{
					 return _dataReader.DisposeAsync();
				}
#elif !NETFRAMEWORK
				public ValueTask DisposeAsync()
				{
					Dispose();
					return new ValueTask(Task.CompletedTask);
				}
#endif
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				await _dataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				base.SetCommand(true);

				var hasParameters = _preparedQuery!.Parameters?.Count > 0;

				_dataConnection.InitCommand(CommandType.Text, _preparedQuery.Commands[0], null, QueryHints, hasParameters);

				if (hasParameters)
					foreach (var p in _preparedQuery.Parameters!)
						_dataConnection.Command.Parameters.Add(p);

				var dataReader = await _dataConnection.ExecuteReaderAsync(_dataConnection.GetCommandBehavior(CommandBehavior.Default), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				return new DataReaderAsync(dataReader);
			}

			public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				await _dataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				base.SetCommand(true);

				if (_preparedQuery!.Commands.Length == 1)
				{
					var hasParameters = _preparedQuery.Parameters?.Count > 0;

					_dataConnection.InitCommand(
						CommandType.Text, _preparedQuery.Commands[0], null, _preparedQuery.QueryHints, hasParameters);

					if (hasParameters)
						foreach (var p in _preparedQuery.Parameters!)
							_dataConnection.Command.Parameters.Add(p);

					return await _dataConnection.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
				}

				for (var i = 0; i < _preparedQuery.Commands.Length; i++)
				{
					var hasParameters = i == 0 && _preparedQuery.Parameters?.Count > 0;

					_dataConnection.InitCommand(
						CommandType.Text, _preparedQuery.Commands[i], null, i == 0 ? _preparedQuery.QueryHints : null, hasParameters);

					if (hasParameters)
						foreach (var p in _preparedQuery.Parameters!)
							_dataConnection.Command.Parameters.Add(p);

					if (i < _preparedQuery.Commands.Length - 1 && _preparedQuery.Commands[i].StartsWith("DROP"))
					{
						try
						{
							await _dataConnection.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
						}
						catch
						{
						}
					}
					else
					{
						await _dataConnection.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
					}
				}

				return -1;
			}

			public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				await _dataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				SetCommand();

				IDbDataParameter? idparam = null;

				if (_dataConnection.DataProvider.SqlProviderFlags.IsIdentityParameterRequired)
				{
					if (_preparedQuery!.Statement.NeedsIdentity())
					{
						idparam = _dataConnection.Command.CreateParameter();

						idparam.ParameterName = "IDENTITY_PARAMETER";
						idparam.Direction     = ParameterDirection.Output;
						idparam.DbType        = DbType.Decimal;

						_dataConnection.Command.Parameters.Add(idparam);
					}
				}

				if (_preparedQuery!.Commands.Length == 1)
				{
					if (idparam != null)
					{
						// так сделано потому, что фаерберд провайдер не возвращает никаких параметров через ExecuteReader
						// остальные провайдеры должны поддерживать такой режим
						await _dataConnection.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

						return idparam.Value;
					}

					return await _dataConnection.ExecuteScalarAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
				}

				await _dataConnection.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				_dataConnection.InitCommand(CommandType.Text, _preparedQuery.Commands[1], null, null, false);

				return await _dataConnection.ExecuteScalarAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}
		}
	}
}
