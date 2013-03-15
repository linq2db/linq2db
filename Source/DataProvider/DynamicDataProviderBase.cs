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
			_connectionTypeName = "{0}.{1}, {0}".Args(_assemblyName, connectionTypeName);
			_dataReaderTypeName = "{0}.{1}, {0}".Args(_assemblyName, dataReaderTypeName);
			_parameterTypeName  = "{0}.{1}, {0}".Args(_assemblyName, parameterTypeName);
		}

		string _assemblyName;
		string _connectionTypeName;
		string _dataReaderTypeName;
		string _parameterTypeName;

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

		private Type _parameterType;
		public  Type  ParameterType
		{
			get { return _parameterType ?? (_parameterType = Type.GetType(_parameterTypeName, true)); }
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
