using System;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public class AccessMappingSchema : MappingSchema
	{
		public AccessMappingSchema(string configuation) : base(configuation)
		{
			SetDataType(typeof(DateTime), DataType.DateTime);
		}
	}
}
