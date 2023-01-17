﻿using System;
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
	using SqlQuery;
	using SqlProvider;

	public partial class DataConnection
	{
		IQueryRunner IDataContext.GetQueryRunner(Query query, int queryNumber, Expression expression, object?[]? parameters, object?[]? preambles)
		{
			CheckAndThrowOnDisposed();
			return new QueryRunner(query, queryNumber, this, expression, parameters, preambles);
		}

		internal sealed class QueryRunner : QueryRunnerBase
		{
			public QueryRunner(Query query, int queryNumber, DataConnection dataConnection, Expression expression, object?[]? parameters, object?[]? preambles)
				: base(query, queryNumber, dataConnection, expression, parameters, preambles)
			{
				_dataConnection = dataConnection;
				_executionScope = _dataConnection.DataProvider.ExecuteScope(_dataConnection);
			}

			readonly IExecutionScope? _executionScope;
			readonly DataConnection   _dataConnection;
			readonly DateTime         _startedOn = DateTime.UtcNow;
			readonly Stopwatch        _stopwatch = Stopwatch.StartNew();

			bool        _isAsync;
			Expression? _mapperExpression;
			DataReaderWrapper? _dataReader;

			public override Expression? MapperExpression
			{
				get => _mapperExpression;
				set
				{
					_mapperExpression = value;

					if (value != null && DataContext.Options.LinqOptions.TraceMapperExpression && _dataConnection.TraceSwitchConnection.TraceInfo)
					{
						_dataConnection.OnTraceConnection(new TraceInfo(_dataConnection, TraceInfoStep.MapperCreated, TraceOperation.BuildMapping, _isAsync)
						{
							TraceLevel       = TraceLevel.Info,
							MapperExpression = MapperExpression,
							StartTime        = _startedOn,
							ExecutionTime    = _stopwatch.Elapsed
						});
					}
				}
			}

			public override string GetSqlText()
			{
				SetCommand(true);

				var sqlProvider = _dataConnection.DataProvider.CreateSqlBuilder(_dataConnection.MappingSchema, _dataConnection.Options);

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
					var queryCommand = _executionQuery.PreparedQuery.Commands[index];
					sqlProvider.PrintParameters(_dataConnection, sb, _executionQuery.CommandsParameters[index]);

					sb.AppendLine(queryCommand.Command);

					if (isFirst && _executionQuery.PreparedQuery.QueryHints != null)
					{
						isFirst = false;

						while (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r')
							sb.Length--;

						sb.AppendLine();

						var sql = sb.ToString();

						var sqlBuilder = _dataConnection.DataProvider.CreateSqlBuilder(_dataConnection.MappingSchema, _dataConnection.Options);
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
					_dataConnection.OnTraceConnection(new TraceInfo(_dataConnection, TraceInfoStep.Completed, TraceOperation.DisposeQuery, _isAsync)
					{
						TraceLevel       = TraceLevel.Info,
						Command          = _dataReader?.Command ?? _dataConnection.CurrentCommand,
						MapperExpression = MapperExpression,
						StartTime        = _startedOn,
						ExecutionTime    = _stopwatch.Elapsed,
						RecordsAffected  = RowsCount
					});
				}

				_dataReader = null;

				base.Dispose();
			}

#if !NATIVE_ASYNC
			public override Task DisposeAsync()
#else
			public override async ValueTask DisposeAsync()
#endif
			{
				if (_executionScope != null)
#if !NATIVE_ASYNC
					_executionScope.Dispose();
#else
					await _executionScope.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
#endif

				if (_dataConnection.TraceSwitchConnection.TraceInfo)
				{
					_dataConnection.OnTraceConnection(new TraceInfo(_dataConnection, TraceInfoStep.Completed, TraceOperation.DisposeQuery, _isAsync)
					{
						TraceLevel       = TraceLevel.Info,
						Command          = _dataConnection.CurrentCommand,
						MapperExpression = MapperExpression,
						StartTime        = _startedOn,
						ExecutionTime    = _stopwatch.Elapsed,
						RecordsAffected  = RowsCount
					});
				}

#if !NATIVE_ASYNC
				return base.DisposeAsync();
#else
				await base.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
#endif
			}

			public sealed class CommandWithParameters
			{
				public CommandWithParameters(string command, SqlParameter[] sqlParameters)
				{
					Command = command;
					SqlParameters = sqlParameters;
				}

				public string              Command       { get; }
				public SqlParameter[]      SqlParameters { get; }
			}

			public sealed class PreparedQuery
			{
				public CommandWithParameters[]      Commands      = null!;
				public SqlStatement                 Statement     = null!;
				public IReadOnlyCollection<string>? QueryHints;
			}

			public sealed class ExecutionPreparedQuery
			{
				public ExecutionPreparedQuery(PreparedQuery preparedQuery, DbParameter[]?[] commandsParameters)
				{
					PreparedQuery      = preparedQuery;
					CommandsParameters = commandsParameters;
				}

				public readonly PreparedQuery         PreparedQuery;
				public readonly DbParameter[]?[] CommandsParameters;
			}

			ExecutionPreparedQuery? _executionQuery;

			static ExecutionPreparedQuery CreateExecutionQuery(
				DataConnection            dataConnection,
				IQueryContext             context,
				IReadOnlyParameterValues? parameterValues,
				bool                      forGetSqlText)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, forGetSqlText);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues, forGetSqlText);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);
				return executionQuery;
			}

			static PreparedQuery GetCommand(DataConnection dataConnection, IQueryContext query, IReadOnlyParameterValues? parameterValues, bool forGetSqlText, int startIndent = 0)
			{
				if (query.Context != null)
				{
					return new PreparedQuery
					{
						Commands   = (CommandWithParameters[])query.Context,
						Statement  = query.Statement,
						QueryHints = dataConnection.GetNextCommandHints(!forGetSqlText),
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

				var sqlBuilder   = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema, dataConnection.Options);
				var sqlOptimizer = dataConnection.DataProvider.GetSqlOptimizer (dataConnection.Options);

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
					Commands   = commands,
					Statement  = sql,
					QueryHints = dataConnection.GetNextCommandHints(!forGetSqlText)
				};
			}

			static DbParameter[]?[] GetParameters(DataConnection dataConnection, PreparedQuery pq, IReadOnlyParameterValues? parameterValues, bool forGetSqlText)
			{
				var result = new DbParameter[pq.Commands.Length][];

				DbCommand? dbCommand = null;

				try
				{
					for (var index = 0; index < pq.Commands.Length; index++)
					{
						var command = pq.Commands[index];
						if (command.SqlParameters.Length == 0)
							continue;

						var parms = new DbParameter[command.SqlParameters.Length];

						for (var i = 0; i < command.SqlParameters.Length; i++)
						{
							var sqlp = command.SqlParameters[i];

							dbCommand ??= forGetSqlText
								? dataConnection.EnsureConnection(false).CreateCommand()
								: dataConnection.GetOrCreateCommand();

							parms[i] = CreateParameter(dataConnection, dbCommand, sqlp, sqlp.GetParameterValue(parameterValues), forGetSqlText);
						}

						result[index] = parms;
					}
				}
				finally
				{
					if (forGetSqlText)
						dbCommand?.Dispose();
				}

				return result;
			}

			static DbParameter CreateParameter(DataConnection dataConnection, DbCommand command, SqlParameter parameter, SqlParameterValue parmValue, bool forGetSqlText)
			{
				var p          = command.CreateParameter();
				var dbDataType = parmValue.DbDataType;
				var paramValue = parameter.CorrectParameterValue(parmValue.ProviderValue);

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

			protected override void SetQuery(IReadOnlyParameterValues parameterValues, bool forGetSqlText)
			{
				_executionQuery = CreateExecutionQuery(_dataConnection, Query.Queries[QueryNumber], parameterValues, forGetSqlText);
			}

			void SetCommand()
			{
				SetCommand(false);
				InitFirstCommand(_dataConnection, _executionQuery!);
			}

			#region ExecuteNonQuery

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task<int> ExecuteNonQueryImplAsync(
				DataConnection         dataConnection,
				ExecutionPreparedQuery executionQuery,
				CancellationToken      cancellationToken)
			{
				if (executionQuery.PreparedQuery.Commands.Length == 1)
				{
					InitFirstCommand(dataConnection, executionQuery);

					return await dataConnection.ExecuteNonQueryAsync(cancellationToken)
						.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				}

				var rowsAffected = -1;

				for (var i = 0; i < executionQuery.PreparedQuery.Commands.Length; i++)
				{
					InitCommand(dataConnection, executionQuery, i);

					if (i < executionQuery.PreparedQuery.Commands.Length - 1 && executionQuery.PreparedQuery.Commands[i].Command.StartsWith("DROP"))
					{
						try
						{
							await dataConnection.ExecuteNonQueryAsync(cancellationToken)
								.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
						}
						catch (Exception)
						{
						}
					}
					else
					{
						var n = await dataConnection.ExecuteNonQueryAsync(cancellationToken)
							.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

						if (i == 0)
							rowsAffected = n;
					}
				}

				return rowsAffected;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
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
				SetCommand(false);
				return ExecuteNonQueryImpl(_dataConnection, _executionQuery!);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static async Task<int> ExecuteNonQueryAsync(
				DataConnection            dataConnection,
				IQueryContext             context,
				IReadOnlyParameterValues? parameterValues,
				CancellationToken         cancellationToken)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, false);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues, false);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);

				return await ExecuteNonQueryImplAsync(dataConnection, executionQuery, cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static int ExecuteNonQuery(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, false);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues, false);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);

				return ExecuteNonQueryImpl(dataConnection, executionQuery);
			}

			#endregion

			#region ExecuteScalar

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static async Task<object?> ExecuteScalarImplAsync(
				DataConnection         dataConnection,
				ExecutionPreparedQuery executionQuery,
				CancellationToken      cancellationToken)
			{
				var idParam = GetIdentityParameter(dataConnection, executionQuery);

				if (executionQuery.PreparedQuery.Commands.Length == 1)
				{
					if (idParam != null)
					{
						// This is because the firebird provider does not return any parameters via ExecuteReader
						// the rest of the providers must support this mode
						await dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

						return idParam.Value;
					}

					return await dataConnection.ExecuteScalarDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				}

				await dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				InitCommand(dataConnection, executionQuery, 1);

				return await dataConnection.ExecuteScalarDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			static object? ExecuteScalarImpl(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				var idParam = GetIdentityParameter(dataConnection, executionQuery);

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

			private static DbParameter? GetIdentityParameter(DataConnection dataConnection, ExecutionPreparedQuery executionQuery)
			{
				DbParameter? idParam = null;

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

				return idParam;
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static Task<object?> ExecuteScalarAsync(
				DataConnection            dataConnection,
				IQueryContext             context,
				IReadOnlyParameterValues? parameterValues,
				CancellationToken         cancellationToken)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, false);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues, false);
				var executionQuery     = new ExecutionPreparedQuery(preparedQuery, commandsParameters);

				InitFirstCommand(dataConnection, executionQuery);

				return ExecuteScalarImplAsync(dataConnection, executionQuery, cancellationToken);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static object? ExecuteScalar(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var preparedQuery      = GetCommand(dataConnection, context, parameterValues, false);
				var commandsParameters = GetParameters(dataConnection, preparedQuery, parameterValues, false);
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
			static void InitCommand(DataConnection dataConnection, CommandWithParameters queryCommand, DbParameter[]? dbParameters, IReadOnlyCollection<string>? queryHints)
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

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static Task<DataReaderWrapper> ExecuteReaderAsync(
				DataConnection            dataConnection,
				IQueryContext             context,
				IReadOnlyParameterValues? parameterValues,
				CancellationToken         cancellationToken)
			{
				var executionQuery = CreateExecutionQuery(dataConnection, context, parameterValues, false);

				InitFirstCommand(dataConnection, executionQuery);

				return dataConnection.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
			public static DataReaderWrapper ExecuteReader(DataConnection dataConnection, IQueryContext context, IReadOnlyParameterValues? parameterValues)
			{
				var executionQuery = CreateExecutionQuery(dataConnection, context, parameterValues, false);

				InitFirstCommand(dataConnection, executionQuery);

				return dataConnection.ExecuteReader();
			}

			public override DataReaderWrapper ExecuteReader()
			{
				SetCommand(false);

				InitFirstCommand(_dataConnection, _executionQuery!);

				return _dataReader = _dataConnection.ExecuteReader();
			}

			#endregion

			sealed class DataReaderAsync : IDataReaderAsync
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
#elif NATIVE_ASYNC
				public ValueTask DisposeAsync()
				{
					Dispose();
					return default;
				}
#else
				public Task DisposeAsync()
				{
					Dispose();
					return TaskEx.CompletedTask;
				}
#endif
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				await _dataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				base.SetCommand(false);

				InitFirstCommand(_dataConnection, _executionQuery!);

				_dataReader = await _dataConnection.ExecuteDataReaderAsync(_dataConnection.GetCommandBehavior(CommandBehavior.Default), cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return new DataReaderAsync(_dataReader);
			}

			public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				await _dataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				base.SetCommand(false);

				if (_executionQuery!.PreparedQuery.Commands.Length == 1)
				{
					InitFirstCommand(_dataConnection, _executionQuery);

					return await _dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				}

				for (var i = 0; i < _executionQuery.PreparedQuery.Commands.Length; i++)
				{
					InitCommand(_dataConnection, _executionQuery, i);

					if (i < _executionQuery.PreparedQuery.Commands.Length - 1 && _executionQuery.PreparedQuery.Commands[i].Command.StartsWith("DROP"))
					{
						try
						{
							await _dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
						}
						catch
						{
						}
					}
					else
					{
						await _dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					}
				}

				return -1;
			}

			public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				await _dataConnection.EnsureConnectionAsync(cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				SetCommand();

				DbParameter? idparam = null;

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
						await _dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

						return idparam.Value;
					}

					return await _dataConnection.ExecuteScalarDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				}

				await _dataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				InitCommand(_dataConnection, _executionQuery, 1);

				return await _dataConnection.ExecuteScalarDataAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}
	}
}
