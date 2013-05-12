using System;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public abstract class DynamicDataProviderBase : DataProviderBase
	{
		protected DynamicDataProviderBase(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
		}

		protected abstract string ConnectionTypeName { get; }
		protected abstract string DataReaderTypeName { get; }

		static readonly object _sync = new object();

		protected virtual void OnConnectionTypeCreated(Type connectionType)
		{
		}

		protected void EnsureConnection()
		{
			GetConnectionType();
		}

		volatile Type _connectionType;

		private Type GetConnectionType()
		{
			if (_connectionType == null)
				lock (_sync)
					if (_connectionType == null)
					{
						var connectionType = Type.GetType(ConnectionTypeName, true);

						OnConnectionTypeCreated(connectionType);

						_connectionType = connectionType;
					}

			return _connectionType;
		}

		private         Type _dataReaderType;
		public override Type  DataReaderType
		{
			get { return _dataReaderType ?? (_dataReaderType = Type.GetType(DataReaderTypeName, true)); }
		}

		Func<string,IDbConnection> _createConnection;

		public override IDbConnection CreateConnection(string connectionString)
		{
			if (_createConnection == null)
			{
				var p = Expression.Parameter(typeof(string));
				var l = Expression.Lambda<Func<string,IDbConnection>>(
					Expression.New(GetConnectionType().GetConstructor(new[] { typeof(string) }), p),
					p);

				_createConnection = l.Compile();
			}

			return _createConnection(connectionString);
		}

		#region Expression Helpers

		protected Action<IDbDataParameter> GetSetParameter(
			Type connectionType,
			//   ((FbParameter)parameter).   FbDbType =           FbDbType.          TimeStamp;
			string parameterTypeName, string propertyName, string dbTypeName, string valueName)
		{
			var pType  = connectionType.Assembly.GetType(connectionType.Namespace + "." + parameterTypeName, true);
			var dbType = connectionType.Assembly.GetType(connectionType.Namespace + "." + dbTypeName, true);
			var value  = Enum.Parse(dbType, valueName);

			var p = Expression.Parameter(typeof(IDbDataParameter));
			var l = Expression.Lambda<Action<IDbDataParameter>>(
				Expression.Assign(
					Expression.PropertyOrField(
						Expression.Convert(p, pType),
						propertyName),
					Expression.Constant(value)),
				p);

			return l.Compile();
		}

		// SetProviderField<MySqlDataReader,MySqlDecimal> ((r,i) => r.GetMySqlDecimal (i));
		//
		// protected void SetProviderField<TP,T>(Expression<Func<TP,int,T>> expr)
		// {
		//     ReaderExpressions[new ReaderInfo { ProviderFieldType = typeof(T) }] = expr;
		// }
		protected void SetProviderField(Type fieldType, string methodName)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int),    "i");

			ReaderExpressions[new ReaderInfo { ProviderFieldType = fieldType }] =
				Expression.Lambda(
					Expression.Call(dataReaderParameter, methodName, null, indexParameter),
					dataReaderParameter,
					indexParameter);
		}

		// SetToTypeField<MySqlDataReader,MySqlDecimal> ((r,i) => r.GetMySqlDecimal (i));
		//
		// protected void SetToTypeField<TP,T>(Expression<Func<TP,int,T>> expr)
		// {
		//     ReaderExpressions[new ReaderInfo { ToType = typeof(T) }] = expr;
		// }
		protected void SetToTypeField(Type toType, string methodName)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int),    "i");

			ReaderExpressions[new ReaderInfo { ToType = toType }] =
				Expression.Lambda(
					Expression.Call(dataReaderParameter, methodName, null, indexParameter),
					dataReaderParameter,
					indexParameter);
		}

		// SetProviderField<OracleDataReader,OracleBFile,OracleBFile>((r,i) => r.GetOracleBFile(i));
		//
		// protected void SetProviderField<TP,T,TS>(Expression<Func<TP,int,T>> expr)
		// {
		//     ReaderExpressions[new ReaderInfo { ToType = typeof(T), ProviderFieldType = typeof(TS) }] = expr;
		// }
		protected void SetProviderField(Type toType, Type fieldType, string methodName)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int),    "i");

			ReaderExpressions[new ReaderInfo { ToType = toType, ProviderFieldType = fieldType }] =
				Expression.Lambda(
					Expression.Call(dataReaderParameter, methodName, null, indexParameter),
					dataReaderParameter,
					indexParameter);
		}

		#endregion
	}
}
