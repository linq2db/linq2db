using System;

namespace LinqToDB.SqlBuilder
{
	using Mapping;

	public class SqlTable<T> : SqlTable
	{
		public SqlTable()
			: base(typeof(T))
		{
		}

		public SqlTable(MappingSchemaOld mappingSchema)
			: base(mappingSchema, typeof(T))
		{
		}
	}
}
