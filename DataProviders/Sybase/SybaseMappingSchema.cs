using System;

namespace LinqToDB.DataProvider.Sybase
{
	using Mapping;

	public class SybaseMappingSchema : MappingSchema
	{
		public SybaseMappingSchema() : this(ProviderName.Sybase)
		{
		}

		protected SybaseMappingSchema(string configuration) : base(configuration)
		{
		}
	}
}
