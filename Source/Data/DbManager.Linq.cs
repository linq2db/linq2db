using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

using LinqToDB.SqlProvider;

namespace LinqToDB.Data
{
	using DataProvider;
	using Linq;
	using Sql;

	public partial class DbManager : IDataContext
	{
		public Table<T> GetTable<T>()
			where T : class
		{
			return new Table<T>(this);
		}

		public Table<T> GetTable<T>(bool dispose)
			where T : class
		{
			return new Table<T>(new DataContextInfo(this, dispose));
		}

		public Table<T> GetTable<T>(object instance, MethodInfo methodInfo, params object[] parameters)
			where T : class
		{
			return Linq.Extensions.GetTable<T>(this, instance, methodInfo, parameters);
		}

		class PreparedQuery
		{
			public string[]           Commands;
			public List<SqlParameter> SqlParameters;
			public IDbDataParameter[] Parameters;
			public SqlQuery           SqlQuery;
			public ISqlProvider       SqlProvider;
		}

		#region SetQuery

		object IDataContext.SetQuery(IQueryContext queryContext)
		{
			var query = GetCommand(queryContext);

			GetParameters(queryContext, query);

			if (TraceSwitch.TraceInfo)
				WriteTraceLine(((IDataContext)this).GetSqlText(query), TraceSwitch.DisplayName);

			return query;
		}

		PreparedQuery GetCommand(IQueryContext query)
		{
			if (query.Context != null)
			{
				return new PreparedQuery
				{
					Commands      = (string[])query.Context,
					SqlParameters = query.SqlQuery.Parameters,
					SqlQuery      = query.SqlQuery
				 };
			}

			var sql = query.SqlQuery.ProcessParameters();

			var newSql = ProcessQuery(sql);

			if (sql != newSql)
			{
				sql = newSql;
				sql.ParameterDependent = true;
			}

			var sqlProvider = DataProvider.CreateSqlProvider();

			var cc = sqlProvider.CommandCount(sql);
			var sb = new StringBuilder();

			var commands = new string[cc];

			for (var i = 0; i < cc; i++)
			{
				sb.Length = 0;

				sqlProvider.BuildSql(i, sql, sb, 0, 0, false);
				commands[i] = sb.ToString();
			}

			if (!query.SqlQuery.ParameterDependent)
				query.Context = commands;

			return new PreparedQuery
			{
				Commands      = commands,
				SqlParameters = sql.Parameters,
				SqlQuery      = sql,
				SqlProvider   = sqlProvider
			};
		}

		protected virtual SqlQuery ProcessQuery(SqlQuery sqlQuery)
		{
			return sqlQuery;
		}

		void GetParameters(IQueryContext query, PreparedQuery pq)
		{
			var parameters = query.GetParameters();

			if (parameters.Length == 0 && pq.SqlParameters.Count == 0)
				return;

			var x = DataProvider.Convert("x", ConvertType.NameToQueryParameter).ToString();
			var y = DataProvider.Convert("y", ConvertType.NameToQueryParameter).ToString();

			var parms = new List<IDbDataParameter>(x == y ? pq.SqlParameters.Count : parameters.Length);

			if (x == y)
			{
				for (var i = 0; i < pq.SqlParameters.Count; i++)
				{
					var sqlp = pq.SqlParameters[i];

					if (sqlp.IsQueryParameter)
					{
						var parm = parameters.Length > i && parameters[i] == sqlp ? parameters[i] : parameters.First(p => p == sqlp);
						AddParameter(parms, x, parm);
					}
				}
			}
			else
			{
				foreach (var parm in parameters)
				{
					if (parm.IsQueryParameter && pq.SqlParameters.Contains(parm))
					{
						var name = DataProvider.Convert(parm.Name, ConvertType.NameToQueryParameter).ToString();
						AddParameter(parms, name, parm);
					}
				}
			}

			pq.Parameters = parms.ToArray();
		}

		void AddParameter(ICollection<IDbDataParameter> parms, string name, SqlParameter parm)
		{
			var value = MappingSchema.ConvertParameterValue(parm.Value, parm.SystemType);

			if (value != null)
			{
				parms.Add(Parameter(name, value));
			}
			else
			{
				var dataType = DataProvider.GetDbType(parm.SystemType);
				parms.Add(dataType == DbType.Object ? Parameter(name, value) : Parameter(name, null, dataType));
			}
		}

		#endregion

		#region ExecuteXXX

		int IDataContext.ExecuteNonQuery(object query)
		{
			var pq = (PreparedQuery)query;

			SetCommand(pq.Commands[0], pq.Parameters);

			var now = default(DateTime);

			if (TraceSwitch.TraceInfo)
				now = DateTime.Now;

			var n = ExecuteNonQuery();

			if (TraceSwitch.TraceInfo)
				WriteTraceLine(string.Format("Execution time: {0}. Records affected: {1}.\r\n", DateTime.Now - now, n), TraceSwitch.DisplayName);

			return n;
		}

		object IDataContext.ExecuteScalar(object query)
		{
			var now = default(DateTime);

			if (TraceSwitch.TraceInfo)
				now = DateTime.Now;

			var ret = ExecuteScalarInternal(query);

			if (TraceSwitch.TraceInfo)
				WriteTraceLine(string.Format("Execution time: {0}\r\n", DateTime.Now - now), TraceSwitch.DisplayName);

			return ret;
		}

		object ExecuteScalarInternal(object query)
		{
			var pq = (PreparedQuery)query;

			SetCommand(pq.Commands[0], pq.Parameters);

			IDbDataParameter idparam = null;

			if ((pq.SqlProvider ?? DataProvider.CreateSqlProvider()).IsIdentityParameterRequired)
			{
				var sql = pq.SqlQuery;

				if (sql.IsInsert && sql.Insert.WithIdentity)
				{
					var pname = DataProvider.Convert("IDENTITY_PARAMETER", ConvertType.NameToQueryParameter).ToString();
					idparam = OutputParameter(pname, DbType.Decimal);
					DataProvider.AttachParameter(Command, idparam);
				}
			}

			if (pq.Commands.Length == 1)
			{
				if (idparam != null)
				{
					ExecuteNonQuery(); // так сделано потому, что фаерберд провайдер не возвращает никаких параметров через ExecuteReader
					                   // остальные провайдеры должны поддерживать такой режим
					return idparam.Value;
				}

				return ExecuteScalar();
			}

			ExecuteNonQuery();

			return SetCommand(pq.Commands[1]).ExecuteScalar();
		}

		IDataReader IDataContext.ExecuteReader(object query)
		{
			var pq = (PreparedQuery)query;

			SetCommand(pq.Commands[0], pq.Parameters);

			var now = default(DateTime);

			if (TraceSwitch.TraceInfo)
				now = DateTime.Now;

			var ret = ExecuteReader();

			if (TraceSwitch.TraceInfo)
				WriteTraceLine(string.Format("Execution time: {0}\r\n", DateTime.Now - now), TraceSwitch.DisplayName);

			return ret;
		}

		void IDataContext.ReleaseQuery(object query)
		{
		}

		#endregion

		#region GetSqlText

		string IDataContext.GetSqlText(object query)
		{
			var pq = (PreparedQuery)query;

			var sqlProvider = pq.SqlProvider ?? DataProvider.CreateSqlProvider();

			var sb = new StringBuilder();

			sb.Append("-- ").Append(ConfigurationString);

			if (ConfigurationString != DataProvider.Name)
				sb.Append(' ').Append(DataProvider.Name);

			if (DataProvider.Name != sqlProvider.Name)
				sb.Append(' ').Append(sqlProvider.Name);

			sb.AppendLine();

			if (pq.Parameters != null && pq.Parameters.Length > 0)
			{
				foreach (var p in pq.Parameters)
					sb
						.Append("-- DECLARE ")
						.Append(p.ParameterName)
						.Append(' ')
						.Append(p.Value == null ? p.DbType.ToString() : p.Value.GetType().Name)
						.AppendLine();

				sb.AppendLine();

				foreach (var p in pq.Parameters)
				{
					var value = p.Value;

					if (value is string || value is char)
						value = "'" + value.ToString().Replace("'", "''") + "'";

					sb
						.Append("-- SET ")
						.Append(p.ParameterName)
						.Append(" = ")
						.Append(value)
						.AppendLine();
				}

				sb.AppendLine();
			}

			foreach (var command in pq.Commands)
				sb.AppendLine(command);

			while (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r')
				sb.Length--;

			sb.AppendLine();

			return sb.ToString();
		}

		#endregion

		#region IDataContext Members

		IDataContext IDataContext.Clone()
		{
			return Clone();
		}

		string IDataContext.ContextID
		{
			get { return DataProvider.Name; }
		}

		static Func<ISqlProvider> GetCreateSqlProvider(DataProviderBase dp)
		{
			return dp.CreateSqlProvider;
		}

		Func<ISqlProvider> IDataContext.CreateSqlProvider
		{
			get { return GetCreateSqlProvider(DataProvider); }
		}

		#endregion
	}
}
