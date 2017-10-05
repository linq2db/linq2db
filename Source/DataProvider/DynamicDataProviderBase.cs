using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	using Configuration;
	using Extensions;
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

		protected abstract void OnConnectionTypeCreated(Type connectionType);

		protected void EnsureConnection()
		{
			GetConnectionType();
		}

		volatile Type _connectionType;

		protected Type GetConnectionType()
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

		public override bool IsCompatibleConnection(IDbConnection connection)
		{
			return GetConnectionType().IsSameOrParentOf(Proxy.GetUnderlyingObject((DbConnection)connection).GetType());
		}

		private         Type _dataReaderType;
		public override Type  DataReaderType => _dataReaderType ?? (_dataReaderType = Type.GetType(DataReaderTypeName, true));

		Func<string,IDbConnection> _createConnection;

		protected override IDbConnection CreateConnectionInternal(string connectionString)
		{
			if (_createConnection == null)
			{
				var l = CreateConnectionExpression(GetConnectionType());
				_createConnection = l.Compile();
			}

			return _createConnection(connectionString);
		}

		public static Expression<Func<string,IDbConnection>> CreateConnectionExpression(Type connectionType)
		{
			var p = Expression.Parameter(typeof(string));
			var l = Expression.Lambda<Func<string,IDbConnection>>(
				Expression.New(connectionType.GetConstructorEx(new[] { typeof(string) }), p),
				p);

			return l;
		}

		#region Expression Helpers

		protected Action<IDbDataParameter> GetSetParameter(
			Type connectionType,
			//   ((FbParameter)parameter).   FbDbType =           FbDbType.          TimeStamp;
			string parameterTypeName, string propertyName, string dbTypeName, string valueName)
		{
			var pType  = connectionType.AssemblyEx().GetType(parameterTypeName.Contains(".") ? parameterTypeName : connectionType.Namespace + "." + parameterTypeName, true);
			var dbType = connectionType.AssemblyEx().GetType(dbTypeName.       Contains(".") ? dbTypeName        : connectionType.Namespace + "." + dbTypeName,        true);
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

		protected Func<IDbDataParameter,bool> IsGetParameter(
			Type connectionType,
			//   ((FbParameter)parameter).   FbDbType =           FbDbType.          TimeStamp;
			string parameterTypeName, string propertyName, string dbTypeName, string valueName)
		{
			var pType  = connectionType.AssemblyEx().GetType(parameterTypeName.Contains(".") ? parameterTypeName : connectionType.Namespace + "." + parameterTypeName, true);
			var dbType = connectionType.AssemblyEx().GetType(dbTypeName.       Contains(".") ? dbTypeName        : connectionType.Namespace + "." + dbTypeName,        true);
			var value  = Enum.Parse(dbType, valueName);

			var p = Expression.Parameter(typeof(IDbDataParameter));
			var l = Expression.Lambda<Func<IDbDataParameter,bool>>(
				Expression.Equal(
					Expression.PropertyOrField(
						Expression.Convert(p, pType),
						propertyName),
					Expression.Constant(value)),
				p);

			return l.Compile();
		}

		// SetField<IfxDataReader,Int64>("BIGINT", (r,i) => r.GetBigInt(i));
		//
		// protected void SetField<TP,T>(string dataTypeName, Expression<Func<TP,int,T>> expr)
		// {
		//     ReaderExpressions[new ReaderInfo { FieldType = typeof(T), DataTypeName = dataTypeName }] = expr;
		// }
		protected void SetField(Type fieldType, string dataTypeName, string methodName)
		{
			var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
			var indexParameter      = Expression.Parameter(typeof(int),    "i");

			ReaderExpressions[new ReaderInfo { FieldType = fieldType, DataTypeName = dataTypeName }] =
				Expression.Lambda(
					Expression.Call(dataReaderParameter, methodName, null, indexParameter),
					dataReaderParameter,
					indexParameter);
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
			var methodCall          = Expression.Call(dataReaderParameter, methodName, null, indexParameter) as Expression;

			if (methodCall.Type != toType)
				methodCall = Expression.Convert(methodCall, toType);

			ReaderExpressions[new ReaderInfo { ToType = toType, ProviderFieldType = fieldType }] =
				Expression.Lambda(methodCall, dataReaderParameter, indexParameter);
		}

		#endregion
	}
}
