using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	using Linq;
	using Common;
	using SqlQuery;
	using SqlProvider;
	using LinqToDB.DataProvider;

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

			bool               _isAsync;
			Expression?        _mapperExpression;
			DataReaderWrapper? _dataReader;

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

				var sqlProvider = _dataConnection.DataProvider.CreateSqlBuilder(_dataConnection.MappingSchema);

				var sb = new StringBuilder();

				sb.Append("-- ").Append(_dataConnection.ConfigurationString);

				if (_dataConnection.ConfigurationString != _dataConnection.DataProvider.Name)
					sb.Append(' ').Append(_dataConnection.DataProvider.Name);

				if (_dataConnection.DataProvider.Name != sqlProvider.Name)
					sb.Append(' ').Append(sqlProvider.Name);

				sb.AppendLine();

				var isFirst = true;

				for (var index = 0; index < _executionQuery!.PreparedQuery.Commands.Length; index++)
				{
					var queryCommand = _executionQuery!.PreparedQuery.Commands[index];
					sqlProvider.PrintParameters(sb, _executionQuery!.CommandsParameters[index]);

					sb.AppendLine(queryCommand.Command);

					if (isFirst && _executionQuery.PreparedQuery.QueryHints != null && _executionQuery.PreparedQuery.QueryHints.Count > 0)
					{
						isFirst = false;

						while (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r')
							sb.Length--;

						sb.AppendLine();

						var sql = sb.ToString();

						var sqlBuilder = _dataConnection.DataProvider.CreateSqlBuilder(_dataConnection.MappingSchema);
						sql = sqlBuilder.ApplyQueryHints(sql, _executionQuery.PreparedQuery.QueryHints);

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
						Command          = _dataReader?.Command ?? _dataConnection.CurrentCommand,
						MapperExpression = MapperExpression,
						StartTime        = _startedOn,
						ExecutionTime    = _stopwatch.Elapsed,
						RecordsAffected  = RowsCount,
						IsAsync          = _isAsync,
					});
				}

				_dataReader = null;

				base.Dispose();
			}

			public class CommandWithParameters
			{
				public CommandWithParameters(string command, SqlParameter[] sqlParameters)
				{
					Command = command;
					SqlParameters = sqlParameters;
				}

				public string              Command       { get; }
				public SqlParameter[]      SqlParameters { get; }
			}

			public class PreparedQuery
			{
				public CommandWithParameters[]          Commands      = null!;
				public SqlStatement                     Statement     = null!;
				public List<string>?                    QueryHints;
			}

			public class ExecutionPreparedQuery
			{
				public ExecutionPreparedQuery(PreparedQuery preparedQuery, IDbDataParameter[]?[] commandsParameters)
				{
					PreparedQuery = preparedQuery;
					CommandsParameters = commandsParameters;
				}

				public readonly PreparedQuery         PreparedQuery;
				public readonly IDbDataParameter[]?[] CommandsParameters;
			}

			ExecutionPreparedQuery? _executionQuery;

			static ExecutionPreparedQuery CreateExecutionQuery(DataConnection dataConnection, IQueryContext context,
				IReadOnlyParameterValues? parameterValues)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);
				return executionQuery;
			}

			static PreparedQuery GetCommand(DataConnection dataConnection, IQueryContext query, IReadOnlyParameterValues? parameterValues, int startIndent = 0)
			{
				if (query.Context != null)
				{
					return new PreparedQuery
					{
						Commands      = (CommandWithParameters[])query.Context,
						Statement     = query.Statement,
						QueryHints    = query.QueryHints,
					};
				}

				var sql = query.Statement;

				// custom query handling
				var preprocessContext = new EvaluationContext(parameterValues);
				var newSql            = dataConnection.ProcessQuery(sql, preprocessContext);

				if (!ReferenceEquals(sql, newSql))
				{
					sql = newSql;
					sql.IsParameterDependent = true;
				}

				var sqlBuilder   = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema);
				var sqlOptimizer = dataConnection.DataProvider.GetSqlOptimizer();

				var cc = sqlBuilder.CommandCount(sql);
				var sb = new StringBuilder();

				var commands = new CommandWithParameters[cc];

				if (!sql.IsParameterDependent)
					sql.IsParameterDependent = sqlOptimizer.IsParameterDependent(sql);

				// optimize, optionally with parameters
				var evaluationContext = new EvaluationContext(sql.IsParameterDependent ? parameterValues : null);

				var aliases = query.Aliases;
				if (aliases == null || !ReferenceEquals(query.Statement, sql))
				{
					// correct aliases if needed
					SqlStatement.PrepareQueryAndAliases(sql, query.Aliases, out aliases);
				}

				for (var i = 0; i < cc; i++)
				{
					var optimizationContext = new OptimizationContext(evaluationContext, aliases, dataConnection.DataProvider.SqlProviderFlags.IsParameterOrderDependent);
					sb.Length = 0;

					sqlBuilder.BuildSql(i, sql, sb, optimizationContext, startIndent);
					commands[i] = new CommandWithParameters(sb.ToString(), optimizationContext.GetParameters().ToArray());
					optimizationContext.ClearParameters();
				}

				if (!sql.IsParameterDependent)
				{
					query.Context = commands;

					// clear aliases, they are not needed after SQL generation.
					//
					query.Aliases = null;
				}

				return new PreparedQuery
				{
					Commands      = commands,
					Statement     = sql,
					QueryHints    = query.QueryHints,
				};
			}

			static IDbDataParameter[]?[] GetParameters(DataConnection dataConnection, PreparedQuery pq, IReadOnlyParameterValues? parameterValues)
			{
				var result = new IDbDataParameter[pq.Commands.Length][];
				for (var index = 0; index < pq.Commands.Length; index++)
				{
					var command = pq.Commands[index];
					if (command.SqlParameters.Length == 0)
						continue;

					var parms = new IDbDataParameter[command.SqlParameters.Length];

					for (var i = 0; i < command.SqlParameters.Length; i++)
					{
						var sqlp = command.SqlParameters[i];

						parms[i] = CreateParameter(dataConnection, sqlp, sqlp.GetParameterValue(parameterValues));
					}

					result[index] = parms;
				}

				return result;
			}

			static IDbDataParameter CreateParameter(DataConnection dataConnection, SqlParameter parameter, SqlParameterValue parmValue)
			{
				// this is not very nice: here we access command object before it initialized
				// and it could result in parameter being created from one command object, but assigned to another command
				// currently not an issue as it still works for supported providers
				var p          = dataConnection.GetOrCreateCommand().CreateParameter();
				var dbDataType = parmValue.DbDataType;
				var paramValue = parameter.CorrectParameterValue(parmValue.Value);

				if (dbDataType.DataType == DataType.Undefined)
				{
					dbDataType = dbDataType.WithDataType(
						dataConnection.MappingSchema.GetDataType(
							dbDataType.SystemType == typeof(object) && paramValue != null
								? paramValue.GetType()
								: dbDataType.SystemType).Type.DataType);
				}

				dataConnection.DataProvider.SetParameter(dataConnection, p, parameter.Name!, dbDataType, paramValue);
				// some providers (e.g. managed sybase provider) could change parameter name
				// which breaks parameters rebind logic
				parameter.Name = p.ParameterName;

				return p;
			}

			protected override void SetQuery(IReadOnlyParameterValues parameterValues)
			{
				_executionQuery = CreateExecutionQuery(_dataConnection, Query.Queries[QueryNumber], parameterValues);
			}

			void SetCommand()
			{
				SetCommand(true);

				InitFirstCommand(_dataConnection, _executionQuery!);
			}

			#region ExecuteNonQuery

			static int ExecuteNonQueryImpl(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				if (executionQuery.PreparedQuery.Commands.Length == 1)
				{
					InitFirstCommand(dataConnection, executionQuery);

					return dataConnection.ExecuteNonQuery();
				}

				var rowsAffected = -1;

				for (var i = 0; i < executionQuery.PreparedQuery.Commands.Length; i++)
				{
					InitCommand(dataConnection, executionQuery, i);

					if (i < executionQuery.PreparedQuery.Commands.Length - 1 && executionQuery.PreparedQuery.Commands[i].Command.StartsWith("DROP"))
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
				return ExecuteNonQueryImpl(_dataConnection, _executionQuery!);
			}

			public static int ExecuteNonQuery(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);

				return ExecuteNonQueryImpl(dataConnection, executionQuery);
			}

			#endregion

			#region ExecuteScalar

			static object? ExecuteScalarImpl(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				IDbDataParameter? idParam = null;

				if (dataConnection.DataProvider.SqlProviderFlags.IsIdentityParameterRequired)
				{
					if (executionQuery.PreparedQuery.Statement.NeedsIdentity())
					{
						idParam = dataConnection.CurrentCommand!.CreateParameter();

						idParam.ParameterName = "IDENTITY_PARAMETER";
						idParam.Direction     = ParameterDirection.Output;
						idParam.DbType        = DbType.Decimal;

						dataConnection.CurrentCommand!.Parameters.Add(idParam);
					}
				}

				if (executionQuery.PreparedQuery.Commands.Length == 1)
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

				InitCommand(dataConnection, executionQuery, 1);

				return dataConnection.ExecuteScalar();
			}

			public static object? ExecuteScalar(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);

				InitFirstCommand(dataConnection, executionQuery);

				return ExecuteScalarImpl(dataConnection, executionQuery);
			}

			public override object? ExecuteScalar()
			{
				SetCommand();
				return ExecuteScalarImpl(_dataConnection, _executionQuery!);
			}

			#endregion

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void InitFirstCommand(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				InitCommand(dataConnection, executionQuery, 0);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void InitCommand(DataConnection dataConnection, ExecutionPreparedQuery executionQuery, int index)
			{
				InitCommand(dataConnection,
					executionQuery.PreparedQuery.Commands[index],
					executionQuery.CommandsParameters[index],
					index == 0 ? executionQuery.PreparedQuery.QueryHints : null);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void InitCommand(DataConnection dataConnection, CommandWithParameters queryCommand, IDbDataParameter[]? dbParameters, List<string>? queryHints)
			{
				var hasParameters = dbParameters?.Length > 0;

				dataConnection.InitCommand(CommandType.Text, queryCommand.Command, null, queryHints, hasParameters);

				if (hasParameters)
				{
					foreach (var p in dbParameters!)
						dataConnection.CurrentCommand!.Parameters.Add(p);
				}

				dataConnection.CommitCommandInit();
			}

			#region ExecuteReader

			public static DataReaderWrapper ExecuteReader(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var executionQuery = CreateExecutionQuery(dataConnection, context, parameterValues);

				InitFirstCommand(dataConnection, executionQuery);

				return dataConnection.ExecuteReader();
			}

			public override DataReaderWrapper ExecuteReader()
			{
				SetCommand(true);

				InitFirstCommand(_dataConnection, _executionQuery!);

				return _dataReader = _dataConnection.ExecuteReader();
			}

			#endregion

			class DataReaderAsync : IDataReaderAsync
			{
				public DataReaderAsync(DataReaderWrapper dataReader)
				{
					_dataReader = dataReader;
				}

				readonly DataReaderWrapper _dataReader;

				public DbDataReader DataReader => _dataReader.DataReader!;

				public Task<bool> ReadAsync(CancellationToken cancellationToken)
				{
					return _dataReader.DataReader!.ReadAsync(cancellationToken);
				}

				public void Dispose()
				{
					_dataReader.Dispose();
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

				InitFirstCommand(_dataConnection, _executionQuery!);

				_dataReader = await _dataConnection.ExecuteDataReaderAsync(_dataConnection.GetCommandBehavior(CommandBehavior.Default), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				return new DataReaderAsync(_dataReader);
			}

			public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				await _dataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				base.SetCommand(true);

				if (_executionQuery!.PreparedQuery.Commands.Length == 1)
				{
					InitFirstCommand(_dataConnection, _executionQuery);

					return await _dataConnection.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
				}

				for (var i = 0; i < _executionQuery.PreparedQuery.Commands.Length; i++)
				{
					InitCommand(_dataConnection, _executionQuery, i);

					if (i < _executionQuery.PreparedQuery.Commands.Length - 1 && _executionQuery.PreparedQuery.Commands[i].Command.StartsWith("DROP"))
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
					if (_executionQuery!.PreparedQuery.Statement.NeedsIdentity())
					{
						idparam = _dataConnection.CurrentCommand!.CreateParameter();

						idparam.ParameterName = "IDENTITY_PARAMETER";
						idparam.Direction     = ParameterDirection.Output;
						idparam.DbType        = DbType.Decimal;

						_dataConnection.CurrentCommand!.Parameters.Add(idparam);
					}
				}

				if (_executionQuery!.PreparedQuery.Commands.Length == 1)
				{
					if (idparam != null)
					{
						// it is done because Firebird does not return parameters through ExecuteReader
						// Other providers should support such mode
						await _dataConnection.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

						return idparam.Value;
					}

					return await _dataConnection.ExecuteScalarAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
				}

				await _dataConnection.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				InitCommand(_dataConnection, _executionQuery, 1);

				return await _dataConnection.ExecuteScalarAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}
		}
	}
}
