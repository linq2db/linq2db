using System;
using System.IO;

using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Access
{
	abstract class AccessSchemaProviderBase : SchemaProviderBase
	{
		protected AccessSchemaProviderBase()
		{
		}

		protected override string GetDatabaseName(DataConnection dbConnection)
		{
			var name = base.GetDatabaseName(dbConnection);

			if (string.IsNullOrEmpty(name))
				name = Path.GetFileNameWithoutExtension(GetDataSourceName(dbConnection));

			return name;
		}

		protected override string? GetProviderSpecificTypeNamespace() => null;

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			if (dataTypeInfo == null && dataType != null)
			{
				if (dataType.ToLowerInvariant() == "text")
					return length == 1 && !options.GenerateChar1AsString ? typeof(char) : typeof(string);
			}

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options);
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			return dataType?.ToLowerInvariant() switch
			{
				"smallint"   => DataType.Int16,
				"short"      => DataType.Int16,

				"counter"    => DataType.Int32,
				"integer"    => DataType.Int32,
				"long"       => DataType.Int32,

				"real"       => DataType.Single,
				"single"     => DataType.Single,

				"double"     => DataType.Double,

				"currency"   => DataType.Money,
				"datetime"   => DataType.DateTime,
				"bit"        => DataType.Boolean,
				"byte"       => DataType.Byte,
				"guid"       => DataType.Guid,
				"binary"     => DataType.Binary,

				"bigbinary"  => DataType.Image,
				"longbinary" => DataType.Image,

				"varbinary"  => DataType.VarBinary,

				"text"       => DataType.NText,
				"longchar"   => DataType.NText,
				"longtext"   => DataType.NText,

				"varchar"    => DataType.VarChar,
				"char"       => DataType.Char,

				"decimal"    => DataType.Decimal,

				_			 => DataType.Undefined,
			};
		}
	}
}
