using System;

namespace LinqToDB.DataProvider.SapHana
{
	using Mapping;

	public class SapHanaMappingSchema : MappingSchema
	{
		public SapHanaMappingSchema() : base(ProviderName.SapHana)
		{
		}

		protected SapHanaMappingSchema(string configuration) : base(configuration)
		{
		}
	}
}
