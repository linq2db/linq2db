using System;
using LinqToDB.Mapping;

namespace LinqToDB.Sql
{
	public class SqlTable<T> : SqlTable
	{
		public SqlTable()
			: base(typeof(T))
		{
		}

		public SqlTable(MappingSchema mappingSchema)
			: base(mappingSchema, typeof(T))
		{
		}
	}
}
