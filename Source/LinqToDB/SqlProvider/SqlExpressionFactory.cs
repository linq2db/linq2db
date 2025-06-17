using System;

using LinqToDB.Common;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
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
