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

		protected void SetTypes(string assembly, string connectionType, string dataReaderType, string parameterType)
		{
			_connectionType = Type.GetType("{0}.{1}, {0}".Args(assembly, connectionType), true);
			_dataReaderType = Type.GetType("{0}.{1}, {0}".Args(assembly, dataReaderType), true);
			ParameterType   = Type.GetType("{0}.{1}, {0}".Args(assembly, parameterType),  true);

			var p = Expression.Parameter(typeof(string));
			var l = Expression.Lambda<Func<string,IDbConnection>>(
				Expression.New(_connectionType.GetConstructor(new[] { typeof(string) }), p),
				p);

			_createConnection = l.Compile();
		}

		Type _connectionType; public override Type ConnectionType { get { return _connectionType; } }
		Type _dataReaderType; public override Type DataReaderType { get { return _dataReaderType; } }

		public Type ParameterType { get; private set; }

		Func<string,IDbConnection> _createConnection;

		public override IDbConnection CreateConnection(string connectionString)
		{
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
