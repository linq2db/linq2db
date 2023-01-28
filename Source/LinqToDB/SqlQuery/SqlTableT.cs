namespace LinqToDB.SqlQuery
{
	using Mapping;

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

		internal SqlTable(MappingSchema mappingSchema, EntityDescriptor? descriptor)
			: base(mappingSchema, descriptor, typeof(T))
		{
		}
	}
}
