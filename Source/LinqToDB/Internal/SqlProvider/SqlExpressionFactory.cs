using System;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.SqlProvider
{
	public class SqlExpressionFactory : ISqlExpressionFactory
	{
		public SqlExpressionFactory(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			DataOptions   = dataOptions;
			MappingSchema = mappingSchema;
		}

		public MappingSchema MappingSchema { get; }
		public DataOptions   DataOptions   { get; }

		public DbDataType GetDbDataType(ISqlExpression expression)
			=> QueryHelper.GetDbDataType(expression, MappingSchema);

		public DbDataType GetDbDataType(Type type)
			=> MappingSchema.GetDbDataType(type);
	}
}
