using System;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class AccessMappingSchema : MappingSchema
	{
		public AccessMappingSchema() : base(ProviderName.Access)
		{
		}
	}
}
