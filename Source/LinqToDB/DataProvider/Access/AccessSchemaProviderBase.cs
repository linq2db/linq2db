using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

		// see https://github.com/linq2db/linq2db.LINQPad/issues/10
		// we create separate connection for GetSchema calls to workaround provider bug
		// logic not applied if active transaction present - user must remove transaction if he has issues
		protected virtual TResult ExecuteOnNewConnection<TResult>(DataConnection dataConnection, Func<DataConnection, TResult> action)
		{
			return action(dataConnection);
		}

		protected override string GetDatabaseName(DataConnection connection)
		{
			var name = base.GetDatabaseName(connection);

			if (name.IsNullOrEmpty())
				name = Path.GetFileNameWithoutExtension(GetDataSourceName(connection));

			return name;
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dts = ExecuteOnNewConnection(dataConnection, cn => base.GetDataTypes(cn));

			if (dts.All(dt => dt.ProviderDbType != 128))
			{
				dts.Add(new DataTypeInfo
				{
					TypeName         = "image",
					DataType         = typeof(byte[]).FullName,
					CreateFormat     = "image({0})",
					CreateParameters = "length",
					ProviderDbType   = 128
				});
			}

			if (dts.All(dt => dt.ProviderDbType != 130))
			{
				dts.Add(new DataTypeInfo
				{
					TypeName         = "text",
					DataType         = typeof(string).FullName,
					CreateFormat     = "text({0})",
					CreateParameters = "length",
					ProviderDbType   = 130
				});
			}

			return dts;
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
				case "binary"     :
				case "bigbinary"  :
				case "longbinary" : return DataType.Binary;
				case "varbinary"  : return DataType.VarBinary;
				case "text"       :
				case "longtext"   : return DataType.NText;
				case "longchar"   :
				case "varchar"    : return DataType.VarChar;
				case "char"       : return DataType.Char;
				case "decimal"    : return DataType.Decimal;
			}

			return DataType.Undefined;
		}
	}
}
