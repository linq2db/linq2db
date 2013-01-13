using System;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class MySqlMappingSchema : MappingSchema
	{
		public MySqlMappingSchema() : base(ProviderName.MySql)
		{
			//SetDataType(typeof(DateTime), DataType.DateTime);
		}
	}
}
