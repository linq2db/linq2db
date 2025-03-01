using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Numerics;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	public class SqlDataType : ISqlExpression, IEquatable<SqlDataType>
	{
		#region Init

		public SqlDataType(DbDataType dataType)
		{
			Type = dataType;
		}

		public SqlDataType(DataType dataType)
		{
			Type = GetDataType(dataType).Type.WithDataType(dataType);
		}

		public SqlDataType(DataType dataType, int? length)
		{
			Type = GetDataType(dataType).Type.WithDataType(dataType).WithLength(length);
		}

		public SqlDataType(DataType dataType, Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type);
		}

		public SqlDataType(DataType dataType, Type type, string dbType)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type)
				.WithDbType(dbType);
		}

		public SqlDataType(DataType dataType, Type type, int length)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));
			if (length <= 0)  throw new ArgumentOutOfRangeException(nameof(length));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type)
				.WithLength(length);
		}

		public SqlDataType(DataType dataType, Type type, int precision, int scale)
		{
			if (type      == null) throw new ArgumentNullException(nameof(type));
			if (precision <= 0   ) throw new ArgumentOutOfRangeException(nameof(precision));
			if (scale     <  0   ) throw new ArgumentOutOfRangeException(nameof(scale));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type)
				.WithPrecision(precision)
				.WithScale(scale);
		}

		internal SqlDataType(ColumnDescriptor column)
			: this(column.GetDbDataType(true))
		{
		}

		internal SqlDataType(SqlField field)
			: this(field.Type)
		{
		}

		#endregion

		#region Public Members

		public DbDataType Type { get; internal set; }

		public static readonly SqlDataType Undefined = new (DataType.Undefined, typeof(object), (int?)null, (int?)null, null, null);

		public bool IsCharDataType
		{
			get
			{
				switch (Type.DataType)
				{
					case DataType.Char     :
					case DataType.NChar    :
					case DataType.VarChar  :
					case DataType.NVarChar : return true;
					default                : return false;
				}
			}
		}

		#endregion

		#region Static Members

		readonly struct TypeInfo
		{
			public TypeInfo(DataType dbType, int? maxLength, int? maxPrecision, int? maxScale, int? maxDisplaySize)
			{
				DataType       = dbType;
				MaxLength      = maxLength;
				MaxPrecision   = maxPrecision;
				MaxScale       = maxScale;
				MaxDisplaySize = maxDisplaySize;
			}

			public readonly DataType DataType;
			public readonly int?     MaxLength;
			public readonly int?     MaxPrecision;
			public readonly int?     MaxScale;
			public readonly int?     MaxDisplaySize;
		}

		static TypeInfo[] SortTypeInfo(params TypeInfo[] info)
		{
			var sortedInfo = new TypeInfo[info.Max(ti => (int)ti.DataType) + 1];

			foreach (var typeInfo in info)
				sortedInfo[(int)typeInfo.DataType] = typeInfo;

			return sortedInfo;
		}

		static int Len(object obj)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}", obj).Length;
		}

		static readonly TypeInfo[] _typeInfo = SortTypeInfo
		(
			//           DbType                 MaxLength           MaxPrecision               MaxScale       MaxDisplaySize
			//
			new TypeInfo(DataType.Int64,                8,   Len( long.MaxValue),                     0,     Len( long.MinValue)),
			new TypeInfo(DataType.Int32,                4,   Len(  int.MaxValue),                     0,     Len(  int.MinValue)),
			new TypeInfo(DataType.Int16,                2,   Len(short.MaxValue),                     0,     Len(short.MinValue)),
			new TypeInfo(DataType.Byte,                 1,   Len( byte.MaxValue),                     0,     Len( byte.MaxValue)),
			new TypeInfo(DataType.Boolean,              1,                     1,                     0,                       1),

			new TypeInfo(DataType.Decimal,             17, Len(decimal.MaxValue), Len(decimal.MaxValue), Len(decimal.MinValue)+1),
			new TypeInfo(DataType.Money,                8,                    19,                     4,                  19 + 2),
			new TypeInfo(DataType.SmallMoney,           4,                    10,                     4,                  10 + 2),
			new TypeInfo(DataType.Double,               8,                    15,                    15,              15 + 2 + 5),
			new TypeInfo(DataType.Single,               4,                     7,                     7,               7 + 2 + 4),

			new TypeInfo(DataType.DateTime,             8,                  null,                  null,                      23),
			new TypeInfo(DataType.DateTime2,            8,                  null,                  null,                      27),
			new TypeInfo(DataType.SmallDateTime,        4,                  null,                  null,                      19),
			new TypeInfo(DataType.Date,                 3,                  null,                  null,                      10),
			new TypeInfo(DataType.Time,                 5,                  null,                  null,                      16),
			new TypeInfo(DataType.DateTimeOffset,      10,                  null,                  null,                      34),

			new TypeInfo(DataType.Char,              8000,                  null,                  null,                    8000),
			new TypeInfo(DataType.VarChar,           8000,                  null,                  null,                    8000),
			new TypeInfo(DataType.Text,              null,                  null,                  null,            int.MaxValue),
			new TypeInfo(DataType.NChar,             4000,                  null,                  null,                    4000),
			new TypeInfo(DataType.NVarChar,          4000,                  null,                  null,                    4000),
			new TypeInfo(DataType.NText,             null,                  null,                  null,        int.MaxValue / 2),
			new TypeInfo(DataType.Json,              null,                  null,                  null,        int.MaxValue / 2),
			new TypeInfo(DataType.BinaryJson,        null,                  null,                  null,        int.MaxValue / 2),

			new TypeInfo(DataType.Binary,            8000,                  null,                  null,                    null),
			new TypeInfo(DataType.VarBinary,         8000,                  null,                  null,                    null),
			new TypeInfo(DataType.Image,     int.MaxValue,                  null,                  null,                    null),

			new TypeInfo(DataType.Timestamp,            8,                  null,                  null,                    null),
			new TypeInfo(DataType.Guid,                16,                  null,                  null,                      36),

			new TypeInfo(DataType.Variant,           null,                  null,                  null,                    null),
			new TypeInfo(DataType.Xml,               null,                  null,                  null,                    null),
			new TypeInfo(DataType.Udt,               null,                  null,                  null,                    null),
			new TypeInfo(DataType.BitArray,          null,                  null,                  null,                    null)
		);

		public static int? GetMaxLength(DataType dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxLength;
		}

		public static int? GetMaxPrecision(DataType dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxPrecision;
		}

		public static int? GetMaxScale(DataType dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxScale;
		}

		public static int? GetMaxDisplaySize(DataType dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxDisplaySize;
		}

		public static SqlDataType GetDataType(DataType type)
		{
			return type switch
			{
				DataType.Int64          => DbInt64,
				DataType.Binary         => DbBinary,
				DataType.Boolean        => DbBoolean,
				DataType.Char           => DbChar,
				DataType.DateTime       => DbDateTime,
				DataType.Decimal        => DbDecimal,
				DataType.Double         => DbDouble,
				DataType.Image          => DbImage,
				DataType.Int32          => DbInt32,
				DataType.Money          => DbMoney,
				DataType.NChar          => DbNChar,
				DataType.NText          => DbNText,
				DataType.NVarChar       => DbNVarChar,
				DataType.Single         => DbSingle,
				DataType.Guid           => DbGuid,
				DataType.SmallDateTime  => DbSmallDateTime,
				DataType.Int16          => DbInt16,
				DataType.SmallMoney     => DbSmallMoney,
				DataType.Text           => DbText,
				DataType.Timestamp      => DbTimestamp,
				DataType.Byte           => DbByte,
				DataType.VarBinary      => DbVarBinary,
				DataType.VarChar        => DbVarChar,
				DataType.Variant        => DbVariant,
				DataType.Xml            => DbXml,
				DataType.BitArray       => DbBitArray,
				DataType.Udt            => DbUdt,
				DataType.Date           => DbDate,
				DataType.Time           => DbTime,
				DataType.DateTime2      => DbDateTime2,
				DataType.DateTimeOffset => DbDateTimeOffset,
				DataType.UInt16         => DbUInt16,
				DataType.UInt32         => DbUInt32,
				DataType.UInt64         => DbUInt64,
				DataType.Dictionary     => DbDictionary,
				DataType.Json           => DbJson,
				DataType.BinaryJson     => DbBinaryJson,
				DataType.SByte          => DbSByte,
				DataType.Int128         => DbInt128,
				DataType.DecFloat       => DbDecFloat,
				DataType.TimeTZ         => DbTimeTZ,
				_                       => throw new InvalidOperationException($"Unexpected type: {type}"),
			};
		}

		public static bool TypeCanBeNull(Type type)
		{
			if (type.IsNullableType() ||
				typeof(INullable).IsSameOrParentOf(type))
				return true;

			return false;
		}

#endregion

		#region Default Types

		internal SqlDataType(DataType dataType, Type type, int? length, int? precision, int? scale, string? dbType)
			: this(new DbDataType(type, dataType, dbType, length, precision, scale))
		{
		}

		SqlDataType(DataType dataType, Type type, Func<DataType,int?> length, int? precision, int? scale, string? dbType)
			: this(dataType, type, length(dataType), precision, scale, dbType)
		{
		}

		SqlDataType(DataType dataType, Type type, int? length, Func<DataType,int?> precision, int? scale, string? dbType)
			: this(dataType, type, length, precision(dataType), scale, dbType)
		{
		}

		public static readonly SqlDataType DbInt128         = new (DataType.Int128,         typeof(BigInteger),    (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbInt64          = new (DataType.Int64,          typeof(long),          (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbInt32          = new (DataType.Int32,          typeof(int),           (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbInt16          = new (DataType.Int16,          typeof(short),         (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbUInt64         = new (DataType.UInt64,         typeof(ulong),         (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbUInt32         = new (DataType.UInt32,         typeof(uint),          (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbUInt16         = new (DataType.UInt16,         typeof(ushort),        (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbSByte          = new (DataType.SByte,          typeof(sbyte),         (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbByte           = new (DataType.Byte,           typeof(byte),          (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbBoolean        = new (DataType.Boolean,        typeof(bool),          (int?)null,     (int?)null, null, null);

		public static readonly SqlDataType DbDecimal        = new (DataType.Decimal,        typeof(decimal),             null, GetMaxPrecision, 10, null);
		public static readonly SqlDataType DbMoney          = new (DataType.Money,          typeof(decimal),             null, GetMaxPrecision,  4, null);
		public static readonly SqlDataType DbSmallMoney     = new (DataType.SmallMoney,     typeof(decimal),             null, GetMaxPrecision,  4, null);
		public static readonly SqlDataType DbDouble         = new (DataType.Double,         typeof(double),        (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbSingle         = new (DataType.Single,         typeof(float),         (int?)null,     (int?)null, null, null);

		public static readonly SqlDataType DbDateTime       = new (DataType.DateTime,       typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbDateTime2      = new (DataType.DateTime2,      typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbSmallDateTime  = new (DataType.SmallDateTime,  typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbDate           = new (DataType.Date,           typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbTime           = new (DataType.Time,           typeof(TimeSpan),      (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbDateTimeOffset = new (DataType.DateTimeOffset, typeof(DateTimeOffset),(int?)null,     (int?)null, null, null);

		public static readonly SqlDataType DbChar           = new (DataType.Char,           typeof(string),      GetMaxLength,           null, null, null);
		public static readonly SqlDataType DbVarChar        = new (DataType.VarChar,        typeof(string),      GetMaxLength,           null, null, null);
		public static readonly SqlDataType DbText           = new (DataType.Text,           typeof(string),      GetMaxLength,           null, null, null);
		public static readonly SqlDataType DbNChar          = new (DataType.NChar,          typeof(string),      GetMaxLength,           null, null, null);
		public static readonly SqlDataType DbNVarChar       = new (DataType.NVarChar,       typeof(string),      GetMaxLength,           null, null, null);
		public static readonly SqlDataType DbNText          = new (DataType.NText,          typeof(string),      GetMaxLength,           null, null, null);
		public static readonly SqlDataType DbJson           = new (DataType.Json,           typeof(string),      GetMaxLength,           null, null, null);
		public static readonly SqlDataType DbBinaryJson     = new (DataType.BinaryJson,     typeof(string),      GetMaxLength,           null, null, null);

		public static readonly SqlDataType DbBinary         = new (DataType.Binary,         typeof(byte[]),      GetMaxLength,           null, null, null);
		public static readonly SqlDataType DbVarBinary      = new (DataType.VarBinary,      typeof(byte[]),      GetMaxLength,           null, null, null);
		public static readonly SqlDataType DbImage          = new (DataType.Image,          typeof(byte[]),      GetMaxLength,           null, null, null);

		public static readonly SqlDataType DbTimestamp      = new (DataType.Timestamp,      typeof(byte[]),        (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbGuid           = new (DataType.Guid,           typeof(Guid),          (int?)null,     (int?)null, null, null);

		public static readonly SqlDataType DbVariant        = new (DataType.Variant,        typeof(object),        (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbXml            = new (DataType.Xml,            typeof(SqlXml),        (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbBitArray       = new (DataType.BitArray,       typeof(BitArray),      (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbUdt            = new (DataType.Udt,            typeof(object),        (int?)null,     (int?)null, null, null);

		public static readonly SqlDataType Boolean          = DbBoolean;
		public static readonly SqlDataType Char             = new (DataType.Char,           typeof(char),                   1,     (int?)null, null, null);
		public static readonly SqlDataType SByte            = DbSByte;
		public static readonly SqlDataType Byte             = DbByte;
		public static readonly SqlDataType Int16            = DbInt16;
		public static readonly SqlDataType UInt16           = new (DataType.UInt16,         typeof(ushort),        (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType Int32            = DbInt32;
		public static readonly SqlDataType UInt32           = new (DataType.UInt32,         typeof(uint),          (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType UInt64           = new (DataType.UInt64,         typeof(ulong),         (int?)null, ulong.MaxValue.ToString(NumberFormatInfo.InvariantInfo).Length, null, null);
		public static readonly SqlDataType Single           = DbSingle;
		public static readonly SqlDataType Double           = DbDouble;
		public static readonly SqlDataType Decimal          = DbDecimal;
		public static readonly SqlDataType DateTime         = DbDateTime2;
		public static readonly SqlDataType String           = DbNVarChar;
		public static readonly SqlDataType Guid             = DbGuid;
		public static readonly SqlDataType ByteArray        = DbVarBinary;
		public static readonly SqlDataType LinqBinary       = DbVarBinary;
		public static readonly SqlDataType CharArray        = new (DataType.NVarChar,       typeof(char[]),      GetMaxLength,           null, null, null);
		public static readonly SqlDataType DateTimeOffset   = DbDateTimeOffset;
		public static readonly SqlDataType TimeSpan         = DbTime;
		public static readonly SqlDataType DbDictionary     = new (DataType.Dictionary,     typeof(Dictionary<string, string>), (int?)null, (int?)null, null, null);

		public static readonly SqlDataType SqlByte          = new (DataType.Byte,           typeof(SqlByte),       (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType SqlInt16         = new (DataType.Int16,          typeof(SqlInt16),      (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType SqlInt32         = new (DataType.Int32,          typeof(SqlInt32),      (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType SqlInt64         = new (DataType.Int64,          typeof(SqlInt64),      (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType SqlSingle        = new (DataType.Single,         typeof(SqlSingle),     (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType SqlBoolean       = new (DataType.Boolean,        typeof(SqlBoolean),    (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType SqlDouble        = new (DataType.Double,         typeof(SqlDouble),     (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType SqlDateTime      = new (DataType.DateTime,       typeof(SqlDateTime),   (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType SqlDecimal       = new (DataType.Decimal,        typeof(SqlDecimal),          null, GetMaxPrecision,  10, null);
		public static readonly SqlDataType SqlMoney         = new (DataType.Money,          typeof(SqlMoney),            null, GetMaxPrecision,   4, null);
		public static readonly SqlDataType SqlString        = new (DataType.NVarChar,       typeof(SqlString),   GetMaxLength,           null, null, null);
		public static readonly SqlDataType SqlBinary        = new (DataType.Binary,         typeof(SqlBinary),   GetMaxLength,           null, null, null);
		public static readonly SqlDataType SqlGuid          = new (DataType.Guid,           typeof(SqlGuid),       (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType SqlBytes         = new (DataType.Image,          typeof(SqlBytes),    GetMaxLength,           null, null, null);
		public static readonly SqlDataType SqlChars         = new (DataType.Text,           typeof(SqlChars),    GetMaxLength,           null, null, null);
		public static readonly SqlDataType SqlXml           = new (DataType.Xml,            typeof(SqlXml),        (int?)null,     (int?)null, null, null);

		// types without default .net type mapping
		public static readonly SqlDataType DbDecFloat       = new (DataType.DecFloat,       typeof(object),        (int?)null,     (int?)null, null, null);
		public static readonly SqlDataType DbTimeTZ         = new (DataType.TimeTZ,         typeof(object),        (int?)null,     (int?)null, null, null);
		#endregion

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region ISqlExpression Members

		public int  Precedence => SqlQuery.Precedence.Primary;
		public Type SystemType => Type.SystemType;

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			if (this == other)
				return true;

			return other is SqlDataType type && Type.Equals(type.Type);
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public bool CanBeNull => false;

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return ((ISqlExpression)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.SqlDataType;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer.Append(Type.DataType);

			if (!string.IsNullOrEmpty(Type.DbType))
				writer.Append(":\"").Append(Type.DbType).Append('"');

			if (Type.Length != null && Type.Length != 0)
				writer.Append('(').Append(Type.Length).Append(')');
			else if (Type.Precision != null && Type.Precision != 0)
				writer.Append('(').Append(Type.Precision).Append(',').Append(Type.Scale).Append(')');

			return writer;
		}

		#endregion

		#region IEquatable<SqlDataType>

		public override int GetHashCode()
		{
			return Type.GetHashCode();
		}

		public bool Equals(SqlDataType? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return Type.Equals(other.Type);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;

			return Equals((SqlDataType)obj);
		}

		#endregion
	}
}
