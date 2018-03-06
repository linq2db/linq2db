using System;
using LinqToDB.Data;

namespace LinqToDB.Extensions
{
	using Common;
	using Mapping;
	using SqlQuery;

	static class MappingExtensions
	{
		public static SqlValue GetSqlValue(this MappingSchema mappingSchema, object value)
		{
			if (value == null)
				throw new InvalidOperationException();

			return GetSqlValue(mappingSchema, value.GetType(), value);
		}

		public static SqlValue GetSqlValue(this MappingSchema mappingSchema, Type systemType, object value)
		{
			var underlyingType = systemType.ToNullableUnderlying();

			if (underlyingType.IsEnumEx() && mappingSchema.GetAttribute<Sql.EnumAttribute>(underlyingType) == null)
			{
				if (value != null || systemType == underlyingType)
				{

					var type = Converter.GetDefaultMappingFromEnumType(mappingSchema, systemType);
					if (Configuration.UseEnumValueNameForStringColumns && type == typeof(string))
						return new SqlValue(type, value.ToString());

					return new SqlValue(type, Converter.ChangeType(value, type, mappingSchema));
				}
			}

			if (systemType == typeof(object) && value != null)
				systemType = value.GetType();

			return new SqlValue(systemType, value);
		}
	}
}
