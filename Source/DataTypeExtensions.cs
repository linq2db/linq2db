using System;
using System.Data;
using System.Data.Linq;
using System.Xml;

namespace LinqToDB
{
	public static class SqlDataType
	{
		public static Type ToSystemType(this DataType type)
		{
			switch (type)
			{
				case DataType.Char           :
				case DataType.VarChar        :
				case DataType.NChar          :
				case DataType.NVarChar       :
				case DataType.Text           :
				case DataType.NText          : return typeof(string);

				case DataType.Binary         :
				case DataType.VarBinary      :
				case DataType.Timestamp      :
				case DataType.Image          : return typeof(byte[]);

				case DataType.Boolean        : return typeof(bool);
				case DataType.Guid           : return typeof(Guid);

				case DataType.SByte          : return typeof(sbyte);
				case DataType.Int16          : return typeof(short);
				case DataType.Int32          : return typeof(int);
				case DataType.Int64          : return typeof(long);
				case DataType.Byte           : return typeof(byte);
				case DataType.UInt16         : return typeof(ushort);
				case DataType.UInt32         : return typeof(uint);
				case DataType.UInt64         : return typeof(ulong);

				case DataType.Single         : return typeof(float);
				case DataType.Double         : return typeof(double);
				case DataType.Decimal        :
				case DataType.Money          :
				case DataType.SmallMoney     : return typeof(decimal);

				case DataType.Date           :
				case DataType.DateTime       :
				case DataType.DateTime2      :
				case DataType.SmallDateTime  : return typeof(DateTime);
				case DataType.Time           : return typeof(TimeSpan);
				case DataType.DateTimeOffset : return typeof(DateTimeOffset);

				case DataType.Xml            : return typeof(string);
				case DataType.Variant        : return typeof(object);
			}

			throw new InvalidOperationException();
		}

		public static DataType ToDataType(this Type type)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				type = type.GetGenericArguments()[0];

			if (type.IsEnum)
				type = Enum.GetUnderlyingType(type);

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.String   : return DataType.NVarChar;
				case TypeCode.Boolean  : return DataType.Boolean;
				case TypeCode.SByte    : return DataType.SByte;
				case TypeCode.Int16    : return DataType.Int16;
				case TypeCode.Int32    : return DataType.Int32;
				case TypeCode.Int64    : return DataType.Int64;
				case TypeCode.Byte     : return DataType.Byte;
				case TypeCode.UInt16   : return DataType.UInt16;
				case TypeCode.UInt32   : return DataType.UInt32;
				case TypeCode.UInt64   : return DataType.UInt64;
				case TypeCode.Single   : return DataType.Single;
				case TypeCode.Double   : return DataType.Double;
				case TypeCode.Decimal  : return DataType.Decimal;
				case TypeCode.DateTime : return DataType.DateTime;
				case TypeCode.Object   : return DataType.Variant;
			}

			if (type == typeof(Guid))           return DataType.Guid;
			if (type == typeof(byte[]))         return DataType.VarBinary;
			if (type == typeof(Binary))         return DataType.VarBinary;
			if (type == typeof(TimeSpan))       return DataType.Time;
			if (type == typeof(DateTimeOffset)) return DataType.DateTimeOffset;
			if (type == typeof(XmlReader))      return DataType.Xml;

			throw new InvalidOperationException();
		}

		public static SqlDbType ToSqlDbType(this DataType type)
		{
			switch (type)
			{
				case DataType.Char           : return SqlDbType.Char;
				case DataType.VarChar        : return SqlDbType.VarChar;
				case DataType.NChar          : return SqlDbType.NChar;
				case DataType.NVarChar       : return SqlDbType.NVarChar;
				case DataType.Text           : return SqlDbType.Text;
				case DataType.NText          : return SqlDbType.NText;

				case DataType.Binary         : return SqlDbType.Binary;
				case DataType.VarBinary      : return SqlDbType.VarBinary;
				case DataType.Image          : return SqlDbType.Image;
				case DataType.Timestamp      : return SqlDbType.Timestamp;

				case DataType.Boolean        : return SqlDbType.Bit;
				case DataType.Guid           : return SqlDbType.UniqueIdentifier;

				case DataType.SByte          : return SqlDbType.SmallInt;
				case DataType.Int16          : return SqlDbType.SmallInt;
				case DataType.Int32          : return SqlDbType.Int;
				case DataType.Int64          : return SqlDbType.BigInt;
				case DataType.Byte           : return SqlDbType.TinyInt;
				case DataType.UInt16         : return SqlDbType.Int;
				case DataType.UInt32         : return SqlDbType.BigInt;
				case DataType.UInt64         : return SqlDbType.Decimal;

				case DataType.Single         : return SqlDbType.Real;
				case DataType.Double         : return SqlDbType.Float;
				case DataType.Decimal        : return SqlDbType.Decimal;
				case DataType.Money          : return SqlDbType.Money;
				case DataType.SmallMoney     : return SqlDbType.SmallMoney;

				case DataType.Date           : return SqlDbType.Date;
				case DataType.Time           : return SqlDbType.Time;
				case DataType.DateTime       : return SqlDbType.DateTime;
				case DataType.DateTime2      : return SqlDbType.DateTime2;
				case DataType.SmallDateTime  : return SqlDbType.SmallDateTime;
				case DataType.DateTimeOffset : return SqlDbType.DateTimeOffset;

				case DataType.Xml            : return SqlDbType.Xml;
				case DataType.Variant        : return SqlDbType.Variant;
			}

			throw new InvalidOperationException();
		}

		public static DataType ToDataType(this SqlDbType type)
		{
			switch (type)
			{
				case SqlDbType.Char             : return DataType.Char;
				case SqlDbType.VarChar          : return DataType.VarChar;
				case SqlDbType.NChar            : return DataType.NChar;
				case SqlDbType.NVarChar         : return DataType.NVarChar;
				case SqlDbType.Text             : return DataType.Text;
				case SqlDbType.NText            : return DataType.NText;

				case SqlDbType.Binary           : return DataType.Binary;
				case SqlDbType.VarBinary        : return DataType.VarBinary;
				case SqlDbType.Image            : return DataType.Image;
				case SqlDbType.Timestamp        : return DataType.Timestamp;

				case SqlDbType.Bit              : return DataType.Boolean;
				case SqlDbType.UniqueIdentifier : return DataType.Guid;

				case SqlDbType.TinyInt          : return DataType.Byte;
				case SqlDbType.SmallInt         : return DataType.Int16;
				case SqlDbType.Int              : return DataType.Int32;
				case SqlDbType.BigInt           : return DataType.Int64;

				case SqlDbType.Real             : return DataType.Single;
				case SqlDbType.Float            : return DataType.Double;
				case SqlDbType.Decimal          : return DataType.Decimal;
				case SqlDbType.Money            : return DataType.Money;
				case SqlDbType.SmallMoney       : return DataType.SmallMoney;

				case SqlDbType.Date             : return DataType.Date;
				case SqlDbType.Time             : return DataType.Time;
				case SqlDbType.DateTime         : return DataType.DateTime;
				case SqlDbType.DateTime2        : return DataType.DateTime2;
				case SqlDbType.SmallDateTime    : return DataType.SmallDateTime;
				case SqlDbType.DateTimeOffset   : return DataType.DateTimeOffset;

				case SqlDbType.Xml              : return DataType.Xml;
				case SqlDbType.Variant          : return DataType.Variant;
			}

			throw new InvalidOperationException();
		}

		public static DbType ToDbType(this DataType type)
		{
			switch (type)
			{
				case DataType.Char           : return DbType.AnsiStringFixedLength;
				case DataType.VarChar        :
				case DataType.Text           : return DbType.AnsiString;
				case DataType.NChar          : return DbType.StringFixedLength;
				case DataType.NVarChar       :
				case DataType.NText          : return DbType.String;

				case DataType.Binary         :
				case DataType.VarBinary      :
				case DataType.Image          :
				case DataType.Timestamp      : return DbType.Binary;

				case DataType.Boolean        : return DbType.Boolean;
				case DataType.Guid           : return DbType.Guid;

				case DataType.SByte          : return DbType.SByte;
				case DataType.Int16          : return DbType.Int16;
				case DataType.Int32          : return DbType.Int32;
				case DataType.Int64          : return DbType.Int64;
				case DataType.Byte           : return DbType.Byte;
				case DataType.UInt16         : return DbType.UInt16;
				case DataType.UInt32         : return DbType.UInt32;
				case DataType.UInt64         : return DbType.UInt64;

				case DataType.Single         : return DbType.Single;
				case DataType.Double         : return DbType.Double;
				case DataType.Decimal        : return DbType.Decimal;
				case DataType.Money          : return DbType.Currency;
				case DataType.SmallMoney     : return DbType.Currency;

				case DataType.Date           : return DbType.Date;
				case DataType.Time           : return DbType.Time;
				case DataType.DateTime       : return DbType.DateTime;
				case DataType.DateTime2      : return DbType.DateTime2;
				case DataType.SmallDateTime  : return DbType.DateTime;
				case DataType.DateTimeOffset : return DbType.DateTimeOffset;

				case DataType.Xml            : return DbType.Xml;
				case DataType.Variant        : return DbType.Object;
			}

			throw new InvalidOperationException();
		}

		public static DataType ToDataType(this DbType type)
		{
			switch (type)
			{
				case DbType.AnsiStringFixedLength : return DataType.Char;
				case DbType.AnsiString            : return DataType.VarChar;
				case DbType.StringFixedLength     : return DataType.NChar;
				case DbType.String                : return DataType.NVarChar;

				case DbType.Binary                : return DataType.VarBinary;

				case DbType.Boolean               : return DataType.Boolean;
				case DbType.Guid                  : return DataType.Guid;

				case DbType.SByte                 : return DataType.SByte;
				case DbType.Int16                 : return DataType.Int16;
				case DbType.Int32                 : return DataType.Int32;
				case DbType.Int64                 : return DataType.Int64;
				case DbType.Byte                  : return DataType.Byte;
				case DbType.UInt16                : return DataType.UInt16;
				case DbType.UInt32                : return DataType.UInt32;
				case DbType.UInt64                : return DataType.UInt64;

				case DbType.Single                : return DataType.Single;
				case DbType.Double                : return DataType.Double;
				case DbType.Decimal               : return DataType.Decimal;
				case DbType.Currency              : return DataType.Money;

				case DbType.Date                  : return DataType.Date;
				case DbType.Time                  : return DataType.Time;
				case DbType.DateTime              : return DataType.DateTime;
				case DbType.DateTime2             : return DataType.DateTime2;
				case DbType.DateTimeOffset        : return DataType.DateTimeOffset;

				case DbType.Xml                   : return DataType.Xml;
				case DbType.Object                : return DataType.Variant;
			}

			throw new InvalidOperationException();
		}
	}
}
