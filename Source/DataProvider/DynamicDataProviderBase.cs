using System;
using System.Data;
using System.Linq.Expressions;

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

		protected void SetTypes(string assemblyName, string connectionTypeName, string dataReaderTypeName, string parameterTypeName)
		{
			_assemblyName       = assemblyName;
			_connectionTypeName = connectionTypeName;
			_dataReaderTypeName = dataReaderTypeName;
			_parameterTypeName  = parameterTypeName;
		}

		string _assemblyName;
		string _connectionTypeName;
		string _dataReaderTypeName;
		string _parameterTypeName;

		private         Type _connectionType;
		public override Type  ConnectionType
		{
			get { return _connectionType ?? (_connectionType = Type.GetType("{0}.{1}, {0}".Args(_assemblyName, _connectionTypeName), true)); }
		}

		private         Type _dataReaderType;
		public override Type  DataReaderType
		{
			get { return _dataReaderType ?? (_dataReaderType = Type.GetType("{0}.{1}, {0}".Args(_assemblyName, _dataReaderTypeName), true)); }
		}

		private Type _parameterType;
		public  Type  ParameterType
		{
			get { return _parameterType ?? (_parameterType = Type.GetType("{0}.{1}, {0}".Args(_assemblyName, _parameterTypeName), true)); }
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

		protected Action<IDbDataParameter> GetSetParameter(string propertyName, string dbTypeName, string valueName)
		{
			var dbType = ConnectionType.Assembly.GetType(ConnectionType.Namespace + "." + dbTypeName, true);
			var value  = Enum.Parse(dbType, valueName);

			var p = Expression.Parameter(typeof(IDbDataParameter));
			var l = Expression.Lambda<Action<IDbDataParameter>>(
				Expression.Assign(
					Expression.PropertyOrField(
						Expression.Convert(p, ParameterType),
						propertyName),
					Expression.Constant(value)),
				p);

			return l.Compile();
		}
	}
}
