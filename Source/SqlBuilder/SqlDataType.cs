using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !SILVERLIGHT
using System.Data.SqlTypes;
#endif

namespace LinqToDB.SqlBuilder
{
	using LinqToDB.Extensions;

	public class SqlDataType : ISqlExpression
	{
		#region Init

		public SqlDataType(DataType dbType)
		{
			var defaultType = GetDataType(dbType);

			DataType  = dbType;
			Type      = defaultType.Type;
			Length    = defaultType.Length;
			Precision = defaultType.Precision;
			Scale     = defaultType.Scale;
		}

		public SqlDataType(DataType dbType, int length)
		{
			if (length <= 0) throw new ArgumentOutOfRangeException("length");

			DataType = dbType;
			Type     = GetDataType(dbType).Type;
			Length   = length;
		}

		public SqlDataType(DataType dbType, int precision, int scale)
		{
			if (precision <= 0) throw new ArgumentOutOfRangeException("precision");
			if (scale     <  0) throw new ArgumentOutOfRangeException("scale");

			DataType  = dbType;
			Type      = GetDataType(dbType).Type;
			Precision = precision;
			Scale     = scale;
		}

		public SqlDataType([JetBrains.Annotations.NotNull]Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var defaultType = GetDataType(type);

			DataType  = defaultType.DataType;
			Type      = type;
			Length    = defaultType.Length;
			Precision = defaultType.Precision;
			Scale     = defaultType.Scale;
		}

		public SqlDataType([JetBrains.Annotations.NotNull] Type type, int length)
		{
			if (type   == null) throw new ArgumentNullException      ("type");
			if (length <= 0)    throw new ArgumentOutOfRangeException("length");

			DataType = GetDataType(type).DataType;
			Type     = type;
			Length   = length;
		}

		public SqlDataType([JetBrains.Annotations.NotNull] Type type, int precision, int scale)
		{
			if (type  == null)  throw new ArgumentNullException      ("type");
			if (precision <= 0) throw new ArgumentOutOfRangeException("precision");
			if (scale     <  0) throw new ArgumentOutOfRangeException("scale");

			DataType  = GetDataType(type).DataType;
			Type      = type;
			Precision = precision;
			Scale     = scale;
		}

		public SqlDataType(DataType dbType, [JetBrains.Annotations.NotNull]Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var defaultType = GetDataType(dbType);

			DataType  = dbType;
			Type      = type;
			Length    = defaultType.Length;
			Precision = defaultType.Precision;
			Scale     = defaultType.Scale;
		}

		public SqlDataType(DataType dbType, [JetBrains.Annotations.NotNull] Type type, int length)
		{
			if (type   == null) throw new ArgumentNullException      ("type");
			if (length <= 0)    throw new ArgumentOutOfRangeException("length");

			DataType = dbType;
			Type     = type;
			Length   = length;
		}

		public SqlDataType(DataType dbType, [JetBrains.Annotations.NotNull] Type type, int precision, int scale)
		{
			if (type  == null)  throw new ArgumentNullException      ("type");
			if (precision <= 0) throw new ArgumentOutOfRangeException("precision");
			if (scale     <  0) throw new ArgumentOutOfRangeException("scale");

			DataType  = dbType;
			Type      = type;
			Precision = precision;
			Scale     = scale;
		}

		#endregion

		#region Public Members

		public DataType DataType  { get; private set; }
		public Type     Type      { get; private set; }
		public int      Length    { get; private set; }
		public int      Precision { get; private set; }
		public int      Scale     { get; private set; }

		#endregion

		#region Static Members

		struct TypeInfo
		{
			public TypeInfo(DataType dbType, int maxLength, int maxPrecision, int maxScale, int maxDisplaySize)
			{
				DataType       = dbType;
				MaxLength      = maxLength;
				MaxPrecision   = maxPrecision;
				MaxScale       = maxScale;
				MaxDisplaySize = maxDisplaySize;
			}

			public readonly DataType DataType;
			public readonly int      MaxLength;
			public readonly int      MaxPrecision;
			public readonly int      MaxScale;
			public readonly int      MaxDisplaySize;
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
			return obj.ToString().Length;
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

			new TypeInfo(DataType.DateTime,             8,                    -1,                    -1,                      23),
#if !MONO
			new TypeInfo(DataType.DateTime2,            8,                    -1,                    -1,                      27),
#endif				
			new TypeInfo(DataType.SmallDateTime,        4,                    -1,                    -1,                      19),
			new TypeInfo(DataType.Date,                 3,                    -1,                    -1,                      10),
			new TypeInfo(DataType.Time,                 5,                    -1,                    -1,                      16),
#if !MONO
			new TypeInfo(DataType.DateTimeOffset,      10,                    -1,                    -1,                      34),
#endif

			new TypeInfo(DataType.Char,              8000,                    -1,                    -1,                    8000),
			new TypeInfo(DataType.VarChar,           8000,                    -1,                    -1,                    8000),
			new TypeInfo(DataType.Text,      int.MaxValue,                    -1,                    -1,            int.MaxValue),
			new TypeInfo(DataType.NChar,             4000,                    -1,                    -1,                    4000),
			new TypeInfo(DataType.NVarChar,          4000,                    -1,                    -1,                    4000),
			new TypeInfo(DataType.NText,     int.MaxValue,                    -1,                    -1,        int.MaxValue / 2),

			new TypeInfo(DataType.Binary,            8000,                    -1,                    -1,                      -1),
			new TypeInfo(DataType.VarBinary,         8000,                    -1,                    -1,                      -1),
			new TypeInfo(DataType.Image,     int.MaxValue,                    -1,                    -1,                      -1),

			new TypeInfo(DataType.Timestamp,            8,                    -1,                    -1,                      -1),
			new TypeInfo(DataType.Guid,                16,                    -1,                    -1,                      36),

			new TypeInfo(DataType.Variant,             -1,                    -1,                    -1,                      -1),
			new TypeInfo(DataType.Xml,                 -1,                    -1,                    -1,                      -1),
			new TypeInfo(DataType.Udt,                 -1,                    -1,                    -1,                      -1)
		);

		public static int GetMaxLength     (DataType dbType) { return _typeInfo[(int)dbType].MaxLength;      }
		public static int GetMaxPrecision  (DataType dbType) { return _typeInfo[(int)dbType].MaxPrecision;   }
		public static int GetMaxScale      (DataType dbType) { return _typeInfo[(int)dbType].MaxScale;       }
		public static int GetMaxDisplaySize(DataType dbType) { return _typeInfo[(int)dbType].MaxDisplaySize; }

		public static SqlDataType GetDataType(Type type)
		{
			var underlyingType = type;

			if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(Nullable<>))
				underlyingType = underlyingType.GetGenericArguments()[0];

			if (underlyingType.IsEnum)
				underlyingType = Enum.GetUnderlyingType(underlyingType);

			switch (Type.GetTypeCode(underlyingType))
			{
				case TypeCode.Boolean  : return Boolean;
				case TypeCode.Char     : return Char;
				case TypeCode.SByte    : return SByte;
				case TypeCode.Byte     : return Byte;
				case TypeCode.Int16    : return Int16;
				case TypeCode.UInt16   : return UInt16;
				case TypeCode.Int32    : return Int32;
				case TypeCode.UInt32   : return UInt32;
				case TypeCode.Int64    : return DbInt64;
				case TypeCode.UInt64   : return UInt64;
				case TypeCode.Single   : return Single;
				case TypeCode.Double   : return Double;
				case TypeCode.Decimal  : return Decimal;
				case TypeCode.DateTime : return DateTime;
				case TypeCode.String   : return String;
				case TypeCode.Object   :
					if (underlyingType == typeof(Guid))           return Guid;
					if (underlyingType == typeof(byte[]))         return ByteArray;
					if (underlyingType == typeof(System.Data.Linq.Binary)) return LinqBinary;
					if (underlyingType == typeof(char[]))         return CharArray;
#if !MONO
					if (underlyingType == typeof(DateTimeOffset)) return DateTimeOffset;
#endif
					if (underlyingType == typeof(TimeSpan))       return TimeSpan;
					break;

				case TypeCode.DBNull   :
				case TypeCode.Empty    :
				default                : break;
			}

#if !SILVERLIGHT

			if (underlyingType == typeof(SqlByte))     return SqlByte;
			if (underlyingType == typeof(SqlInt16))    return SqlInt16;
			if (underlyingType == typeof(SqlInt32))    return SqlInt32;
			if (underlyingType == typeof(SqlInt64))    return SqlInt64;
			if (underlyingType == typeof(SqlSingle))   return SqlSingle;
			if (underlyingType == typeof(SqlBoolean))  return SqlBoolean;
			if (underlyingType == typeof(SqlDouble))   return SqlDouble;
			if (underlyingType == typeof(SqlDateTime)) return SqlDateTime;
			if (underlyingType == typeof(SqlDecimal))  return SqlDecimal;
			if (underlyingType == typeof(SqlMoney))    return SqlMoney;
			if (underlyingType == typeof(SqlString))   return SqlString;
			if (underlyingType == typeof(SqlBinary))   return SqlBinary;
			if (underlyingType == typeof(SqlGuid))     return SqlGuid;
			if (underlyingType == typeof(SqlBytes))    return SqlBytes;
			if (underlyingType == typeof(SqlChars))    return SqlChars;
			if (underlyingType == typeof(SqlXml))      return SqlXml;

#endif

			return DbVariant;
		}

		public static SqlDataType GetDataType(DataType type)
		{
			switch (type)
			{
				case DataType.Int64            : return DbInt64;
				case DataType.Binary           : return DbBinary;
				case DataType.Boolean          : return DbBoolean;
				case DataType.Char             : return DbChar;
				case DataType.DateTime         : return DbDateTime;
				case DataType.Decimal          : return DbDecimal;
				case DataType.Double           : return DbDouble;
				case DataType.Image            : return DbImage;
				case DataType.Int32            : return DbInt32;
				case DataType.Money            : return DbMoney;
				case DataType.NChar            : return DbNChar;
				case DataType.NText            : return DbNText;
				case DataType.NVarChar         : return DbNVarChar;
				case DataType.Single           : return DbSingle;
				case DataType.Guid             : return DbGuid;
				case DataType.SmallDateTime    : return DbSmallDateTime;
				case DataType.Int16            : return DbInt16;
				case DataType.SmallMoney       : return DbSmallMoney;
				case DataType.Text             : return DbText;
				case DataType.Timestamp        : return DbTimestamp;
				case DataType.Byte             : return DbByte;
				case DataType.VarBinary        : return DbVarBinary;
				case DataType.VarChar          : return DbVarChar;
				case DataType.Variant          : return DbVariant;
#if !SILVERLIGHT
				case DataType.Xml              : return DbXml;
#endif
				case DataType.Udt              : return DbUdt;
				case DataType.Date             : return DbDate;
				case DataType.Time             : return DbTime;
#if !MONO
				case DataType.DateTime2        : return DbDateTime2;
				case DataType.DateTimeOffset   : return DbDateTimeOffset;
#endif
			}

			throw new InvalidOperationException();
		}

		public static bool CanBeNull(Type type)
		{
			if (type.IsValueType == false ||
				type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
#if !SILVERLIGHT
				|| typeof(INullable).IsSameOrParentOf(type)
#endif
				)
				return true;

			return false;
		}

		#endregion

		#region Default Types

		internal SqlDataType(DataType dbType, Type type, int length, int precision, int scale)
		{
			DataType  = dbType;
			Type      = type;
			Length    = length;
			Precision = precision;
			Scale     = scale;
		}

		SqlDataType(DataType dbType, Type type, Func<DataType,int> length, int precision, int scale)
			: this(dbType, type, length(dbType), precision, scale)
		{
		}

		SqlDataType(DataType dbType, Type type, int length, Func<DataType,int> precision, int scale)
			: this(dbType, type, length, precision(dbType), scale)
		{
		}

		public static readonly SqlDataType DbInt64          = new SqlDataType(DataType.Int64,          typeof(Int64),                  0, 0,                0);
		public static readonly SqlDataType DbInt32          = new SqlDataType(DataType.Int32,          typeof(Int32),                  0, 0,                0);
		public static readonly SqlDataType DbInt16          = new SqlDataType(DataType.Int16,          typeof(Int16),                  0, 0,                0);
		public static readonly SqlDataType DbSByte          = new SqlDataType(DataType.SByte,          typeof(SByte),                  0, 0,                0);
		public static readonly SqlDataType DbByte           = new SqlDataType(DataType.Byte,           typeof(Byte),                   0, 0,                0);
		public static readonly SqlDataType DbBoolean        = new SqlDataType(DataType.Boolean,        typeof(Boolean),                0, 0,                0);

		public static readonly SqlDataType DbDecimal        = new SqlDataType(DataType.Decimal,        typeof(Decimal),                0, GetMaxPrecision, 10);
		public static readonly SqlDataType DbMoney          = new SqlDataType(DataType.Money,          typeof(Decimal),                0, GetMaxPrecision,  4);
		public static readonly SqlDataType DbSmallMoney     = new SqlDataType(DataType.SmallMoney,     typeof(Decimal),                0, GetMaxPrecision,  4);
		public static readonly SqlDataType DbDouble         = new SqlDataType(DataType.Double,         typeof(Double),                 0,               0,  0);
		public static readonly SqlDataType DbSingle         = new SqlDataType(DataType.Single,         typeof(Single),                 0,               0,  0);

		public static readonly SqlDataType DbDateTime       = new SqlDataType(DataType.DateTime,       typeof(DateTime),               0,               0,  0);
#if !MONO
		public static readonly SqlDataType DbDateTime2      = new SqlDataType(DataType.DateTime2,      typeof(DateTime),               0,               0,  0);
#else		
		public static readonly SqlDataType DbDateTime2      = new SqlDataType(DataType.DateTime,       typeof(DateTime),               0,               0,  0);
#endif		
		public static readonly SqlDataType DbSmallDateTime  = new SqlDataType(DataType.SmallDateTime,  typeof(DateTime),               0,               0,  0);
		public static readonly SqlDataType DbDate           = new SqlDataType(DataType.Date,           typeof(DateTime),               0,               0,  0);
		public static readonly SqlDataType DbTime           = new SqlDataType(DataType.Time,           typeof(TimeSpan),               0,               0,  0);
#if !MONO
		public static readonly SqlDataType DbDateTimeOffset = new SqlDataType(DataType.DateTimeOffset, typeof(DateTimeOffset),         0,               0,  0);
#endif

		public static readonly SqlDataType DbChar           = new SqlDataType(DataType.Char,           typeof(String),      GetMaxLength,               0,  0);
		public static readonly SqlDataType DbVarChar        = new SqlDataType(DataType.VarChar,        typeof(String),      GetMaxLength,               0,  0);
		public static readonly SqlDataType DbText           = new SqlDataType(DataType.Text,           typeof(String),      GetMaxLength,               0,  0);
		public static readonly SqlDataType DbNChar          = new SqlDataType(DataType.NChar,          typeof(String),      GetMaxLength,               0,  0);
		public static readonly SqlDataType DbNVarChar       = new SqlDataType(DataType.NVarChar,       typeof(String),      GetMaxLength,               0,  0);
		public static readonly SqlDataType DbNText          = new SqlDataType(DataType.NText,          typeof(String),      GetMaxLength,               0,  0);

		public static readonly SqlDataType DbBinary         = new SqlDataType(DataType.Binary,         typeof(Byte[]),      GetMaxLength,               0,  0);
		public static readonly SqlDataType DbVarBinary      = new SqlDataType(DataType.VarBinary,      typeof(Byte[]),      GetMaxLength,               0,  0);
		public static readonly SqlDataType DbImage          = new SqlDataType(DataType.Image,          typeof(Byte[]),      GetMaxLength,               0,  0);

		public static readonly SqlDataType DbTimestamp      = new SqlDataType(DataType.Timestamp,      typeof(Byte[]),                 0,               0,  0);
		public static readonly SqlDataType DbGuid           = new SqlDataType(DataType.Guid,           typeof(Guid),                   0,               0,  0);

		public static readonly SqlDataType DbVariant        = new SqlDataType(DataType.Variant,        typeof(Object),                 0,               0,  0);
#if !SILVERLIGHT
		public static readonly SqlDataType DbXml            = new SqlDataType(DataType.Xml,            typeof(SqlXml),                 0,               0,  0);
#endif
		public static readonly SqlDataType DbUdt            = new SqlDataType(DataType.Udt,            typeof(Object),                 0,               0,  0);

		public static readonly SqlDataType Boolean          = DbBoolean;
		public static readonly SqlDataType Char             = new SqlDataType(DataType.Char,           typeof(Char),                   1,               0,  0);
		public static readonly SqlDataType SByte            = DbSByte;
		public static readonly SqlDataType Byte             = DbByte;
		public static readonly SqlDataType Int16            = DbInt16;
		public static readonly SqlDataType UInt16           = new SqlDataType(DataType.UInt16,         typeof(UInt16),                 0,               0,  0);
		public static readonly SqlDataType Int32            = DbInt32;
		public static readonly SqlDataType UInt32           = new SqlDataType(DataType.UInt32,         typeof(UInt32),                 0,               0,  0);
		public static readonly SqlDataType UInt64           = new SqlDataType(DataType.UInt64,         typeof(UInt64),                 0, ulong.MaxValue.ToString().Length, 0);
		public static readonly SqlDataType Single           = DbSingle;
		public static readonly SqlDataType Double           = DbDouble;
		public static readonly SqlDataType Decimal          = DbDecimal;
		public static readonly SqlDataType DateTime         = DbDateTime2;
		public static readonly SqlDataType String           = DbNVarChar;
		public static readonly SqlDataType Guid             = DbGuid;
		public static readonly SqlDataType ByteArray        = DbVarBinary;
		public static readonly SqlDataType LinqBinary       = DbVarBinary;
		public static readonly SqlDataType CharArray        = new SqlDataType(DataType.NVarChar,       typeof(Char[]),      GetMaxLength,               0,  0);
#if !MONO
		public static readonly SqlDataType DateTimeOffset   = DbDateTimeOffset;
#endif
		public static readonly SqlDataType TimeSpan         = DbTime;

#if !SILVERLIGHT
		public static readonly SqlDataType SqlByte          = new SqlDataType(DataType.Byte,           typeof(SqlByte),                0,               0,  0);
		public static readonly SqlDataType SqlInt16         = new SqlDataType(DataType.Int16,          typeof(SqlInt16),               0,               0,  0);
		public static readonly SqlDataType SqlInt32         = new SqlDataType(DataType.Int32,          typeof(SqlInt32),               0,               0,  0);
		public static readonly SqlDataType SqlInt64         = new SqlDataType(DataType.Int64,          typeof(SqlInt64),               0,               0,  0);
		public static readonly SqlDataType SqlSingle        = new SqlDataType(DataType.Single,         typeof(SqlSingle),              0,               0,  0);
		public static readonly SqlDataType SqlBoolean       = new SqlDataType(DataType.Boolean,        typeof(SqlBoolean),             0,               0,  0);
		public static readonly SqlDataType SqlDouble        = new SqlDataType(DataType.Double,         typeof(SqlDouble),              0,               0,  0);
		public static readonly SqlDataType SqlDateTime      = new SqlDataType(DataType.DateTime,       typeof(SqlDateTime),            0,               0,  0);
		public static readonly SqlDataType SqlDecimal       = new SqlDataType(DataType.Decimal,        typeof(SqlDecimal),             0, GetMaxPrecision, 10);
		public static readonly SqlDataType SqlMoney         = new SqlDataType(DataType.Money,          typeof(SqlMoney),               0, GetMaxPrecision,  4);
		public static readonly SqlDataType SqlString        = new SqlDataType(DataType.NVarChar,       typeof(SqlString),   GetMaxLength,               0,  0);
		public static readonly SqlDataType SqlBinary        = new SqlDataType(DataType.Binary,         typeof(SqlBinary),   GetMaxLength,               0,  0);
		public static readonly SqlDataType SqlGuid          = new SqlDataType(DataType.Guid,           typeof(SqlGuid),                0,               0,  0);
		public static readonly SqlDataType SqlBytes         = new SqlDataType(DataType.Image,          typeof(SqlBytes),    GetMaxLength,               0,  0);
		public static readonly SqlDataType SqlChars         = new SqlDataType(DataType.Text,           typeof(SqlChars),    GetMaxLength,               0,  0);
		public static readonly SqlDataType SqlXml           = new SqlDataType(DataType.Xml,            typeof(SqlXml),                 0,               0,  0);
#endif

		#endregion

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpression Members

		public int Precedence
		{
			get { return SqlBuilder.Precedence.Primary; }
		}

		public Type SystemType
		{
			get { return typeof(Type); }
		}

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			return func(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			if (this == other)
				return true;

			var value = (SqlDataType)other;
			return Type == value.Type && Length == value.Length && Precision == value.Precision && Scale == value.Scale;
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull()
		{
			return false;
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return ((ISqlExpression)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
				objectTree.Add(this, clone = new SqlDataType(DataType, Type, Length, Precision, Scale));

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.SqlDataType; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append(this.DataType);

			if (Length != 0)
				sb.Append('(').Append(Length).Append(')');
			else if (Precision != 0)
				sb.Append('(').Append(Precision).Append(',').Append(Scale).Append(')');

			return sb;
		}

		#endregion
	}
}
