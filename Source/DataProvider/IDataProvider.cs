using System;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public interface IDataProvider
	{
		string        Name          { get; }
		string        ProviderName  { get; }
		MappingSchema MappingSchema { get; }

		void          Configure          (string name, string value);
		IDbConnection CreateConnection   (string       connectionString);

		Expression    ConvertDataReader  (Expression reader);
		Expression    GetReaderExpression(IDataReader reader, int idx, Expression readerExpression, Type toType);
	}
}
