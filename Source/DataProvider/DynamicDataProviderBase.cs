using System;
using System.Data;
using System.Linq.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.DataProvider
{
	using Common;
	using Mapping;

	public abstract class DynamicDataProviderBase : DataProviderBase
	{
		protected DynamicDataProviderBase(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
		}

		protected void SetTypes(string assemblyName, string connectionTypeName, string dataReaderTypeName)
		{
			_connectionTypeName = "{0}.{1}, {0}".Args(assemblyName, connectionTypeName);
			_dataReaderTypeName = "{0}.{1}, {0}".Args(assemblyName, dataReaderTypeName);
		}

		protected void SetTypes(string connectionTypeName, string dataReaderTypeName)
		{
			_connectionTypeName = connectionTypeName;
			_dataReaderTypeName = dataReaderTypeName;
		}

		string _connectionTypeName;
		string _dataReaderTypeName;

		static readonly object _sync = new object();

		protected virtual void OnInitConnectionType()
		{
		}

		void CreateConnectionType()
		{
			lock (_sync)
			{
				if (_connectionType == null)
				{
					_connectionType = Type.GetType(_connectionTypeName, true);
					OnInitConnectionType();
				}
			}
		}

		private         Type _connectionType;
		public override Type  ConnectionType
		{
			get
			{
				if (_connectionType == null)
					CreateConnectionType();
				return _connectionType;
			}
		}

		private         Type _dataReaderType;
		public override Type  DataReaderType
		{
			get { return _dataReaderType ?? (_dataReaderType = Type.GetType(_dataReaderTypeName, true)); }
		}

		Func<string,IDbConnection> _createConnection;

		public override IDbConnection CreateConnection(string connectionString)
		{
			if (_createConnection == null)
			{
				var p = Expression.Parameter(typeof(string));
				var l = Expression.Lambda<Func<string,IDbConnection>>(
					Expression.New(ConnectionType.GetConstructor(new[] { typeof(string) }), p),
					p);

				_createConnection = l.Compile();
			}

			return _createConnection(connectionString);
		}

		#region Expression Helpers

		//                                                      ((FbParameter)parameter).   FbDbType =           FbDbType.          TimeStamp;
		protected Action<IDbDataParameter> GetSetParameter(string parameterTypeName, string propertyName, string dbTypeName, string valueName)
		{
			var pType  = ConnectionType.Assembly.GetType(ConnectionType.Namespace + "." + parameterTypeName, true);
			var dbType = ConnectionType.Assembly.GetType(ConnectionType.Namespace + "." + dbTypeName, true);
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

		// SetToTypeField  <MySqlDataReader,MySqlDecimal> ((r,i) => r.GetMySqlDecimal (i));
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

		#endregion
	}
}
