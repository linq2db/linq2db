using System;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public class SQLiteMappingSchema : MappingSchema
	{
		public SQLiteMappingSchema() : this(ProviderName.SQLite)
		{
		}

		protected SQLiteMappingSchema(string configuration) : base(configuration)
		{
		}
	}
}
