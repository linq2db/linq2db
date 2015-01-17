using System;

namespace LinqToDB.DataProvider.Firebird
{
	using Mapping;
	using SqlQuery;

	public class FirebirdMappingSchema : MappingSchema
	{
		public FirebirdMappingSchema() : this(ProviderName.Firebird)
		{
		}

		protected FirebirdMappingSchema(string configuration) : base(configuration)
		{
			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
		}
	}
}
