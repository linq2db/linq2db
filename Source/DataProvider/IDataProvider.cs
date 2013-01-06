using System;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public interface IDataProvider
	{
		string        Name           { get; }
		Type          ConnectionType { get; }
		MappingSchema MappingSchema  { get; }

		void          Configure          (string name, string value);
		IDbConnection CreateConnection   (string connectionString);

		Expression    ConvertDataReader  (Expression reader);
		Expression    GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType);

		bool? IsDBNullAllowed(IDataReader reader, int idx);

		void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value);
	}
}
