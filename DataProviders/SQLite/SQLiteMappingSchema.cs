using System;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class SQLiteMappingSchema : MappingSchema
	{
		public SQLiteMappingSchema() : base(ProviderName.SQLite)
		{
		}
	}
}
