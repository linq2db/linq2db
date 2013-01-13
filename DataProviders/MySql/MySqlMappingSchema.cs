using System;

using MySql.Data.Types;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class MySqlMappingSchema : MappingSchema
	{
		public MySqlMappingSchema() : base(ProviderName.MySql)
		{
			SetDataType(typeof(MySqlDecimal),  DataType.Decimal);
			SetDataType(typeof(MySqlDateTime), DataType.DateTime2);
		}
	}
}
