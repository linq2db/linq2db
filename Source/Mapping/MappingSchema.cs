using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Properties;
using LinqToDB.Reflection;
using LinqToDB.Reflection.Extension;
using LinqToDB.Reflection.MetadataProvider;

#region ReSharper disable
// ReSharper disable SuggestUseVarKeywordEvident
// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable RedundantTypeArgumentsOfMethod
#endregion

using KeyValue = System.Collections.Generic.KeyValuePair<System.Type,System.Type>;
using Convert  = LinqToDB.Common.Convert;

namespace LinqToDB.Mapping
{
	public class MappingSchema
	{
		#region Constructors

		public MappingSchema()
		{
			InitNullValues();
		}

		#endregion

		#region ObjectMapper Support

		private readonly Dictionary<Type,ObjectMapper> _mappers        = new Dictionary<Type,ObjectMapper>();
		private readonly Dictionary<Type,ObjectMapper> _pendingMappers = new Dictionary<Type,ObjectMapper>();

		public ObjectMapper GetObjectMapper(Type type)
		{
			ObjectMapper om;

			lock (_mappers)
			{
				if (_mappers.TryGetValue(type, out om))
					return om;

				// This object mapper is initializing right now.
				// Note that only one thread can access to _pendingMappers each time.
				//
				if (_pendingMappers.TryGetValue(type, out om))
					return om;

				om = CreateObjectMapper(type);

				if (om == null)
					throw new MappingException(
						string.Format("Cannot create object mapper for the '{0}' type.", type.FullName));

				_pendingMappers.Add(type, om);

				try
				{
					om.Init(this, type);
				}
				finally
				{
					_pendingMappers.Remove(type);
				}

				// Officially publish this ready to use object mapper.
				//
				SetObjectMapperInternal(type, om);

				return om;
			}
		}

		private void SetObjectMapperInternal(Type type, ObjectMapper om)
		{
			_mappers.Add(type, om);

			if (type.IsAbstract)
			{
				var actualType = TypeAccessor.GetAccessor(type).Type;

				if (!_mappers.ContainsKey(actualType))
					_mappers.Add(actualType, om);
			}
		}

		public void SetObjectMapper(Type type, ObjectMapper om)
		{
			if (type == null) throw new ArgumentNullException("type");

			lock (_mappers)
				SetObjectMapperInternal(type, om);
		}

		protected virtual ObjectMapper CreateObjectMapper(Type type)
		{
			Attribute attr = ReflectionExtensions.GetFirstAttribute(type, typeof(ObjectMapperAttribute));
			return attr == null? CreateObjectMapperInstance(type): ((ObjectMapperAttribute)attr).ObjectMapper;
		}

		protected virtual ObjectMapper CreateObjectMapperInstance(Type type)
		{
			return new ObjectMapper();
		}

		#endregion

		#region MetadataProvider

		private MetadataProviderBase _metadataProvider;
		public  MetadataProviderBase  MetadataProvider
		{
			[DebuggerStepThrough]
			get { return _metadataProvider ?? (_metadataProvider = CreateMetadataProvider()); }
			set { _metadataProvider = value; }
		}

		protected virtual MetadataProviderBase CreateMetadataProvider()
		{
			return MetadataProviderBase.CreateProvider();
		}

		#endregion

		#region Public Members

		public ExtensionList Extensions { get; set; }

		#endregion

		#region Convert

		public virtual void InitNullValues()
		{
			DefaultSByteNullValue          = (SByte)         GetNullValue(typeof(SByte));
			DefaultInt16NullValue          = (Int16)         GetNullValue(typeof(Int16));
			DefaultInt32NullValue          = (Int32)         GetNullValue(typeof(Int32));
			DefaultInt64NullValue          = (Int64)         GetNullValue(typeof(Int64));
			DefaultByteNullValue           = (Byte)          GetNullValue(typeof(Byte));
			DefaultUInt16NullValue         = (UInt16)        GetNullValue(typeof(UInt16));
			DefaultUInt32NullValue         = (UInt32)        GetNullValue(typeof(UInt32));
			DefaultUInt64NullValue         = (UInt64)        GetNullValue(typeof(UInt64));
			DefaultCharNullValue           = (Char)          GetNullValue(typeof(Char));
			DefaultSingleNullValue         = (Single)        GetNullValue(typeof(Single));
			DefaultDoubleNullValue         = (Double)        GetNullValue(typeof(Double));
			DefaultBooleanNullValue        = (Boolean)       GetNullValue(typeof(Boolean));

			DefaultStringNullValue         = (String)        GetNullValue(typeof(String));
			DefaultDateTimeNullValue       = (DateTime)      GetNullValue(typeof(DateTime));
			DefaultDateTimeOffsetNullValue = (DateTimeOffset)GetNullValue(typeof(DateTimeOffset));
			DefaultLinqBinaryNullValue     = (Binary)        GetNullValue(typeof(Binary));
			DefaultDecimalNullValue        = (Decimal)       GetNullValue(typeof(Decimal));
			DefaultGuidNullValue           = (Guid)          GetNullValue(typeof(Guid));
			DefaultStreamNullValue         = (Stream)        GetNullValue(typeof(Stream));
#if !SILVERLIGHT
			DefaultXmlReaderNullValue      = (XmlReader)     GetNullValue(typeof(XmlReader));
			DefaultXmlDocumentNullValue    = (XmlDocument)   GetNullValue(typeof(XmlDocument));
#endif
		}

		#region Primitive Types

		[CLSCompliant(false)]
		public sbyte DefaultSByteNullValue { get; set; }

		[CLSCompliant(false)]
		public virtual SByte ConvertToSByte(object value)
		{
			return
				value is SByte ? (SByte)value :
				value == null ? DefaultSByteNullValue :
					Convert.ToSByte(value);
		}

		public short DefaultInt16NullValue { get; set; }

		public virtual Int16 ConvertToInt16(object value)
		{
			return
				value is Int16? (Int16)value:
				value == null || value is DBNull? DefaultInt16NullValue:
					Convert.ToInt16(value);
		}

		public int DefaultInt32NullValue { get; set; }

		public virtual Int32 ConvertToInt32(object value)
		{
			return
				value is Int32? (Int32)value:
				value == null || value is DBNull? DefaultInt32NullValue:
					Convert.ToInt32(value);
		}

		public long DefaultInt64NullValue { get; set; }

		public virtual Int64 ConvertToInt64(object value)
		{
			return
				value is Int64? (Int64)value:
				value == null || value is DBNull? DefaultInt64NullValue:
					Convert.ToInt64(value);
		}

		public byte DefaultByteNullValue { get; set; }

		public virtual Byte ConvertToByte(object value)
		{
			return
				value is Byte? (Byte)value:
				value == null || value is DBNull? DefaultByteNullValue:
					Convert.ToByte(value);
		}

		[CLSCompliant(false)]
		public ushort DefaultUInt16NullValue { get; set; }

		[CLSCompliant(false)]
		public virtual UInt16 ConvertToUInt16(object value)
		{
			return
				value is UInt16? (UInt16)value:
				value == null || value is DBNull? DefaultUInt16NullValue:
					Convert.ToUInt16(value);
		}

		[CLSCompliant(false)]
		public uint DefaultUInt32NullValue { get; set; }

		[CLSCompliant(false)]
		public virtual UInt32 ConvertToUInt32(object value)
		{
			return
				value is UInt32? (UInt32)value:
				value == null || value is DBNull? DefaultUInt32NullValue:
					Convert.ToUInt32(value);
		}

		[CLSCompliant(false)]
		public ulong DefaultUInt64NullValue { get; set; }

		[CLSCompliant(false)]
		public virtual UInt64 ConvertToUInt64(object value)
		{
			return
				value is UInt64? (UInt64)value:
				value == null || value is DBNull? DefaultUInt64NullValue:
					Convert.ToUInt64(value);
		}

		public char DefaultCharNullValue { get; set; }

		public virtual Char ConvertToChar(object value)
		{
			return
				value is Char? (Char)value:
				value == null || value is DBNull? DefaultCharNullValue:
					Convert.ToChar(value);
		}

		public float DefaultSingleNullValue { get; set; }

		public virtual Single ConvertToSingle(object value)
		{
			return
				value is Single? (Single)value:
				value == null || value is DBNull? DefaultSingleNullValue:
					Convert.ToSingle(value);
		}

		public double DefaultDoubleNullValue { get; set; }

		public virtual Double ConvertToDouble(object value)
		{
			return
				value is Double? (Double)value:
				value == null || value is DBNull? DefaultDoubleNullValue:
					Convert.ToDouble(value);
		}

		public bool DefaultBooleanNullValue { get; set; }

		public virtual Boolean ConvertToBoolean(object value)
		{
			return
				value is Boolean? (Boolean)value:
				value == null || value is DBNull? DefaultBooleanNullValue:
					Convert.ToBoolean(value);
		}

		#endregion

		#region Simple Types

		public string DefaultStringNullValue { get; set; }

		public virtual String ConvertToString(object value)
		{
			return
				value is String? (String)value :
				value == null || value is DBNull? DefaultStringNullValue:
					Convert.ToString(value);
		}

		public DateTime DefaultDateTimeNullValue { get; set; }

		public virtual DateTime ConvertToDateTime(object value)
		{
			return
				value is DateTime? (DateTime)value:
				value == null || value is DBNull? DefaultDateTimeNullValue:
					Convert.ToDateTime(value);
		}

		public virtual TimeSpan ConvertToTimeSpan(object value)
		{
			return ConvertToDateTime(value).TimeOfDay;
		}

		public DateTimeOffset DefaultDateTimeOffsetNullValue { get; set; }

		public virtual DateTimeOffset ConvertToDateTimeOffset(object value)
		{
			return
				value is DateTimeOffset? (DateTimeOffset)value:
				value == null || value is DBNull? DefaultDateTimeOffsetNullValue:
					Convert.ToDateTimeOffset(value);
		}

		public Binary DefaultLinqBinaryNullValue { get; set; }

		public virtual Binary ConvertToLinqBinary(object value)
		{
			return
				value is Binary ? (Binary)value:
				value is byte[] ? new Binary((byte[])value) : 
				value == null || value is DBNull? DefaultLinqBinaryNullValue:
					Convert.ToLinqBinary(value);
		}

		public decimal DefaultDecimalNullValue { get; set; }

		public virtual Decimal ConvertToDecimal(object value)
		{
			return
				value is Decimal? (Decimal)value:
				value == null || value is DBNull? DefaultDecimalNullValue:
					Convert.ToDecimal(value);
		}

		public Guid DefaultGuidNullValue { get; set; }

		public virtual Guid ConvertToGuid(object value)
		{
			return
				value is Guid? (Guid)value:
				value == null || value is DBNull? DefaultGuidNullValue:
					Convert.ToGuid(value);
		}

		public Stream DefaultStreamNullValue { get; set; }

		public virtual Stream ConvertToStream(object value)
		{
			return
				value is Stream? (Stream)value:
				value == null || value is DBNull? DefaultStreamNullValue:
					 Convert.ToStream(value);
		}

#if !SILVERLIGHT

		public XmlReader DefaultXmlReaderNullValue { get; set; }

		public virtual XmlReader ConvertToXmlReader(object value)
		{
			return
				value is XmlReader? (XmlReader)value:
				value == null || value is DBNull? DefaultXmlReaderNullValue:
					Convert.ToXmlReader(value);
		}

		public XmlDocument DefaultXmlDocumentNullValue { get; set; }

		public virtual XmlDocument ConvertToXmlDocument(object value)
		{
			return
				value is XmlDocument? (XmlDocument)value:
				value == null || value is DBNull? DefaultXmlDocumentNullValue:
					Convert.ToXmlDocument(value);
		}

#endif

		public virtual byte[] ConvertToByteArray(object value)
		{
			return
				value is byte[]? (byte[])value:
				value == null || value is DBNull? null:
					Convert.ToByteArray(value);
		}

		public virtual char[] ConvertToCharArray(object value)
		{
			return
				value is char[]? (char[])value:
				value == null || value is DBNull? null:
					Convert.ToCharArray(value);
		}

		#endregion

		#region Nullable Types

		[CLSCompliant(false)]
		public virtual SByte? ConvertToNullableSByte(object value)
		{
			return
				value is SByte? (SByte?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableSByte(value);
		}

		public virtual Int16? ConvertToNullableInt16(object value)
		{
			return
				value is Int16? (Int16?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableInt16(value);
		}

		public virtual Int32? ConvertToNullableInt32(object value)
		{
			return
				value is Int32? (Int32?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableInt32(value);
		}

		public virtual Int64? ConvertToNullableInt64(object value)
		{
			return
				value is Int64? (Int64?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableInt64(value);
		}

		public virtual Byte? ConvertToNullableByte(object value)
		{
			return
				value is Byte? (Byte?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableByte(value);
		}

		[CLSCompliant(false)]
		public virtual UInt16? ConvertToNullableUInt16(object value)
		{
			return
				value is UInt16? (UInt16?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableUInt16(value);
		}

		[CLSCompliant(false)]
		public virtual UInt32? ConvertToNullableUInt32(object value)
		{
			return
				value is UInt32? (UInt32?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableUInt32(value);
		}

		[CLSCompliant(false)]
		public virtual UInt64? ConvertToNullableUInt64(object value)
		{
			return
				value is UInt64? (UInt64?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableUInt64(value);
		}

		public virtual Char? ConvertToNullableChar(object value)
		{
			return
				value is Char? (Char?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableChar(value);
		}

		public virtual Double? ConvertToNullableDouble(object value)
		{
			return
				value is Double? (Double?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableDouble(value);
		}

		public virtual Single? ConvertToNullableSingle(object value)
		{
			return
				value is Single? (Single?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableSingle(value);
		}

		public virtual Boolean? ConvertToNullableBoolean(object value)
		{
			return
				value is Boolean? (Boolean?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableBoolean(value);
		}

		public virtual DateTime? ConvertToNullableDateTime(object value)
		{
			return
				value is DateTime? (DateTime?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableDateTime(value);
		}

		public virtual TimeSpan? ConvertToNullableTimeSpan(object value)
		{
			DateTime? dt = ConvertToNullableDateTime(value);
			return dt == null? null : (TimeSpan?)dt.Value.TimeOfDay;
		}

		public virtual DateTimeOffset? ConvertToNullableDateTimeOffset(object value)
		{
			return
				value is DateTimeOffset? (DateTimeOffset?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableDateTimeOffset(value);
		}

		public virtual Decimal? ConvertToNullableDecimal(object value)
		{
			return
				value is Decimal? (Decimal?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableDecimal(value);
		}

		public virtual Guid? ConvertToNullableGuid(object value)
		{
			return
				value is Guid? (Guid?)value:
				value == null || value is DBNull? null:
					Convert.ToNullableGuid(value);
		}

		#endregion

		#region SqlTypes

#if !SILVERLIGHT

		public virtual SqlByte ConvertToSqlByte(object value)
		{
			return
				value == null || value is DBNull? SqlByte.Null :
				value is SqlByte? (SqlByte)value:
					Convert.ToSqlByte(value);
		}

		public virtual SqlInt16 ConvertToSqlInt16(object value)
		{
			return
				value == null || value is DBNull? SqlInt16.Null:
				value is SqlInt16? (SqlInt16)value:
					Convert.ToSqlInt16(value);
		}

		public virtual SqlInt32 ConvertToSqlInt32(object value)
		{
			return
				value == null || value is DBNull? SqlInt32.Null:
				value is SqlInt32? (SqlInt32)value:
					Convert.ToSqlInt32(value);
		}

		public virtual SqlInt64 ConvertToSqlInt64(object value)
		{
			return
				value == null || value is DBNull? SqlInt64.Null:
				value is SqlInt64? (SqlInt64)value:
					Convert.ToSqlInt64(value);
		}

		public virtual SqlSingle ConvertToSqlSingle(object value)
		{
			return
				value == null || value is DBNull? SqlSingle.Null:
				value is SqlSingle? (SqlSingle)value:
					Convert.ToSqlSingle(value);
		}

		public virtual SqlBoolean ConvertToSqlBoolean(object value)
		{
			return
				value == null || value is DBNull? SqlBoolean.Null:
				value is SqlBoolean? (SqlBoolean)value:
					Convert.ToSqlBoolean(value);
		}

		public virtual SqlDouble ConvertToSqlDouble(object value)
		{
			return
				value == null || value is DBNull? SqlDouble.Null:
				value is SqlDouble? (SqlDouble)value:
					Convert.ToSqlDouble(value);
		}

		public virtual SqlDateTime ConvertToSqlDateTime(object value)
		{
			return
				value == null || value is DBNull? SqlDateTime.Null:
				value is SqlDateTime? (SqlDateTime)value:
					Convert.ToSqlDateTime(value);
		}

		public virtual SqlDecimal ConvertToSqlDecimal(object value)
		{
			return
				value == null || value is DBNull? SqlDecimal.Null:
				value is SqlDecimal? (SqlDecimal)value:
				value is SqlMoney?   ((SqlMoney)value).ToSqlDecimal():
					Convert.ToSqlDecimal(value);
		}

		public virtual SqlMoney ConvertToSqlMoney(object value)
		{
			return
				value == null || value is DBNull? SqlMoney.Null:
				value is SqlMoney?   (SqlMoney)value:
				value is SqlDecimal? ((SqlDecimal)value).ToSqlMoney():
					Convert.ToSqlMoney(value);
		}

		public virtual SqlString ConvertToSqlString(object value)
		{
			return
				value == null || value is DBNull? SqlString.Null:
				value is SqlString? (SqlString)value:
					Convert.ToSqlString(value);
		}

		public virtual SqlBinary ConvertToSqlBinary(object value)
		{
			return
				value == null || value is DBNull? SqlBinary.Null:
				value is SqlBinary? (SqlBinary)value:
					Convert.ToSqlBinary(value);
		}

		public virtual SqlGuid ConvertToSqlGuid(object value)
		{
			return
				value == null || value is DBNull? SqlGuid.Null:
				value is SqlGuid? (SqlGuid)value:
					Convert.ToSqlGuid(value);
		}

		public virtual SqlBytes ConvertToSqlBytes(object value)
		{
			return
				value == null || value is DBNull? SqlBytes.Null:
				value is SqlBytes? (SqlBytes)value:
					Convert.ToSqlBytes(value);
		}

		public virtual SqlChars ConvertToSqlChars(object value)
		{
			return
				value == null || value is DBNull? SqlChars.Null:
				value is SqlChars? (SqlChars)value:
					Convert.ToSqlChars(value);
		}

		public virtual SqlXml ConvertToSqlXml(object value)
		{
			return
				value == null || value is DBNull? SqlXml.Null:
				value is SqlXml? (SqlXml)value:
					Convert.ToSqlXml(value);
		}

#endif

		#endregion

		#region General case

		public virtual T GetDefaultNullValue<T>()
		{
			switch (Type.GetTypeCode(typeof(T)))
			{
				case TypeCode.Boolean:  return (T)(object)DefaultBooleanNullValue;
				case TypeCode.Byte:     return (T)(object)DefaultByteNullValue;
				case TypeCode.Char:     return (T)(object)DefaultCharNullValue;
				case TypeCode.DateTime: return (T)(object)DefaultDateTimeNullValue;
				case TypeCode.Decimal:  return (T)(object)DefaultDecimalNullValue;
				case TypeCode.Double:   return (T)(object)DefaultDoubleNullValue;
				case TypeCode.Int16:    return (T)(object)DefaultInt16NullValue;
				case TypeCode.Int32:    return (T)(object)DefaultInt32NullValue;
				case TypeCode.Int64:    return (T)(object)DefaultInt64NullValue;
				case TypeCode.SByte:    return (T)(object)DefaultSByteNullValue;
				case TypeCode.Single:   return (T)(object)DefaultSingleNullValue;
				case TypeCode.String:   return (T)(object)DefaultStringNullValue;
				case TypeCode.UInt16:   return (T)(object)DefaultUInt16NullValue;
				case TypeCode.UInt32:   return (T)(object)DefaultUInt32NullValue;
				case TypeCode.UInt64:   return (T)(object)DefaultUInt64NullValue;
			}

			if (typeof(Guid)           == typeof(T)) return (T)(object)DefaultGuidNullValue;
			if (typeof(Stream)         == typeof(T)) return (T)(object)DefaultStreamNullValue;
#if !SILVERLIGHT
			if (typeof(XmlReader)      == typeof(T)) return (T)(object)DefaultXmlReaderNullValue;
			if (typeof(XmlDocument)    == typeof(T)) return (T)(object)DefaultXmlDocumentNullValue;
#endif
			if (typeof(DateTimeOffset) == typeof(T)) return (T)(object)DefaultDateTimeOffsetNullValue;

			return default(T);
		}

		public virtual T ConvertTo<T,TP>(TP value)
		{
			return Equals(value, default(TP))?
				GetDefaultNullValue<T>():
				Convert<T,TP>.From(value);
		}

		public virtual object ConvertChangeType(object value, Type conversionType)
		{
			return ConvertChangeType(value, conversionType, ReflectionExtensions.IsNullable(conversionType));
		}

		public virtual object ConvertChangeType(object value, Type conversionType, bool isNullable)
		{
			if (conversionType.IsArray)
			{
				if (null == value)
					return null;
				
				Type srcType = value.GetType();

				if (srcType == conversionType)
					return value;

				if (srcType.IsArray)
				{
					Type srcElementType = srcType.GetElementType();
					Type dstElementType = conversionType.GetElementType();

					if (srcElementType.IsArray != dstElementType.IsArray
						|| (srcElementType.IsArray &&
							srcElementType.GetArrayRank() != dstElementType.GetArrayRank()))
					{
						throw new InvalidCastException(string.Format(
							Resources.MappingSchema_IncompatibleArrayTypes,
							srcType.FullName, conversionType.FullName));
					}

					Array srcArray = (Array)value;
					Array dstArray;

					int rank = srcArray.Rank;

					if (rank == 1 && 0 == srcArray.GetLowerBound(0))
					{
						int arrayLength = srcArray.Length;

						dstArray = Array.CreateInstance(dstElementType, arrayLength);

						// Int32 is assignable from UInt32, SByte from Byte and so on.
						//
						if (dstElementType.IsAssignableFrom(srcElementType))
							Array.Copy(srcArray, dstArray, arrayLength);
						else
							for (int i = 0; i < arrayLength; ++i)
								dstArray.SetValue(ConvertChangeType(srcArray.GetValue(i), dstElementType, isNullable), i);
					}
					else
					{
#if SILVERLIGHT
						throw new InvalidOperationException();
#else
						var arrayLength = 1;
						var dimensions  = new int[rank];
						var indices     = new int[rank];
						var lbounds     = new int[rank];

						for (int i = 0; i < rank; ++i)
						{
							arrayLength *= (dimensions[i] = srcArray.GetLength(i));
							lbounds[i] = srcArray.GetLowerBound(i);
						}

						dstArray = Array.CreateInstance(dstElementType, dimensions, lbounds);

						for (int i = 0; i < arrayLength; ++i)
						{
							var index = i;

							for (var j = rank - 1; j >= 0; --j)
							{
								indices[j] = index % dimensions[j] + lbounds[j];
								index /= dimensions[j];
							}

							dstArray.SetValue(ConvertChangeType(srcArray.GetValue(indices), dstElementType, isNullable), indices);
						}

#endif
					}

					return dstArray;
				}
			}
			else if (conversionType.IsEnum)
				return Enum.ToObject(conversionType, ConvertChangeType(value, Enum.GetUnderlyingType(conversionType), false));

			if (isNullable)
			{
				if (ReflectionExtensions.IsNullable(conversionType))
				{
					// Return a null reference or boxed not null value.
					//
					return value == null || value is DBNull? null:
						ConvertChangeType(value, conversionType.GetGenericArguments()[0]);
				}

				Type type = conversionType.IsEnum? Enum.GetUnderlyingType(conversionType): conversionType;

				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Boolean:  return ConvertToNullableBoolean (value);
					case TypeCode.Byte:     return ConvertToNullableByte    (value);
					case TypeCode.Char:     return ConvertToNullableChar    (value);
					case TypeCode.DateTime: return ConvertToNullableDateTime(value);
					case TypeCode.Decimal:  return ConvertToNullableDecimal (value);
					case TypeCode.Double:   return ConvertToNullableDouble  (value);
					case TypeCode.Int16:    return ConvertToNullableInt16   (value);
					case TypeCode.Int32:    return ConvertToNullableInt32   (value);
					case TypeCode.Int64:    return ConvertToNullableInt64   (value);
					case TypeCode.SByte:    return ConvertToNullableSByte   (value);
					case TypeCode.Single:   return ConvertToNullableSingle  (value);
					case TypeCode.UInt16:   return ConvertToNullableUInt16  (value);
					case TypeCode.UInt32:   return ConvertToNullableUInt32  (value);
					case TypeCode.UInt64:   return ConvertToNullableUInt64  (value);
				}

				if (typeof(Guid)           == conversionType) return ConvertToNullableGuid(value);
				if (typeof(DateTimeOffset) == conversionType) return ConvertToNullableDateTimeOffset(value);
			}

			switch (Type.GetTypeCode(conversionType))
			{
				case TypeCode.Boolean:  return ConvertToBoolean (value);
				case TypeCode.Byte:     return ConvertToByte    (value);
				case TypeCode.Char:     return ConvertToChar    (value);
				case TypeCode.DateTime: return ConvertToDateTime(value);
				case TypeCode.Decimal:  return ConvertToDecimal (value);
				case TypeCode.Double:   return ConvertToDouble  (value);
				case TypeCode.Int16:    return ConvertToInt16   (value);
				case TypeCode.Int32:    return ConvertToInt32   (value);
				case TypeCode.Int64:    return ConvertToInt64   (value);
				case TypeCode.SByte:    return ConvertToSByte   (value);
				case TypeCode.Single:   return ConvertToSingle  (value);
				case TypeCode.String:   return ConvertToString  (value);
				case TypeCode.UInt16:   return ConvertToUInt16  (value);
				case TypeCode.UInt32:   return ConvertToUInt32  (value);
				case TypeCode.UInt64:   return ConvertToUInt64  (value);
			}

			if (typeof(Guid)           == conversionType) return ConvertToGuid          (value);
			if (typeof(Stream)         == conversionType) return ConvertToStream        (value);
#if !SILVERLIGHT
			if (typeof(XmlReader)      == conversionType) return ConvertToXmlReader     (value);
			if (typeof(XmlDocument)    == conversionType) return ConvertToXmlDocument   (value);
#endif
			if (typeof(byte[])         == conversionType) return ConvertToByteArray     (value);
			if (typeof(Binary)         == conversionType) return ConvertToLinqBinary    (value);
			if (typeof(DateTimeOffset) == conversionType) return ConvertToDateTimeOffset(value);
			if (typeof(char[])         == conversionType) return ConvertToCharArray     (value);

#if !SILVERLIGHT

			if (typeof(SqlInt32)       == conversionType) return ConvertToSqlInt32      (value);
			if (typeof(SqlString)      == conversionType) return ConvertToSqlString     (value);
			if (typeof(SqlDecimal)     == conversionType) return ConvertToSqlDecimal    (value);
			if (typeof(SqlDateTime)    == conversionType) return ConvertToSqlDateTime   (value);
			if (typeof(SqlBoolean)     == conversionType) return ConvertToSqlBoolean    (value);
			if (typeof(SqlMoney)       == conversionType) return ConvertToSqlMoney      (value);
			if (typeof(SqlGuid)        == conversionType) return ConvertToSqlGuid       (value);
			if (typeof(SqlDouble)      == conversionType) return ConvertToSqlDouble     (value);
			if (typeof(SqlByte)        == conversionType) return ConvertToSqlByte       (value);
			if (typeof(SqlInt16)       == conversionType) return ConvertToSqlInt16      (value);
			if (typeof(SqlInt64)       == conversionType) return ConvertToSqlInt64      (value);
			if (typeof(SqlSingle)      == conversionType) return ConvertToSqlSingle     (value);
			if (typeof(SqlBinary)      == conversionType) return ConvertToSqlBinary     (value);
			if (typeof(SqlBytes)       == conversionType) return ConvertToSqlBytes      (value);
			if (typeof(SqlChars)       == conversionType) return ConvertToSqlChars      (value);
			if (typeof(SqlXml)         == conversionType) return ConvertToSqlXml        (value);

#endif

			return System.Convert.ChangeType(value, conversionType, Thread.CurrentThread.CurrentCulture);
		}

		#endregion
		
		#endregion

		#region Factory Members

		public virtual DataReaderMapper CreateDataReaderMapper(IDataReader dataReader)
		{
			return new DataReaderMapper(this, dataReader);
		}

		#endregion

		#region GetNullValue

		public virtual object GetNullValue(Type type)
		{
			return TypeAccessor.GetNullValue(type);
		}

		public virtual bool IsNull(object value)
		{
			return TypeAccessor.IsNull(value);
		}

		#endregion

		#region GetMapValues

		private readonly Dictionary<Type,MapValue[]> _mapValues = new Dictionary<Type,MapValue[]>();

		public virtual MapValue[] GetMapValues([JetBrains.Annotations.NotNull] Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			lock (_mapValues)
			{
				MapValue[] mapValues;

				if (_mapValues.TryGetValue(type, out mapValues))
					return mapValues;

				var  typeExt = TypeExtension.GetTypeExtension(type, Extensions);
				bool isSet;

				mapValues = MetadataProvider.GetMapValues(typeExt, type, out isSet);

				_mapValues.Add(type, mapValues);

				return mapValues;
			}
		}

		#endregion

		#region ValueMapper

		[CLSCompliant(false)]
		public virtual IValueMapper DefaultValueMapper
		{
			get { return ValueMapping.DefaultMapper; }
		}

		internal readonly Dictionary<Type,IValueMapper>     SameTypeMappers      = new Dictionary<Type,IValueMapper>();
		internal readonly Dictionary<KeyValue,IValueMapper> DifferentTypeMappers = new Dictionary<KeyValue,IValueMapper>();

		[CLSCompliant(false)]
		public void SetValueMapper(
			Type         sourceType,
			Type         destType,
			IValueMapper mapper)
		{
			if (sourceType == null) sourceType = typeof(object);
			if (destType   == null) destType   = typeof(object);

			if (sourceType == destType)
			{
				lock (SameTypeMappers)
				{
					if (mapper == null)
						SameTypeMappers.Remove(sourceType);
					else if (SameTypeMappers.ContainsKey(sourceType))
						SameTypeMappers[sourceType] = mapper;
					else
						SameTypeMappers.Add(sourceType, mapper);
				}
			}
			else
			{
				KeyValue key = new KeyValue(sourceType, destType);

				lock (DifferentTypeMappers)
				{
					if (mapper == null)
						DifferentTypeMappers.Remove(key);
					else if (DifferentTypeMappers.ContainsKey(key))
						DifferentTypeMappers[key] = mapper;
					else
						DifferentTypeMappers.Add(key, mapper);
				}
			}
		}

		[CLSCompliant(false)]
		protected internal virtual IValueMapper GetValueMapper(
			Type sourceType,
			Type destType)
		{
			return ValueMapping.GetMapper(sourceType, destType);
		}

		[CLSCompliant(false)]
		internal protected IValueMapper[] GetValueMappers(
			IMapDataSource      source,
			IMapDataDestination dest,
			int[]               index)
		{
			IValueMapper[] mappers = new IValueMapper[index.Length];

			for (int i = 0; i < index.Length; i++)
			{
				int n = index[i];

				if (n < 0)
					continue;

				if (!source.SupportsTypedValues(i) || !dest.SupportsTypedValues(n))
				{
					mappers[i] = DefaultValueMapper;
					continue;
				}

				Type sourceType = source.GetFieldType(i);
				Type destType   = dest.  GetFieldType(n);

				if (sourceType == null) sourceType = typeof(object);
				if (destType   == null) destType   = typeof(object);

				IValueMapper t;

				if (sourceType == destType)
				{
					lock (SameTypeMappers)
						if (!SameTypeMappers.TryGetValue(sourceType, out t))
							SameTypeMappers.Add(sourceType, t = GetValueMapper(sourceType, destType));
				}
				else
				{
					var key = new KeyValue(sourceType, destType);

					lock (DifferentTypeMappers)
						if (!DifferentTypeMappers.TryGetValue(key, out t))
								DifferentTypeMappers[key] = t = GetValueMapper(sourceType, destType);
				}

				mappers[i] = t;
			}

			return mappers;
		}

		#endregion

		#region Base Mapping

		[CLSCompliant(false)]
		protected static int[] GetIndex(IMapDataSource source, IMapDataDestination dest)
		{
			int   count = source.Count;
			int[] index = new int[count];

			for (int i = 0; i < count; i++)
				index[i] = dest.GetOrdinal(source.GetName(i));

			return index;
		}

		[CLSCompliant(false)]
		protected virtual void MapInternal(
			IMapDataSource      source,
			object              sourceObject,
			IMapDataDestination dest,
			object              destObject)
		{
			int[]          index   = GetIndex       (source, dest);
			IValueMapper[] mappers = GetValueMappers(source, dest, index);

			for (int i = 0; i < index.Length; i++)
			{
				int n = index[i];

				if (n >= 0)
					mappers[i].Map(source, sourceObject, i, dest, destObject, n);
			}
		}

		#endregion

		#region ValueToEnum, EnumToValue

		public virtual object MapValueToEnum(object value, Type type)
		{
			if (value == null)
				return GetNullValue(type);

			MapValue[] mapValues = GetMapValues(type);

			if (mapValues != null)
			{
				var comp = (IComparable)value;

				foreach (MapValue mv in mapValues)
				foreach (object mapValue in mv.MapValues)
				{
					try
					{
						if (comp.CompareTo(mapValue) == 0)
							return mv.OrigValue;
					}
					catch (ArgumentException ex)
					{
						Debug.WriteLine(ex.Message, MethodBase.GetCurrentMethod().Name);
					}
				}
			}

			InvalidCastException exInvalidCast = null;

			try
			{
				value = ConvertChangeType(value, Enum.GetUnderlyingType(type));

				if (Enum.IsDefined(type, value))
				{
					// Regular (known) enum field w/o explicit mapping defined.
					//
					return Enum.ToObject(type, value);
				}
			}
			catch (InvalidCastException ex)
			{
				exInvalidCast = ex;
			}

			if (exInvalidCast != null)
			{
				// Rethrow an InvalidCastException when no default value specified.
				//
				throw exInvalidCast;
			}

			// At this point we have an undefined enum value.
			//
			return Enum.ToObject(type, value);
		}

		public virtual object MapEnumToValue(object value, [JetBrains.Annotations.NotNull] Type type, bool convertToUnderlyingType)
		{
			if (value == null)
				return null;

			if (type == null) throw new ArgumentNullException("type");

			type = value.GetType();

			object nullValue = GetNullValue(type);

			if (nullValue != null)
			{
				IComparable comp = (IComparable)value;

				try
				{
					if (comp.CompareTo(nullValue) == 0)
						return null;
				}
				catch
				{
				}
			}

			MapValue[] mapValues = GetMapValues(type);

			if (mapValues != null)
			{
				IComparable comp = (IComparable)value;

				foreach (MapValue mv in mapValues)
				{
					try
					{
						if (comp.CompareTo(mv.OrigValue) == 0)
							return mv.MapValues[0];
					}
					catch
					{
					}
				}
			}

			return convertToUnderlyingType ?
				System.Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()), Thread.CurrentThread.CurrentCulture) :
				value;
		}

		public virtual object MapEnumToValue(object value, bool convertToUnderlyingType)
		{
			if (value == null)
				return null;

			return MapEnumToValue(value, value.GetType(), convertToUnderlyingType);
		}

		public object MapEnumToValue(object value)
		{
			return MapEnumToValue(value, false);
		}

		public virtual object MapEnumToValue(object value, Type type)
		{
			return MapEnumToValue(value, type, false);
		}

		public T MapValueToEnum<T>(object value)
		{
			return (T)MapValueToEnum(value, typeof(T));
		}

		#endregion

		#region ConvertParameterValue

		public virtual object ConvertParameterValue(object value, Type systemType)
		{
			return value;
		}

		#endregion
	}
}
