using System;

namespace LinqToDB.Extensions
{
	using Common;
	using Mapping;
	using SqlBuilder;

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

			if (underlyingType.IsEnum && mappingSchema.GetAttribute<Sql.EnumAttribute>(underlyingType) == null)
			{
				if (value != null || systemType == underlyingType)
				{
					var type = Converter.GetDefaultMappingFromEnumType(mappingSchema, systemType);

					return new SqlValue(type, Converter.ChangeType(value, type, mappingSchema));
				}
			}

			return new SqlValue(systemType, value);
		}
	}
}
