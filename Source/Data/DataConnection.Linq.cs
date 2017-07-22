using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Data
{
	using DataProvider;
	using Linq;
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	public partial class DataConnection : IDataContext
	{
		public ITable<T> GetTable<T>()
			where T : class
		{
			return new Table<T>(this);
		}

		public ITable<T> GetTable<T>(object instance, MethodInfo methodInfo, params object[] parameters)
			where T : class
		{
			return DataExtensions.GetTable<T>(this, instance, methodInfo, parameters);
		}

		internal class PreparedQuery
		{
			public string[]           Commands;
			public List<SqlParameter> SqlParameters;
			public IDbDataParameter[] Parameters;
			public SelectQuery        SelectQuery;
			public ISqlBuilder        SqlProvider;
			public List<string>       QueryHints;
		}

		#region SetQuery

		internal PreparedQuery GetCommand(IQueryContext query)
		{
			if (query.Context != null)
			{
				return new PreparedQuery
				{
					Commands      = (string[])query.Context,
					SqlParameters = query.SelectQuery.Parameters,
					SelectQuery   = query.SelectQuery,
					QueryHints    = query.QueryHints,
				 };
			}

			var sql    = query.SelectQuery.ProcessParameters(MappingSchema);
			var newSql = ProcessQuery(sql);

			if (!object.ReferenceEquals(sql, newSql))
			{
				sql = newSql;
				sql.IsParameterDependent = true;
			}

			var sqlProvider = DataProvider.CreateSqlBuilder();

			var cc = sqlProvider.CommandCount(sql);
			var sb = new StringBuilder();

			var commands = new string[cc];

			for (var i = 0; i < cc; i++)
			{
				sb.Length = 0;

				sqlProvider.BuildSql(i, sql, sb);
				commands[i] = sb.ToString();
			}

			if (!query.SelectQuery.IsParameterDependent)
				query.Context = commands;

			return new PreparedQuery
			{
				Commands      = commands,
				SqlParameters = sql.Parameters,
				SelectQuery   = sql,
				SqlProvider   = sqlProvider,
				QueryHints    = query.QueryHints,
			};
		}

		protected virtual SelectQuery ProcessQuery(SelectQuery selectQuery)
		{
			return selectQuery;
		}

		void GetParameters(IQueryContext query, PreparedQuery pq)
		{
			var parameters = query.GetParameters();

			if (parameters.Length == 0 && pq.SqlParameters.Count == 0)
				return;

			var ordered = DataProvider.SqlProviderFlags.IsParameterOrderDependent;
			var c       = ordered ? pq.SqlParameters.Count : parameters.Length;
			var parms   = new List<IDbDataParameter>(c);

			if (ordered)
			{
				for (var i = 0; i < pq.SqlParameters.Count; i++)
				{
					var sqlp = pq.SqlParameters[i];

					if (sqlp.IsQueryParameter)
					{
						var parm = parameters.Length > i && object.ReferenceEquals(parameters[i], sqlp) ? parameters[i] : parameters.First(p => object.ReferenceEquals(p, sqlp));
						AddParameter(parms, parm.Name, parm);
					}
				}
			}
			else
			{
				foreach (var parm in parameters)
				{
					if (parm.IsQueryParameter && pq.SqlParameters.Contains(parm))
						AddParameter(parms, parm.Name, parm);
				}
			}

			pq.Parameters = parms.ToArray();
		}

		void AddParameter(ICollection<IDbDataParameter> parms, string name, SqlParameter parm)
		{
			var p         = Command.CreateParameter();
			var dataType  = parm.DataType;
			var parmValue = parm.Value;

			if (dataType == DataType.Undefined)
			{
				dataType = MappingSchema.GetDataType(
					parm.SystemType == typeof(object) && parmValue != null ?
						parmValue.GetType() :
						parm.SystemType).DataType;
			}

			DataProvider.SetParameter(p, name, dataType, parmValue);

			parms.Add(p);
		}

		#endregion

		#region ExecuteXXX

		int IDataContext.ExecuteNonQuery(object query)
		{
			ThrowOnDisposed();

			var pq = (PreparedQuery)query;

			if (pq.Commands.Length == 1)
			{
				InitCommand(CommandType.Text, pq.Commands[0], null, pq.QueryHints);

				if (pq.Parameters != null)
					foreach (var p in pq.Parameters)
						Command.Parameters.Add(p);

				return ExecuteNonQuery();
			}
			else
			{
				for (var i = 0; i < pq.Commands.Length; i++)
				{
					InitCommand(CommandType.Text, pq.Commands[i], null, i == 0 ? pq.QueryHints : null);

					if (i == 0 && pq.Parameters != null)
						foreach (var p in pq.Parameters)
							Command.Parameters.Add(p);

					if (i < pq.Commands.Length - 1 && pq.Commands[i].StartsWith("DROP"))
					{
						try
						{
							ExecuteNonQuery();
						}
						catch (Exception)
						{
						}
					}
					else
					{
						ExecuteNonQuery();
					}
				}

				return -1;
			}
		}

		object IDataContext.ExecuteScalar(object query)
		{
			ThrowOnDisposed();

			var pq = (PreparedQuery)query;

			InitCommand(CommandType.Text, pq.Commands[0], null, pq.QueryHints);

			if (pq.Parameters != null)
				foreach (var p in pq.Parameters)
					Command.Parameters.Add(p);

			IDbDataParameter idparam = null;

			if (DataProvider.SqlProviderFlags.IsIdentityParameterRequired)
			{
				var sql = pq.SelectQuery;

				if (sql.IsInsert && sql.Insert.WithIdentity)
				{
					idparam = Command.CreateParameter();

					idparam.ParameterName = "IDENTITY_PARAMETER";
					idparam.Direction     = ParameterDirection.Output;
					idparam.Direction     = ParameterDirection.Output;
					idparam.DbType        = DbType.Decimal;

					Command.Parameters.Add(idparam);
				}
			}

			if (pq.Commands.Length == 1)
			{
				if (idparam != null)
				{
					// так сделано потому, что фаерберд провайдер не возвращает никаких параметров через ExecuteReader
					// остальные провайдеры должны поддерживать такой режим
					ExecuteNonQuery();

					return idparam.Value;
				}

				return ExecuteScalar();
			}

			ExecuteNonQuery();

			InitCommand(CommandType.Text, pq.Commands[1], null, null);

			return ExecuteScalar();
		}

		IDataReader IDataContext.ExecuteReader(object query)
		{
			ThrowOnDisposed();

			var pq = (PreparedQuery)query;

			InitCommand(CommandType.Text, pq.Commands[0], null, pq.QueryHints);

			if (pq.Parameters != null)
				foreach (var p in pq.Parameters)
					Command.Parameters.Add(p);

			return ExecuteReader();
		}

		void IDataContext.ReleaseQuery(object query)
		{
			ThrowOnDisposed();
		}

		#endregion

		#region GetSqlText

		string IDataContext.GetSqlText(object query)
		{
			ThrowOnDisposed();

			var pq = (PreparedQuery)query;

			var sqlProvider = pq.SqlProvider ?? DataProvider.CreateSqlBuilder();

			var sb = new StringBuilder();

			sb.Append("-- ").Append(ConfigurationString);

			if (ConfigurationString != DataProvider.Name)
				sb.Append(' ').Append(DataProvider.Name);

			if (DataProvider.Name != sqlProvider.Name)
				sb.Append(' ').Append(sqlProvider.Name);

			sb.AppendLine();

			sqlProvider.PrintParameters(sb, pq.Parameters);

			var isFirst = true;

			foreach (var command in pq.Commands)
			{
				sb.AppendLine(command);

				if (isFirst && pq.QueryHints != null && pq.QueryHints.Count > 0)
				{
					isFirst = false;

					while (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r')
						sb.Length--;

					sb.AppendLine();

					var sql = sb.ToString();

					var sqlBuilder = DataProvider.CreateSqlBuilder();
					sql = sqlBuilder.ApplyQueryHints(sql, pq.QueryHints);

					sb = new StringBuilder(sql);
				}
			}

			while (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r')
				sb.Length--;

			sb.AppendLine();

			return sb.ToString();
		}

		#endregion

		#region IDataContext Members

		SqlProviderFlags IDataContext.SqlProviderFlags { get { return DataProvider.SqlProviderFlags; } }
		Type             IDataContext.DataReaderType   { get { return DataProvider.DataReaderType;   } }

		bool             IDataContext.CloseAfterUse    { get; set; }

		Expression IDataContext.GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			return DataProvider.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
		}

		bool? IDataContext.IsDBNullAllowed(IDataReader reader, int idx)
		{
			return DataProvider.IsDBNullAllowed(reader, idx);
		}

		object IDataContext.SetQuery(IQueryContext queryContext)
		{
			ThrowOnDisposed();

			var query = GetCommand(queryContext);

			GetParameters(queryContext, query);

//			if (TraceSwitch.TraceInfo)
//				WriteTraceLine(((IDataContext)this).GetSqlText(query).Replace("\r", ""), TraceSwitch.DisplayName);

			return query;
		}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			ThrowOnDisposed();

			if (forNestedQuery && _connection != null && IsMarsEnabled)
				return new DataConnection(DataProvider, _connection) { _mappingSchema = _mappingSchema, Transaction = Transaction };

			return (DataConnection)Clone();
		}

		string IDataContext.ContextID
		{
			get { return DataProvider.Name; }
		}

		static Func<ISqlBuilder> GetCreateSqlProvider(IDataProvider dp)
		{
			return dp.CreateSqlBuilder;
		}

		Func<ISqlBuilder> IDataContext.CreateSqlProvider
		{
			get { return GetCreateSqlProvider(DataProvider); }
		}

		static Func<ISqlOptimizer> GetGetSqlOptimizer(IDataProvider dp)
		{
			return dp.GetSqlOptimizer;
		}

		Func<ISqlOptimizer> IDataContext.GetSqlOptimizer
		{
			get { return GetGetSqlOptimizer(DataProvider); }
		}

		#endregion
	}
}
