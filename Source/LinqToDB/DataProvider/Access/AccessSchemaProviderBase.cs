using System;
using System.IO;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Data;
	using SchemaProvider;

	abstract class AccessSchemaProviderBase : SchemaProviderBase
	{
		public AccessSchemaProviderBase()
		{
		}

		protected override string GetDatabaseName(DataConnection connection)
		{
			var name = base.GetDatabaseName(connection);

			if (name.IsNullOrEmpty())
				name = Path.GetFileNameWithoutExtension(GetDataSourceName(connection));

			return name;
		}

		protected override string? GetProviderSpecificTypeNamespace() => null;

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, long? length, int? precision, int? scale)
		{
			if (dataTypeInfo == null && dataType != null)
			{
				switch (dataType.ToLower())
				{
					case "text" : return typeof(string);
					default     : throw new InvalidOperationException();
				}
			}

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale);
		}

		protected override DataType GetDataType(string? dataType, string? columnType, long? length, int? prec, int? scale)
		{
			switch (dataType?.ToLower())
			{
				case "smallint"   :
				case "short"      : return DataType.Int16;
				case "counter"    :
				case "integer"    :
				case "long"       : return DataType.Int32;
				case "real"       :
				case "single"     : return DataType.Single;
				case "double"     : return DataType.Double;
				case "currency"   : return DataType.Money;
				case "datetime"   : return DataType.DateTime;
				case "bit"        : return DataType.Boolean;
				case "byte"       : return DataType.Byte;
				case "guid"       : return DataType.Guid;
				case "binary"     : return DataType.Binary;
				case "bigbinary"  :
				case "longbinary" : return DataType.Image;
				case "varbinary"  : return DataType.VarBinary;
				case "text"       :
				case "longchar"   :
				case "longtext"   : return DataType.NText;
				case "varchar"    : return DataType.VarChar;
				case "char"       : return DataType.Char;
				case "decimal"    : return DataType.Decimal;
			}

			return DataType.Undefined;
		}
	}
}
