using System;

namespace LinqToDB.DataProvider.Firebird
{
	using Mapping;

	public class FirebirdMappingSchema : MappingSchema
	{
		public FirebirdMappingSchema() : this(ProviderName.Firebird)
		{
		}

		protected FirebirdMappingSchema(string configuration) : base(configuration)
		{
		}
	}
}
