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
	using SqlBuilder;
	using SqlProvider;

	public partial class DataConnection : IDataContext
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
			return DataExtensions.GetTable<T>(this, instance, methodInfo, parameters);
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

			var sql    = query.SqlQuery.ProcessParameters();
			var newSql = ProcessQuery(sql);

			if (!object.ReferenceEquals(sql, newSql))
			{
				sql = newSql;
				sql.IsParameterDependent = true;
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

			if (!query.SqlQuery.IsParameterDependent)
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

			var ordered = DataProvider.SqlProviderFlags.IsParameterOrderDependent;
			var c       = ordered ? pq.SqlParameters.Count : parameters.Length;

			List<IDbDataParameter> parms = new List<IDbDataParameter>(c);

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
			var p = Command.CreateParameter();

			DataType dataType;

			switch (parm.DbType)
			{
				case DbType.AnsiString            : dataType = DataType.VarChar;        break;
				case DbType.Binary                : dataType = DataType.VarBinary;      break;
				case DbType.Byte                  : dataType = DataType.Byte;           break;
				case DbType.Boolean               : dataType = DataType.Boolean;        break;
				case DbType.Currency              : dataType = DataType.Money;          break;
				case DbType.Date                  : dataType = DataType.Date;           break;
				case DbType.DateTime              : dataType = DataType.DateTime;       break;
				case DbType.Decimal               : dataType = DataType.Decimal;        break;
				case DbType.Double                : dataType = DataType.Double;         break;
				case DbType.Guid                  : dataType = DataType.Guid;           break;
				case DbType.Int16                 : dataType = DataType.Int16;          break;
				case DbType.Int32                 : dataType = DataType.Int32;          break;
				case DbType.Int64                 : dataType = DataType.Int64;          break;
				case DbType.SByte                 : dataType = DataType.SByte;          break;
				case DbType.Single                : dataType = DataType.Single;         break;
				case DbType.String                : dataType = DataType.NVarChar;       break;
				case DbType.Time                  : dataType = DataType.Time;           break;
				case DbType.UInt16                : dataType = DataType.UInt16;         break;
				case DbType.UInt32                : dataType = DataType.UInt32;         break;
				case DbType.UInt64                : dataType = DataType.UInt64;         break;
				case DbType.VarNumeric            : dataType = DataType.VarNumeric;     break;
				case DbType.AnsiStringFixedLength : dataType = DataType.Char;           break;
				case DbType.StringFixedLength     : dataType = DataType.NChar;          break;
				case DbType.Xml                   : dataType = DataType.Xml;            break;
				case DbType.DateTime2             : dataType = DataType.DateTime2;      break;
				case DbType.DateTimeOffset        : dataType = DataType.DateTimeOffset; break;
				default :
					dataType = MappingSchema.GetDataType(
						parm.SystemType == typeof(object) && parm.Value != null ?
							parm.Value.GetType() :
							parm.SystemType);
					break;
			}

			DataProvider.SetParameter(p, name, dataType, parm.Value);

			parms.Add(p);
		}

		#endregion

		#region ExecuteXXX

		int IDataContext.ExecuteNonQuery(object query)
		{
			var pq = (PreparedQuery)query;

			SetCommand(pq.Commands[0]);

			if (pq.Parameters != null)
				foreach (var p in pq.Parameters)
					Command.Parameters.Add(p);

			if (TraceSwitch.TraceInfo)
			{
				var now = DateTime.Now;
				var n   = Command.ExecuteNonQuery();

				WriteTraceLine(string.Format("Execution time: {0}. Records affected: {1}.\r\n", DateTime.Now - now, n), TraceSwitch.DisplayName);

				return n;
			}

			return Command.ExecuteNonQuery();
		}

		object IDataContext.ExecuteScalar(object query)
		{
			if (TraceSwitch.TraceInfo)
			{
				var now = DateTime.Now;
				var ret = ExecuteScalarInternal(query);

				WriteTraceLine(string.Format("Execution time: {0}\r\n", DateTime.Now - now), TraceSwitch.DisplayName);

				return ret;
			}

			return ExecuteScalarInternal(query);
		}

		object ExecuteScalarInternal(object query)
		{
			var pq = (PreparedQuery)query;

			SetCommand(pq.Commands[0]);

			if (pq.Parameters != null)
				foreach (var p in pq.Parameters)
					Command.Parameters.Add(p);

			IDbDataParameter idparam = null;

			if (DataProvider.SqlProviderFlags.IsIdentityParameterRequired)
			{
				var sql = pq.SqlQuery;

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
					Command.ExecuteNonQuery(); // так сделано потому, что фаерберд провайдер не возвращает никаких параметров через ExecuteReader
					                           // остальные провайдеры должны поддерживать такой режим
					return idparam.Value;
				}

				return Command.ExecuteScalar();
			}

			Command.ExecuteNonQuery();

			SetCommand(pq.Commands[1]);

			return Command.ExecuteScalar();
		}

		IDataReader IDataContext.ExecuteReader(object query)
		{
			var pq = (PreparedQuery)query;

			SetCommand(pq.Commands[0]);

			if (pq.Parameters != null)
				foreach (var p in pq.Parameters)
					Command.Parameters.Add(p);

			if (TraceSwitch.TraceInfo)
			{
				var now = DateTime.Now;
				var ret = Command.ExecuteReader();

				WriteTraceLine(string.Format("Execution time: {0}\r\n", DateTime.Now - now), TraceSwitch.DisplayName);

				return ret;
			}

			return Command.ExecuteReader();
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

		SqlProviderFlags IDataContext.SqlProviderFlags { get { return DataProvider.SqlProviderFlags; } }
		Type             IDataContext.DataReaderType   { get { return DataProvider.DataReaderType;   } }

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
			var query = GetCommand(queryContext);

			GetParameters(queryContext, query);

			if (TraceSwitch.TraceInfo)
				WriteTraceLine(((IDataContext)this).GetSqlText(query).Replace("\r", ""), TraceSwitch.DisplayName);

			return query;
		}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			if (forNestedQuery && _connection != null && IsMarsEnabled)
				return new DataConnection(DataProvider, _connection) { _mappingSchema = _mappingSchema, Transaction = Transaction };

			return (DataConnection)Clone();
		}

		string IDataContext.ContextID
		{
			get { return DataProvider.Name; }
		}

		static Func<ISqlProvider> GetCreateSqlProvider(IDataProvider dp)
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
