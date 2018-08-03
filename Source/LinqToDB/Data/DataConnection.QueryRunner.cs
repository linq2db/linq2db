using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
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
		IQueryRunner IDataContext.GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters)
		{
			CheckAndThrowOnDisposed();
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

			bool       _isAsync;
			Expression _mapperExpression;

			public override Expression  MapperExpression
			{
				get => _mapperExpression;
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

			public override string GetSqlText()
			{
				SetCommand(false);

				var sqlProvider = _preparedQuery.SqlProvider ?? _dataConnection.DataProvider.CreateSqlBuilder();

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

						var sqlBuilder = _dataConnection.DataProvider.CreateSqlBuilder();
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

			public class PreparedQuery
			{
				public string[]           Commands;
				public List<SqlParameter> SqlParameters;
				public IDbDataParameter[] Parameters;
				public SqlStatement       Statement;
				public ISqlBuilder        SqlProvider;
				public List<string>       QueryHints;
			}

			PreparedQuery _preparedQuery;

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

				var sql    = query.Statement.ProcessParameters(dataConnection.MappingSchema);
				var newSql = dataConnection.ProcessQuery(sql);

				if (!object.ReferenceEquals(sql, newSql))
				{
					sql = newSql;
					sql.IsParameterDependent = true;
				}

				var sqlProvider = dataConnection.DataProvider.CreateSqlBuilder();

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
					query.Context = commands;

				return new PreparedQuery
				{
					Commands      = commands,
					SqlParameters = sql.Parameters,
					Statement     = sql,
					SqlProvider   = sqlProvider,
					QueryHints    = query.QueryHints,
				};
			}

			static void GetParameters(DataConnection dataConnection, IQueryContext query, PreparedQuery pq)
			{
				var parameters = query.GetParameters();

				if (parameters.Length == 0 && pq.SqlParameters.Count == 0)
					return;

				var ordered = dataConnection.DataProvider.SqlProviderFlags.IsParameterOrderDependent;
				var c       = ordered ? pq.SqlParameters.Count : parameters.Length;
				var parms   = new List<IDbDataParameter>(c);

				if (ordered)
				{
					for (var i = 0; i < pq.SqlParameters.Count; i++)
					{
						var sqlp = pq.SqlParameters[i];

						if (sqlp.IsQueryParameter)
						{
							var parm = parameters.Length > i && object.ReferenceEquals(parameters[i], sqlp) ?
								parameters[i] :
								parameters.First(p => object.ReferenceEquals(p, sqlp));
							AddParameter(dataConnection, parms, parm.Name, parm);
						}
					}
				}
				else
				{
					foreach (var parm in parameters)
					{
						if (parm.IsQueryParameter && pq.SqlParameters.Contains(parm))
							AddParameter(dataConnection, parms, parm.Name, parm);
					}
				}

				pq.Parameters = parms.ToArray();
			}

			static void AddParameter(DataConnection dataConnection, ICollection<IDbDataParameter> parms, string name, SqlParameter parm)
			{
				var p          = dataConnection.Command.CreateParameter();
				var systemType = parm.SystemType;
				var dataType   = parm.DataType;
				var paramValue = parm.Value;

				if (systemType == null)
				{
					if (paramValue != null)
						systemType = paramValue.GetType();
				}

				if (dataType == DataType.Undefined)
				{
					dataType = dataConnection.MappingSchema.GetDataType(
						parm.SystemType == typeof(object) && paramValue != null ?
							paramValue.GetType() :
							systemType).DataType;
				}


				dataConnection.DataProvider.SetParameter(p, name, new DbDataType(systemType, dataType, parm.DbType), paramValue);

				parms.Add(p);
			}

			public static PreparedQuery SetQuery(DataConnection dataConnection, IQueryContext queryContext, int startIndent = 0)
			{
				var preparedQuery = GetCommand(dataConnection, queryContext, startIndent);

				GetParameters(dataConnection, queryContext, preparedQuery);

				return preparedQuery;
			}

			protected override void SetQuery()
			{
				_preparedQuery = SetQuery(_dataConnection, Query.Queries[QueryNumber]);
			}

			void SetCommand()
			{
				SetCommand(true);

				_dataConnection.InitCommand(CommandType.Text, _preparedQuery.Commands[0], null, QueryHints);

				if (_preparedQuery.Parameters != null)
					foreach (var p in _preparedQuery.Parameters)
						_dataConnection.Command.Parameters.Add(p);
			}

			#region ExecuteNonQuery

			static int ExecuteNonQueryImpl(DataConnection dataConnection, PreparedQuery preparedQuery)
			{
				if (preparedQuery.Commands.Length == 1)
				{
					dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[0], null, preparedQuery.QueryHints);

					if (preparedQuery.Parameters != null)
						foreach (var p in preparedQuery.Parameters)
							dataConnection.Command.Parameters.Add(p);

					return dataConnection.ExecuteNonQuery();
				}

				var rowsAffected = -1;

				for (var i = 0; i < preparedQuery.Commands.Length; i++)
				{
					dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[i], null, i == 0 ? preparedQuery.QueryHints : null);

					if (i == 0 && preparedQuery.Parameters != null)
						foreach (var p in preparedQuery.Parameters)
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
				return ExecuteNonQueryImpl(_dataConnection, _preparedQuery);
			}

			public static int ExecuteNonQuery(DataConnection dataConnection, IQueryContext context)
			{
				var preparedQuery = GetCommand(dataConnection, context);

				GetParameters(dataConnection, context, preparedQuery);

				return ExecuteNonQueryImpl(dataConnection, preparedQuery);
			}

			#endregion

			#region ExecuteScalar

			static object ExecuteScalarImpl(DataConnection dataConnection, PreparedQuery preparedQuery)
			{
				IDbDataParameter idParam = null;

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

				dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[1], null, null);

				return dataConnection.ExecuteScalar();
			}

			public static object ExecuteScalar(DataConnection dataConnection, IQueryContext context)
			{
				var preparedQuery = GetCommand(dataConnection, context);

				GetParameters(dataConnection, context, preparedQuery);

				dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[0], null, preparedQuery.QueryHints);

				if (preparedQuery.Parameters != null)
					foreach (var p in preparedQuery.Parameters)
						dataConnection.Command.Parameters.Add(p);

				return ExecuteScalarImpl(dataConnection, preparedQuery);
			}

			public override object ExecuteScalar()
			{
				SetCommand();
				return ExecuteScalarImpl(_dataConnection, _preparedQuery);
			}

			#endregion

			#region ExecuteReader

			public static IDataReader ExecuteReader(DataConnection dataConnection, IQueryContext context)
			{
				var preparedQuery = GetCommand(dataConnection, context);

				GetParameters(dataConnection, context, preparedQuery);

				dataConnection.InitCommand(CommandType.Text, preparedQuery.Commands[0], null, preparedQuery.QueryHints);

				if (preparedQuery.Parameters != null)
					foreach (var p in preparedQuery.Parameters)
						dataConnection.Command.Parameters.Add(p);

				return dataConnection.ExecuteReader();
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
					_dataReader.Dispose();
				}
			}

			public override async Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				await _dataConnection.EnsureConnectionAsync(cancellationToken);

				base.SetCommand(true);

				_dataConnection.InitCommand(CommandType.Text, _preparedQuery.Commands[0], null, QueryHints);

				if (_preparedQuery.Parameters != null)
					foreach (var p in _preparedQuery.Parameters)
						_dataConnection.Command.Parameters.Add(p);

				var dataReader = await _dataConnection.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);

				return new DataReaderAsync(dataReader);
			}

			public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				await _dataConnection.EnsureConnectionAsync(cancellationToken);

				base.SetCommand(true);

				if (_preparedQuery.Commands.Length == 1)
				{
					_dataConnection.InitCommand(
						CommandType.Text, _preparedQuery.Commands[0], null, _preparedQuery.QueryHints);

					if (_preparedQuery.Parameters != null)
						foreach (var p in _preparedQuery.Parameters)
							_dataConnection.Command.Parameters.Add(p);

					return await _dataConnection.ExecuteNonQueryAsync(cancellationToken);
				}

				for (var i = 0; i < _preparedQuery.Commands.Length; i++)
				{
					_dataConnection.InitCommand(
						CommandType.Text, _preparedQuery.Commands[i], null, i == 0 ? _preparedQuery.QueryHints : null);

					if (i == 0 && _preparedQuery.Parameters != null)
						foreach (var p in _preparedQuery.Parameters)
							_dataConnection.Command.Parameters.Add(p);

					if (i < _preparedQuery.Commands.Length - 1 && _preparedQuery.Commands[i].StartsWith("DROP"))
					{
						try
						{
							await _dataConnection.ExecuteNonQueryAsync(cancellationToken);
						}
						catch
						{
						}
					}
					else
					{
						await _dataConnection.ExecuteNonQueryAsync(cancellationToken);
					}
				}

				return -1;
			}

			public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
			{
				_isAsync = true;

				await _dataConnection.EnsureConnectionAsync(cancellationToken);

				SetCommand();

				IDbDataParameter idparam = null;

				if (_dataConnection.DataProvider.SqlProviderFlags.IsIdentityParameterRequired)
				{
					if (_preparedQuery.Statement.NeedsIdentity())
					{
						idparam = _dataConnection.Command.CreateParameter();

						idparam.ParameterName = "IDENTITY_PARAMETER";
						idparam.Direction     = ParameterDirection.Output;
						idparam.DbType        = DbType.Decimal;

						_dataConnection.Command.Parameters.Add(idparam);
					}
				}

				if (_preparedQuery.Commands.Length == 1)
				{
					if (idparam != null)
					{
						// так сделано потому, что фаерберд провайдер не возвращает никаких параметров через ExecuteReader
						// остальные провайдеры должны поддерживать такой режим
						await _dataConnection.ExecuteNonQueryAsync(cancellationToken);

						return idparam.Value;
					}

					return await _dataConnection.ExecuteScalarAsync(cancellationToken);
				}

				await _dataConnection.ExecuteNonQueryAsync(cancellationToken);

				_dataConnection.InitCommand(CommandType.Text, _preparedQuery.Commands[1], null, null);

				return await _dataConnection.ExecuteScalarAsync(cancellationToken);
			}
		}
	}
}
