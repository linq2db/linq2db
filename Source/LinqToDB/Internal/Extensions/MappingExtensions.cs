using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Extensions
{
	static class MappingExtensions
	{
		public static SqlValue GetSqlValueFromObject(this ColumnDescriptor columnDescriptor, object obj)
		{
			var providerValue = columnDescriptor.GetProviderValue(obj);

			return new SqlValue(columnDescriptor.GetDbDataType(true), providerValue);
		}

		public static SqlValue GetSqlValue<T>(this MappingSchema mappingSchema, DbDataType type, T originalValue)
			where T: notnull
		{
			var converter = mappingSchema.GetConverter<T, DataParameter>(ConversionType.ToDatabase);

			if (converter != null)
			{
				var p = converter(originalValue);
				return new SqlValue(p.DbDataType, p.Value);
			}

			return new SqlValue(type, originalValue);
		}

		public static SqlValue GetSqlValue(this MappingSchema mappingSchema, Type systemType, object? originalValue, DbDataType? columnType)
		{
			if (originalValue is DataParameter p)
				return new SqlValue(p.Value == null ? p.GetOrSetDbDataType(columnType) : p.DbDataType, p.Value);

			var underlyingType = systemType.UnwrapNullableType();

			if (!mappingSchema.ValueToSqlConverter.CanConvert(underlyingType))
			{
				if (underlyingType.IsEnum && !mappingSchema.HasAttribute<Sql.EnumAttribute>(underlyingType))
				{
					if (originalValue != null || systemType == underlyingType)
					{
						var type = Converter.GetDefaultMappingFromEnumType(mappingSchema, systemType)!;

						return new SqlValue(
							type,
							LinqToDB.Common.Configuration.UseEnumValueNameForStringColumns
								&& type == typeof(string)
								&& mappingSchema.GetMapValues(underlyingType) == null
								? string.Format(CultureInfo.InvariantCulture, "{0}", originalValue)
								: Converter.ChangeType(originalValue, type, mappingSchema)
						);
					}
				}
			}

			if (systemType == typeof(object) && originalValue != null)
				systemType = originalValue.GetType();

			var valueDbType = originalValue == null
				? columnType ?? mappingSchema.GetDbDataType(systemType)
				: mappingSchema.GetDbDataType(systemType);

			return new SqlValue(valueDbType, originalValue);
		}

		public static bool TryConvertToSql(this MappingSchema mappingSchema, StringBuilder stringBuilder, DbDataType? dataType, DataOptions options, object? value)
		{
			var sqlConverter = mappingSchema.ValueToSqlConverter;

			if (value is null)
			{
				return sqlConverter.TryConvert(stringBuilder, mappingSchema, dataType, options, value);
			}

			var systemType     = value.GetType();
			var underlyingType = systemType.UnwrapNullableType();

			if (!mappingSchema.ValueToSqlConverter.CanConvert(underlyingType))
			{
				if (underlyingType.IsEnum && !mappingSchema.HasAttribute<Sql.EnumAttribute>(underlyingType))
				{
					if (systemType == underlyingType)
					{
						var type = Converter.GetDefaultMappingFromEnumType(mappingSchema, systemType)!;

						value = LinqToDB.Common.Configuration.UseEnumValueNameForStringColumns
							&& type                                       == typeof(string)
							&& mappingSchema.GetMapValues(underlyingType) == null
							? string.Format(CultureInfo.InvariantCulture, "{0}", value)
							: Converter.ChangeType(value, type, mappingSchema);
					}
				}
			}

			return sqlConverter.TryConvert(stringBuilder, mappingSchema, dataType, options, value);
		}

		public static void ConvertToSqlValue(this MappingSchema mappingSchema, StringBuilder stringBuilder,
			DbDataType? dataType, DataOptions options, object? value)
		{
			if (!mappingSchema.TryConvertToSql(stringBuilder, dataType, options, value))
				throw new LinqToDBException($"Cannot convert value of type {value?.GetType()} to SQL");
		}

		public static Sql.ExpressionAttribute? GetExpressionAttribute(this MemberInfo member, MappingSchema mappingSchema)
		{
			return mappingSchema.GetAttribute<Sql.ExpressionAttribute>(member.ReflectedType!, member);
		}

		public static Sql.TableFunctionAttribute? GetTableFunctionAttribute(this MemberInfo member, MappingSchema mappingSchema)
		{
			return mappingSchema.GetAttribute<Sql.TableFunctionAttribute>(member.ReflectedType!, member);
		}

		public static bool IsCollectionType(this MappingSchema mappingSchema, Type type)
		{
			if (mappingSchema.IsScalarType(type))
				return false;
			if (!typeof(IEnumerable<>).IsSameOrParentOf(type))
				return false;
			return true;
		}
	}
}
