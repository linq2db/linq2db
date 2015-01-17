using System;

namespace LinqToDB.DataProvider.SapHana
{
	using Mapping;
	using SqlQuery;

	public class SapHanaMappingSchema : MappingSchema
	{
		public SapHanaMappingSchema() : base(ProviderName.SapHana)
		{
		}

		protected SapHanaMappingSchema(string configuration) : base(configuration)
		{
			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
		}
	}
}
