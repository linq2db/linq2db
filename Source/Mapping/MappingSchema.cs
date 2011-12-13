using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Xml;

using LinqToDB.Common;
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
			Attribute attr = TypeHelper.GetFirstAttribute(type, typeof(ObjectMapperAttribute));
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
			return ConvertChangeType(value, conversionType, TypeHelper.IsNullable(conversionType));
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
				if (TypeHelper.IsNullable(conversionType))
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

		public virtual DataReaderListMapper CreateDataReaderListMapper(IDataReader reader)
		{
			return new DataReaderListMapper(CreateDataReaderMapper(reader));
		}

		public virtual DataReaderMapper CreateDataReaderMapper(
			IDataReader          dataReader,
			NameOrIndexParameter nameOrIndex)
		{
			return new ScalarDataReaderMapper(this, dataReader, nameOrIndex);
		}

		public virtual DataReaderListMapper CreateDataReaderListMapper(
			IDataReader          reader,
			NameOrIndexParameter nameOrIndex)
		{
			return new DataReaderListMapper(CreateDataReaderMapper(reader, nameOrIndex));
		}

#if !SILVERLIGHT

		public virtual DataRowMapper CreateDataRowMapper(
			DataRow        row,
			DataRowVersion version)
		{
			return new DataRowMapper(row, version);
		}

		public virtual DataTableMapper CreateDataTableMapper(
			DataTable      dataTable,
			DataRowVersion version)
		{
			return new DataTableMapper(dataTable, CreateDataRowMapper(null, version));
		}

#endif

		public virtual DictionaryMapper CreateDictionaryMapper(IDictionary dictionary)
		{
			return new DictionaryMapper(dictionary);
		}

		public virtual DictionaryListMapper CreateDictionaryListMapper(
			IDictionary          dic,
			NameOrIndexParameter keyFieldNameOrIndex,
			ObjectMapper         objectMapper)
		{
			return new DictionaryListMapper(dic, keyFieldNameOrIndex, objectMapper);
		}
		
		public virtual DictionaryIndexListMapper CreateDictionaryListMapper(
			IDictionary  dic,
			MapIndex     index,
			ObjectMapper objectMapper)
		{
			return new DictionaryIndexListMapper(dic, index, objectMapper);
		}

		public virtual DictionaryListMapper<TK,T> CreateDictionaryListMapper<TK,T>(
			IDictionary<TK,T>     dic,
			NameOrIndexParameter keyFieldNameOrIndex,
			ObjectMapper         objectMapper)
		{
			return new DictionaryListMapper<TK,T>(dic, keyFieldNameOrIndex, objectMapper);
		}

		public virtual DictionaryIndexListMapper<T> CreateDictionaryListMapper<T>(
			IDictionary<CompoundValue,T> dic,
			MapIndex                     index,
			ObjectMapper                 objectMapper)
		{
			return new DictionaryIndexListMapper<T>(dic, index, objectMapper);
		}

		public virtual EnumeratorMapper CreateEnumeratorMapper(IEnumerator enumerator)
		{
			return new EnumeratorMapper(enumerator);
		}

		public virtual ObjectListMapper CreateObjectListMapper(IList list, ObjectMapper objectMapper)
		{
			return new ObjectListMapper(list, objectMapper);
		}

		public virtual ScalarListMapper CreateScalarListMapper(IList list, Type type)
		{
			return new ScalarListMapper(list, type);
		}

		public virtual SimpleDestinationListMapper CreateScalarDestinationListMapper(IList list, Type type)
		{
			return new SimpleDestinationListMapper(CreateScalarListMapper(list, type));
		}

		public virtual SimpleSourceListMapper CreateScalarSourceListMapper(IList list, Type type)
		{
			return new SimpleSourceListMapper(CreateScalarListMapper(list, type));
		}

		public virtual ScalarListMapper<T> CreateScalarListMapper<T>(IList<T> list)
		{
			return new ScalarListMapper<T>(this, list);
		}

		public virtual SimpleDestinationListMapper CreateScalarDestinationListMapper<T>(IList<T> list)
		{
			return new SimpleDestinationListMapper(CreateScalarListMapper<T>(list));
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

		#region GetDefaultValue

		private readonly Dictionary<Type,object> _defaultValues = new Dictionary<Type,object>();

		public virtual object GetDefaultValue([JetBrains.Annotations.NotNull] Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			lock (_defaultValues)
			{
				object defaultValue;

				if (_defaultValues.TryGetValue(type, out defaultValue))
					return defaultValue;

				var  typeExt = TypeExtension.GetTypeExtension(type, Extensions);
				bool isSet;

				defaultValue = MetadataProvider.GetDefaultValue(this, typeExt, type, out isSet);

				_defaultValues.Add(type, defaultValue = TypeExtension.ChangeType(defaultValue, type));

				return defaultValue;
			}
		}

		#endregion

		#region GetDataSource, GetDataDestination

		[CLSCompliant(false)]
		public virtual IMapDataSource GetDataSource(object obj)
		{
			if (obj == null) throw new ArgumentNullException("obj");

			if (obj is IMapDataSource)
				return (IMapDataSource)obj;

			if (obj is IDataReader)
				return CreateDataReaderMapper((IDataReader)obj);

#if !SILVERLIGHT

			if (obj is DataRow)
				return CreateDataRowMapper((DataRow)obj, DataRowVersion.Default);

			if (obj is DataRowView)
				return CreateDataRowMapper(
					((DataRowView)obj).Row,
					((DataRowView)obj).RowVersion);

			if (obj is DataTable)
				return CreateDataRowMapper(((DataTable)(obj)).Rows[0], DataRowVersion.Default);

#endif

			if (obj is IDictionary)
				return CreateDictionaryMapper((IDictionary)obj);

			return GetObjectMapper(obj.GetType());
		}

		[CLSCompliant(false)]
		public virtual IMapDataDestination GetDataDestination(object obj)
		{
			if (obj == null) throw new ArgumentNullException("obj");

			if (obj is IMapDataDestination)
				return (IMapDataDestination)obj;

#if !SILVERLIGHT

			if (obj is DataRow)
				return CreateDataRowMapper((DataRow)obj, DataRowVersion.Default);

			if (obj is DataRowView)
				return CreateDataRowMapper(
					((DataRowView)obj).Row,
					((DataRowView)obj).RowVersion);

			if (obj is DataTable)
			{
				DataTable dt = obj as DataTable;
				DataRow   dr = dt.NewRow();

				dt.Rows.Add(dr);

				return CreateDataRowMapper(dr, DataRowVersion.Default);
			}

#endif

			if (obj is IDictionary)
				return CreateDictionaryMapper((IDictionary)obj);

			return GetObjectMapper(obj.GetType());
		}

		[CLSCompliant(false)]
		public virtual IMapDataSourceList GetDataSourceList(object obj)
		{
			if (obj == null) throw new ArgumentNullException("obj");

			if (obj is IMapDataSourceList)
				return (IMapDataSourceList)obj;

			if (obj is IDataReader)
				return CreateDataReaderListMapper((IDataReader)obj);

			Type type = obj.GetType().GetElementType();

			return TypeHelper.IsScalar(type)?
				(IMapDataSourceList)CreateScalarSourceListMapper((IList)obj, type):
				CreateObjectListMapper((IList)obj, CreateObjectMapper(type));
		}

		[CLSCompliant(false)]
		public virtual IMapDataDestinationList GetDataDestinationList(object obj)
		{
			if (obj == null) throw new ArgumentNullException("obj");

			if (obj is IMapDataDestinationList)
				return (IMapDataDestinationList)obj;

			Type type = obj.GetType().GetElementType();

			return TypeHelper.IsScalar(type)?
				(IMapDataDestinationList)CreateScalarDestinationListMapper((IList)obj, type):
				CreateObjectListMapper((IList)obj, CreateObjectMapper(type));
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
		internal protected static int[] GetIndex(
			IMapDataSource      source,
			IMapDataDestination dest)
		{
			int   count = source.Count;
			int[] index = new int[count];

			for (int i = 0; i < count; i++)
				index[i] = dest.GetOrdinal(source.GetName(i));

			return index;
		}

		[CLSCompliant(false), Obsolete]
		protected static void MapInternal(
			IMapDataSource      source, object sourceObject,
			IMapDataDestination dest,   object destObject,
			int[]               index)
		{
			for (int i = 0; i < index.Length; i++)
			{
				int n = index[i];

				if (n >= 0)
					dest.SetValue(destObject, n, source.GetValue(sourceObject, i));
			}
		}

		[CLSCompliant(false)]
		internal protected static void MapInternal(
			IMapDataSource      source, object sourceObject,
			IMapDataDestination dest,   object destObject,
			int[]               index,
			IValueMapper[]      mappers)
		{
			for (int i = 0; i < index.Length; i++)
			{
				int n = index[i];

				if (n >= 0)
					mappers[i].Map(source, sourceObject, i, dest, destObject, n);
			}
		}

		[CLSCompliant(false)]
		protected virtual void MapInternal(
			InitContext         initContext,
			IMapDataSource      source, object sourceObject, 
			IMapDataDestination dest,   object destObject,
			params object[]     parameters)
		{
			ISupportMapping smSource = sourceObject as ISupportMapping;
			ISupportMapping smDest   = destObject   as ISupportMapping;

			if (smSource != null)
			{
				if (initContext == null)
				{
					initContext = new InitContext();

					initContext.MappingSchema = this;
					initContext.DataSource    = source;
					initContext.SourceObject  = sourceObject;
					initContext.ObjectMapper  = dest as ObjectMapper;
					initContext.Parameters    = parameters;
				}

				initContext.IsSource = true;
				smSource.BeginMapping(initContext);
				initContext.IsSource = false;

				if (initContext.StopMapping)
					return;
			}

			if (smDest != null)
			{
				if (initContext == null)
				{
					initContext = new InitContext();

					initContext.MappingSchema = this;
					initContext.DataSource    = source;
					initContext.SourceObject  = sourceObject;
					initContext.ObjectMapper  = dest as ObjectMapper;
					initContext.Parameters    = parameters;
				}

				smDest.BeginMapping(initContext);

				if (initContext.StopMapping)
					return;

				if (dest != initContext.ObjectMapper && initContext.ObjectMapper != null)
					dest = initContext.ObjectMapper;
			}

			int[]          index   = GetIndex       (source, dest);
			IValueMapper[] mappers = GetValueMappers(source, dest, index);

			MapInternal(source, sourceObject, dest, destObject, index, mappers);

			if (smDest != null)
				smDest.EndMapping(initContext);

			if (smSource != null)
			{
				initContext.IsSource = true;
				smSource.EndMapping(initContext);
				initContext.IsSource = false;
			}
		}

		protected virtual object MapInternal(InitContext initContext)
		{
			object dest = initContext.ObjectMapper.CreateInstance(initContext);

			if (initContext.StopMapping == false)
			{
				MapInternal(initContext,
					initContext.DataSource, initContext.SourceObject,
					initContext.ObjectMapper, dest,
					initContext.Parameters);
			}

			return dest;
		}

		[CLSCompliant(false)]
		public void MapSourceToDestination(
			IMapDataSource      source, object sourceObject, 
			IMapDataDestination dest,   object destObject,
			params object[]     parameters)
		{
			MapInternal(null, source, sourceObject, dest, destObject, parameters);
		}

		public void MapSourceToDestination(
			object          sourceObject,
			object          destObject,
			params object[] parameters)
		{
			IMapDataSource      source = GetDataSource     (sourceObject);
			IMapDataDestination dest   = GetDataDestination(destObject);

			MapInternal(null, source, sourceObject, dest, destObject, parameters);
		}

		private static readonly ObjectMapper _nullMapper = new ObjectMapper();

		private class MapInfo
		{
			public int[]          Index;
			public IValueMapper[] Mappers;
		}

		[CLSCompliant(false)]
		public virtual void MapSourceListToDestinationList(
			IMapDataSourceList      dataSourceList,
			IMapDataDestinationList dataDestinationList,
			params object[]         parameters)
		{
			if (dataSourceList      == null) throw new ArgumentNullException("dataSourceList");
			if (dataDestinationList == null) throw new ArgumentNullException("dataDestinationList");

			Dictionary<ObjectMapper,MapInfo> infos = new Dictionary<ObjectMapper,MapInfo>();

			InitContext ctx = new InitContext();

			ctx.MappingSchema = this;
			ctx.Parameters    = parameters;

			dataSourceList.     InitMapping(ctx); if (ctx.StopMapping) return;
			dataDestinationList.InitMapping(ctx); if (ctx.StopMapping) return;

			int[]               index   = null;
			IValueMapper[]      mappers = null;
			ObjectMapper        current = _nullMapper;
			IMapDataDestination dest    = dataDestinationList.GetDataDestination(ctx);
			ObjectMapper        om      = dest as ObjectMapper;

			while (dataSourceList.SetNextDataSource(ctx))
			{
				ctx.ObjectMapper = om;
				ctx.StopMapping  = false;

				object destObject = dataDestinationList.GetNextObject(ctx);

				if (ctx.StopMapping) continue;

				ISupportMapping smSource = ctx.SourceObject as ISupportMapping;
				ISupportMapping smDest   = destObject       as ISupportMapping;

				if (smSource != null)
				{
					ctx.IsSource = true;
					smSource.BeginMapping(ctx);
					ctx.IsSource = false;

					if (ctx.StopMapping)
						continue;
				}

				if (smDest != null)
				{
					smDest.BeginMapping(ctx);

					if (ctx.StopMapping)
						continue;
				}

				IMapDataDestination currentDest = current ?? dest;

				if (current != ctx.ObjectMapper)
				{
					current     = ctx.ObjectMapper;
					currentDest = current ?? dest;

					if (current != null)
					{
						MapInfo info;
						if (!infos.TryGetValue(current, out info))
						{
							info = new MapInfo();

							info.Index   = GetIndex(ctx.DataSource, currentDest);
							info.Mappers = GetValueMappers(ctx.DataSource, currentDest, info.Index);

							infos.Add(current, info);
						}

						index   = info.Index;
						mappers = info.Mappers;
					}
					else
					{
						index   = GetIndex(ctx.DataSource, currentDest);
						mappers = GetValueMappers(ctx.DataSource, currentDest, index);
					}
				}

				MapInternal(
					ctx.DataSource,
					ctx.SourceObject,
					currentDest,
					destObject,
					index,
					mappers);

				if (smDest != null)
					smDest.EndMapping(ctx);

				if (smSource != null)
				{
					ctx.IsSource = true;
					smSource.EndMapping(ctx);
					ctx.IsSource = false;
				}
			}

			dataDestinationList.EndMapping(ctx);
			dataSourceList.     EndMapping(ctx);
		}

		#endregion

		#region ValueToEnum, EnumToValue

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
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

			// Default value.
			//
			object defaultValue = GetDefaultValue(type);

			if (defaultValue != null)
				return defaultValue;

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

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
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

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
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

		#region Object

		#region MapObjectToObject

		public object MapObjectToObject(
			object          sourceObject,
			object          destObject,
			params object[] parameters)
		{
			if (sourceObject == null) throw new ArgumentNullException("sourceObject");
			if (destObject   == null) throw new ArgumentNullException("destObject");

			MapInternal(
				null,
				GetObjectMapper(sourceObject.GetType()), sourceObject,
				GetObjectMapper(destObject.  GetType()), destObject,
				parameters);

			return destObject;
		}

		public object MapObjectToObject(
			object          sourceObject,
			Type            destObjectType,
			params object[] parameters)
		{
			if (sourceObject == null) throw new ArgumentNullException("sourceObject");

			InitContext ctx = new InitContext();

			ctx.MappingSchema = this;
			ctx.DataSource    = GetObjectMapper(sourceObject.GetType());
			ctx.SourceObject  = sourceObject;
			ctx.ObjectMapper  = GetObjectMapper(destObjectType);
			ctx.Parameters    = parameters;

			return MapInternal(ctx);
		}

		public T MapObjectToObject<T>(
			object          sourceObject,
			params object[] parameters)
		{
			return (T)MapObjectToObject(sourceObject, typeof(T), parameters);
		}

		#endregion

		#region MapObjectToDataRow

#if !SILVERLIGHT

		public DataRow MapObjectToDataRow(
			object  sourceObject,
			DataRow destRow)
		{
			if (sourceObject == null) throw new ArgumentNullException("sourceObject");

			MapInternal(
				null,
				GetObjectMapper    (sourceObject.GetType()), sourceObject,
				CreateDataRowMapper(destRow, DataRowVersion.Default), destRow,
				null);

			return destRow;
		}

		public DataRow MapObjectToDataRow(
			object    sourceObject,
			DataTable destTable)
		{
			if (destTable    == null) throw new ArgumentNullException("destTable");
			if (sourceObject == null) throw new ArgumentNullException("sourceObject");

			DataRow destRow = destTable.NewRow();

			destTable.Rows.Add(destRow);

			MapInternal(
				null,
				GetObjectMapper    (sourceObject.GetType()), sourceObject,
				CreateDataRowMapper(destRow, DataRowVersion.Default), destRow,
				null);

			return destRow;
		}

#endif

		#endregion

		#region MapObjectToDictionary

		public IDictionary MapObjectToDictionary(
			object      sourceObject,
			IDictionary destDictionary)
		{
			if (sourceObject == null) throw new ArgumentNullException("sourceObject");

			MapInternal(
				null,
				GetObjectMapper       (sourceObject.GetType()), sourceObject,
				CreateDictionaryMapper(destDictionary),         destDictionary,
				null);

			return destDictionary;
		}

		public IDictionary MapObjectToDictionary(object sourceObject)
		{
			if (sourceObject == null) throw new ArgumentNullException("sourceObject");

			ObjectMapper om = GetObjectMapper(sourceObject.GetType());

			var destDictionary = new Dictionary<object,object>(om.Count);

			MapInternal(
				null,
				om, sourceObject,
				CreateDictionaryMapper(destDictionary), destDictionary,
				null);

			return destDictionary;
		}

		#endregion

		#endregion

		#region DataRow

#if !SILVERLIGHT

		#region MapDataRowToObject

		public object MapDataRowToObject(
			DataRow         dataRow,
			object          destObject,
			params object[] parameters)
		{
			if (destObject == null) throw new ArgumentNullException("destObject");

			MapInternal(
				null,
				CreateDataRowMapper(dataRow, DataRowVersion.Default), dataRow,
				GetObjectMapper(destObject.  GetType()), destObject,
				parameters);

			return destObject;
		}

		public object MapDataRowToObject(
			DataRow         dataRow,
			DataRowVersion  version,
			object          destObject,
			params object[] parameters)
		{
			if (destObject == null) throw new ArgumentNullException("destObject");

			MapInternal(
				null,
				CreateDataRowMapper(dataRow, version), dataRow,
				GetObjectMapper(destObject.  GetType()), destObject,
				parameters);

			return destObject;
		}

		public object MapDataRowToObject(
			DataRow         dataRow,
			Type            destObjectType,
			params object[] parameters)
		{
			InitContext ctx = new InitContext();

			ctx.MappingSchema = this;
			ctx.DataSource    = CreateDataRowMapper(dataRow, DataRowVersion.Default);
			ctx.SourceObject  = dataRow;
			ctx.ObjectMapper  = GetObjectMapper(destObjectType);
			ctx.Parameters    = parameters;

			return MapInternal(ctx);
		}

		public object MapDataRowToObject(
			DataRow         dataRow,
			DataRowVersion  version,
			Type            destObjectType,
			params object[] parameters)
		{
			InitContext ctx = new InitContext();

			ctx.MappingSchema = this;
			ctx.DataSource    = CreateDataRowMapper(dataRow, version);
			ctx.SourceObject  = dataRow;
			ctx.ObjectMapper  = GetObjectMapper(destObjectType);
			ctx.Parameters    = parameters;

			return MapInternal(ctx);
		}

		public T MapDataRowToObject<T>(
			DataRow         dataRow,
			params object[] parameters)
		{
			return (T)MapDataRowToObject(dataRow, typeof(T), parameters);
		}

		public T MapDataRowToObject<T>(
			DataRow         dataRow,
			DataRowVersion  version,
			params object[] parameters)
		{
			return (T)MapDataRowToObject(dataRow, version, typeof(T), parameters);
		}

		#endregion

		#region MapDataRowToDataRow

		public DataRow MapDataRowToDataRow(
			DataRow sourceRow,
			DataRow destRow)
		{
			MapInternal(
				null,
				CreateDataRowMapper(sourceRow, DataRowVersion.Default), sourceRow,
				CreateDataRowMapper(destRow,   DataRowVersion.Default), destRow,
				null);

			return destRow;
		}

		public DataRow MapDataRowToDataRow(
			DataRow        sourceRow,
			DataRowVersion version,
			DataRow        destRow)
		{
			MapInternal(
				null,
				CreateDataRowMapper(sourceRow, version), sourceRow,
				CreateDataRowMapper(destRow, DataRowVersion.Default), destRow,
				null);

			return destRow;
		}

		public DataRow MapDataRowToDataRow(
			DataRow   sourceRow,
			DataTable destTable)
		{
			if (destTable == null) throw new ArgumentNullException("destTable");

			DataRow destRow = destTable.NewRow();

			destTable.Rows.Add(destRow);

			MapInternal(
				null,
				CreateDataRowMapper(sourceRow, DataRowVersion.Default), sourceRow,
				CreateDataRowMapper(destRow,   DataRowVersion.Default), destRow,
				null);

			return destRow;
		}

		public DataRow MapDataRowToDataRow(
			DataRow        sourceRow,
			DataRowVersion version,
			DataTable      destTable)
		{
			if (destTable == null) throw new ArgumentNullException("destTable");

			DataRow destRow = destTable.NewRow();

			destTable.Rows.Add(destRow);

			MapInternal(
				null,
				CreateDataRowMapper(sourceRow, version), sourceRow,
				CreateDataRowMapper(destRow, DataRowVersion.Default), destRow,
				null);

			return destRow;
		}

		#endregion

		#region MapDataRowToDictionary

		public IDictionary MapDataRowToDictionary(
			DataRow sourceRow,
			IDictionary destDictionary)
		{
			MapInternal(
				null,
				CreateDataRowMapper   (sourceRow, DataRowVersion.Default), sourceRow,
				CreateDictionaryMapper(destDictionary),                    destDictionary,
				null);

			return destDictionary;
		}

		public Hashtable MapDataRowToDictionary(DataRow sourceRow)
		{
			if (sourceRow == null) throw new ArgumentNullException("sourceRow");

			Hashtable destDictionary = new Hashtable(sourceRow.Table.Columns.Count);

			MapInternal(
				null,
				CreateDataRowMapper   (sourceRow, DataRowVersion.Default), sourceRow,
				CreateDictionaryMapper(destDictionary),                    destDictionary,
				null);

			return destDictionary;
		}

		public IDictionary MapDataRowToDictionary(
			DataRow        sourceRow,
			DataRowVersion version,
			IDictionary    destDictionary)
		{
			MapInternal(
				null,
				CreateDataRowMapper   (sourceRow, version), sourceRow,
				CreateDictionaryMapper(destDictionary),     destDictionary,
				null);

			return destDictionary;
		}

		public Hashtable MapDataRowToDictionary(
			DataRow        sourceRow,
			DataRowVersion version)
		{
			if (sourceRow == null) throw new ArgumentNullException("sourceRow");

			Hashtable destDictionary = new Hashtable(sourceRow.Table.Columns.Count);

			MapInternal(
				null,
				CreateDataRowMapper   (sourceRow, version), sourceRow,
				CreateDictionaryMapper(destDictionary),     destDictionary,
				null);

			return destDictionary;
		}

		#endregion

#endif

		#endregion

		#region DataReader

		#region MapDataReaderToObject

		public object MapDataReaderToObject(
			IDataReader     dataReader,
			object          destObject,
			params object[] parameters)
		{
			if (destObject == null) throw new ArgumentNullException("destObject");

			MapInternal(
				null,
				CreateDataReaderMapper(dataReader), dataReader,
				GetObjectMapper(destObject. GetType()), destObject,
				parameters);

			return destObject;
		}

        //NOTE changed to virtual
		public virtual object MapDataReaderToObject(
			IDataReader     dataReader,
			Type            destObjectType,
			params object[] parameters)
		{
			InitContext ctx = new InitContext();

			ctx.MappingSchema = this;
			ctx.DataSource    = CreateDataReaderMapper(dataReader);
			ctx.SourceObject  = dataReader;
			ctx.ObjectMapper  = GetObjectMapper(destObjectType);
			ctx.Parameters    = parameters;

			return MapInternal(ctx);
		}

		public T MapDataReaderToObject<T>(
			IDataReader     dataReader,
			params object[] parameters)
		{
			return (T)MapDataReaderToObject(dataReader, typeof(T), parameters);
		}

		#endregion

		#region MapDataReaderToDataRow

#if !SILVERLIGHT

		public DataRow MapDataReaderToDataRow(IDataReader dataReader, DataRow destRow)
		{
			MapInternal(
				null,
				CreateDataReaderMapper(dataReader), dataReader,
				CreateDataRowMapper(destRow, DataRowVersion.Default), destRow,
				null);

			return destRow;
		}

		public DataRow MapDataReaderToDataRow(
			IDataReader dataReader,
			DataTable   destTable)
		{
			if (destTable == null) throw new ArgumentNullException("destTable");

			DataRow destRow = destTable.NewRow();

			destTable.Rows.Add(destRow);

			MapInternal(
				null,
				CreateDataReaderMapper(dataReader), dataReader,
				CreateDataRowMapper(destRow, DataRowVersion.Default), destRow,
				null);

			return destRow;
		}

#endif

		#endregion

		#region MapDataReaderToDictionary

		public IDictionary MapDataReaderToDictionary(
			IDataReader dataReader,
			IDictionary destDictionary)
		{
			MapInternal(
				null,
				CreateDataReaderMapper(dataReader),     dataReader,
				CreateDictionaryMapper(destDictionary), destDictionary,
				null);

			return destDictionary;
		}

		public IDictionary MapDataReaderToDictionary(IDataReader dataReader)
		{
			if (dataReader == null) throw new ArgumentNullException("dataReader");

			var destDictionary = new Dictionary<object,object>(dataReader.FieldCount);

			MapInternal(
				null,
				CreateDataReaderMapper(dataReader),     dataReader,
				CreateDictionaryMapper(destDictionary), destDictionary,
				null);

			return destDictionary;
		}

		#endregion

		#endregion

		#region Dictionary

		#region MapDictionaryToObject

		public object MapDictionaryToObject(
			IDictionary     sourceDictionary,
			object          destObject,
			params object[] parameters)
		{
			if (destObject == null) throw new ArgumentNullException("destObject");

			MapInternal(
				null,
				CreateDictionaryMapper(sourceDictionary),       sourceDictionary,
				GetObjectMapper       (destObject.  GetType()), destObject,
				parameters);

			return destObject;
		}

		public object MapDictionaryToObject(
			IDictionary     sourceDictionary,
			Type            destObjectType,
			params object[] parameters)
		{
			InitContext ctx = new InitContext();

			ctx.MappingSchema = this;
			ctx.DataSource    = CreateDictionaryMapper(sourceDictionary);
			ctx.SourceObject  = sourceDictionary;
			ctx.ObjectMapper  = GetObjectMapper(destObjectType);
			ctx.Parameters    = parameters;

			return MapInternal(ctx);
		}

		public T MapDictionaryToObject<T>(IDictionary sourceDictionary, params object[] parameters)
		{
			return (T)MapDictionaryToObject(sourceDictionary, typeof(T), parameters);
		}

		#endregion

		#region MapDictionaryToDataRow

#if !SILVERLIGHT

		public DataRow MapDictionaryToDataRow(
			IDictionary sourceDictionary,
			DataRow     destRow)
		{
			MapInternal(
				null,
				CreateDictionaryMapper(sourceDictionary),                sourceDictionary,
				CreateDataRowMapper   (destRow, DataRowVersion.Default), destRow,
				null);

			return destRow;
		}

		public DataRow MapDictionaryToDataRow(
			IDictionary sourceDictionary,
			DataTable   destTable)
		{
			if (destTable == null) throw new ArgumentNullException("destTable");

			DataRow destRow = destTable.NewRow();

			destTable.Rows.Add(destRow);

			MapInternal(
				null,
				CreateDictionaryMapper(sourceDictionary),                sourceDictionary,
				CreateDataRowMapper   (destRow, DataRowVersion.Default), destRow,
				null);

			return destRow;
		}

#endif

		#endregion

		#endregion

		#region List

		#region MapListToList

		public IList MapListToList(
			ICollection     sourceList,
			IList           destList,
			Type            destObjectType,
			params object[] parameters)
		{
			if (sourceList == null) throw new ArgumentNullException("sourceList");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceList.GetEnumerator()),
				CreateObjectListMapper(destList, GetObjectMapper(destObjectType)),
				parameters);

			return destList;
		}

		public IList MapListToList(
			ICollection     sourceList,
			Type            destObjectType,
			params object[] parameters)
		{
			if (sourceList == null) throw new ArgumentNullException("sourceList");

			var destList = new List<object>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceList.GetEnumerator()),
				CreateObjectListMapper(destList, GetObjectMapper(destObjectType)),
				parameters);

			return destList;
		}

		public List<T> MapListToList<T>(
			ICollection     sourceList,
			List<T>         destList,
			params object[] parameters)
		{
			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceList.GetEnumerator()),
				CreateObjectListMapper(destList, GetObjectMapper(typeof(T))),
				parameters);

			return destList;
		}

		public List<T> MapListToList<T>(
			ICollection     sourceList,
			params object[] parameters)
		{
			List<T> destList = new List<T>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceList.GetEnumerator()),
				CreateObjectListMapper(destList, GetObjectMapper(typeof(T))),
				parameters);

			return destList;
		}

		#endregion

		#region MapListToDataTable

#if !SILVERLIGHT

		public DataTable MapListToDataTable(
			ICollection sourceList,
			DataTable   destTable)
		{
			if (sourceList == null) throw new ArgumentNullException("sourceList");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceList.GetEnumerator()),
				CreateDataTableMapper (destTable, DataRowVersion.Default),
				null);

			return destTable;
		}

		[SuppressMessage("Microsoft.Globalization", "CA1306:SetLocaleForDataTypes")]
		public DataTable MapListToDataTable(ICollection sourceList)
		{
			if (sourceList == null) throw new ArgumentNullException("sourceList");

			DataTable destTable = new DataTable();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceList.GetEnumerator()),
				CreateDataTableMapper (destTable, DataRowVersion.Default),
				null);

			return destTable;
		}

#endif

		#endregion

		#region MapListToDictionary

		public IDictionary MapListToDictionary(
			ICollection          sourceList,
			IDictionary          destDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			Type                 destObjectType,
			params object[]      parameters)
		{
			if (sourceList == null) throw new ArgumentNullException("sourceList");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper    (sourceList.GetEnumerator()),
				CreateDictionaryListMapper(destDictionary, keyFieldNameOrIndex, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary MapListToDictionary(
			ICollection          sourceList,
			NameOrIndexParameter keyFieldNameOrIndex,
			Type                 destObjectType,
			params object[]      parameters)
		{
			if (sourceList == null) throw new ArgumentNullException("sourceList");

			IDictionary destDictionary = new Dictionary<object,object>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper    (sourceList.GetEnumerator()),
				CreateDictionaryListMapper(destDictionary, keyFieldNameOrIndex, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary<TK,T> MapListToDictionary<TK,T>(
			ICollection          sourceList,
			IDictionary<TK,T>     destDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			params object[]      parameters)
		{
			MapSourceListToDestinationList(
				CreateEnumeratorMapper         (sourceList.GetEnumerator()),
				CreateDictionaryListMapper<TK,T>(destDictionary, keyFieldNameOrIndex, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		public Dictionary<TK,T> MapListToDictionary<TK,T>(
			ICollection          sourceList,
			NameOrIndexParameter keyFieldNameOrIndex,
			params object[]      parameters)
		{
			Dictionary<TK,T> destDictionary = new Dictionary<TK,T>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper          (sourceList.GetEnumerator()),
				CreateDictionaryListMapper<TK,T>(destDictionary, keyFieldNameOrIndex, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		#endregion

		#region MapListToDictionaryIndex

		public IDictionary MapListToDictionary(
			ICollection     sourceList,
			IDictionary     destDictionary,
			MapIndex        index,
			Type            destObjectType,
			params object[] parameters)
		{
			if (sourceList == null) throw new ArgumentNullException("sourceList");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper    (sourceList.GetEnumerator()),
				CreateDictionaryListMapper(destDictionary, index, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary MapListToDictionary(
			ICollection     sourceList,
			MapIndex        index,
			Type            destObjectType,
			params object[] parameters)
		{
			if (sourceList == null) throw new ArgumentNullException("sourceList");

			IDictionary destDictionary = new Dictionary<object,object>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper    (sourceList.GetEnumerator()),
				CreateDictionaryListMapper(destDictionary, index, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary<CompoundValue,T> MapListToDictionary<T>(
			ICollection                  sourceList,
			IDictionary<CompoundValue,T> destDictionary,
			MapIndex                     index,
			params object[]              parameters)
		{
			MapSourceListToDestinationList(
				CreateEnumeratorMapper       (sourceList.GetEnumerator()),
				CreateDictionaryListMapper<T>(destDictionary, index, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		public Dictionary<CompoundValue,T> MapListToDictionary<T>(
			ICollection     sourceList,
			MapIndex        index,
			params object[] parameters)
		{
			Dictionary<CompoundValue, T> destDictionary = new Dictionary<CompoundValue,T>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper       (sourceList.GetEnumerator()),
				CreateDictionaryListMapper<T>(destDictionary, index, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		#endregion

		#endregion

		#region Table

#if !SILVERLIGHT


		#region MapDataTableToDataTable

		public DataTable MapDataTableToDataTable(
			DataTable sourceTable,
			DataTable destTable)
		{
			MapSourceListToDestinationList(
				CreateDataTableMapper(sourceTable, DataRowVersion.Default),
				CreateDataTableMapper(destTable,   DataRowVersion.Default),
				null);

			return destTable;
		}

		public DataTable MapDataTableToDataTable(
			DataTable      sourceTable,
			DataRowVersion version,
			DataTable      destTable)
		{
			MapSourceListToDestinationList(
				CreateDataTableMapper(sourceTable, version),
				CreateDataTableMapper(destTable,   DataRowVersion.Default),
				null);

			return destTable;
		}

		public DataTable MapDataTableToDataTable(DataTable sourceTable)
		{
			if (sourceTable == null) throw new ArgumentNullException("sourceTable");

			DataTable destTable = sourceTable.Clone();

			MapSourceListToDestinationList(
				CreateDataTableMapper(sourceTable, DataRowVersion.Default),
				CreateDataTableMapper(destTable,   DataRowVersion.Default),
				null);

			return destTable;
		}

		public DataTable MapDataTableToDataTable(
			DataTable      sourceTable,
			DataRowVersion version)
		{
			if (sourceTable == null) throw new ArgumentNullException("sourceTable");

			DataTable destTable = sourceTable.Clone();

			MapSourceListToDestinationList(
				CreateDataTableMapper(sourceTable, version),
				CreateDataTableMapper(destTable,   DataRowVersion.Default),
				null);

			return destTable;
		}

		#endregion

		#region MapDataTableToList

		public IList MapDataTableToList(
			DataTable       sourceTable,
			IList           list,
			Type            destObjectType,
			params object[] parameters)
		{
			MapSourceListToDestinationList(
				CreateDataTableMapper (sourceTable, DataRowVersion.Default),
				CreateObjectListMapper(list, GetObjectMapper(destObjectType)),
				parameters);

			return list;
		}

		public IList MapDataTableToList(
			DataTable       sourceTable,
			DataRowVersion  version,
			IList           list,
			Type            destObjectType,
			params object[] parameters)
		{
			MapSourceListToDestinationList(
				CreateDataTableMapper (sourceTable, version),
				CreateObjectListMapper(list, GetObjectMapper(destObjectType)),
				parameters);

			return list;
		}

		public ArrayList MapDataTableToList(
			DataTable       sourceTable,
			Type            destObjectType,
			params object[] parameters)
		{
			ArrayList list = new ArrayList();

			MapSourceListToDestinationList(
				CreateDataTableMapper (sourceTable, DataRowVersion.Default),
				CreateObjectListMapper(list, GetObjectMapper(destObjectType)),
				parameters);

			return list;
		}

		public ArrayList MapDataTableToList(
			DataTable       sourceTable,
			DataRowVersion  version,
			Type            destObjectType,
			params object[] parameters)
		{
			ArrayList list = new ArrayList();

			MapSourceListToDestinationList(
				CreateDataTableMapper (sourceTable, version),
				CreateObjectListMapper(list, GetObjectMapper(destObjectType)),
				parameters);

			return list;
		}

		public List<T> MapDataTableToList<T>(
			DataTable       sourceTable,
			List<T>         list,
			params object[] parameters)
		{
			MapSourceListToDestinationList(
				CreateDataTableMapper (sourceTable, DataRowVersion.Default),
				CreateObjectListMapper(list, GetObjectMapper(typeof(T))),
				parameters);

			return list;
		}

		public List<T> MapDataTableToList<T>(
			DataTable       sourceTable,
			DataRowVersion  version,
			List<T>         list,
			params object[] parameters)
		{
			MapSourceListToDestinationList(
				CreateDataTableMapper (sourceTable, version),
				CreateObjectListMapper(list, GetObjectMapper(typeof(T))),
				parameters);

			return list;
		}

		public List<T> MapDataTableToList<T>(
			DataTable       sourceTable,
			params object[] parameters)
		{
			List<T> list = new List<T>();

			MapSourceListToDestinationList(
				CreateDataTableMapper (sourceTable, DataRowVersion.Default),
				CreateObjectListMapper(list, GetObjectMapper(typeof(T))),
				parameters);

			return list;
		}

		public List<T> MapDataTableToList<T>(
			DataTable       sourceTable,
			DataRowVersion  version,
			params object[] parameters)
		{
			List<T> list = new List<T>();

			MapSourceListToDestinationList(
				CreateDataTableMapper (sourceTable, version),
				CreateObjectListMapper(list, GetObjectMapper(typeof(T))),
				parameters);

			return list;
		}

		#endregion

		#region MapDataTableToDictionary

		public IDictionary MapDataTableToDictionary(
			DataTable            sourceTable,
			IDictionary          destDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			Type                 destObjectType,
			params object[]      parameters)
		{
			MapSourceListToDestinationList(
				CreateDataTableMapper     (sourceTable,    DataRowVersion.Default),
				CreateDictionaryListMapper(destDictionary, keyFieldNameOrIndex, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public Hashtable MapDataTableToDictionary(
			DataTable            sourceTable,
			NameOrIndexParameter keyFieldNameOrIndex,
			Type                 destObjectType,
			params object[]      parameters)
		{
			Hashtable destDictionary = new Hashtable();

			MapSourceListToDestinationList(
				CreateDataTableMapper     (sourceTable,    DataRowVersion.Default),
				CreateDictionaryListMapper(destDictionary, keyFieldNameOrIndex, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary<TK,T> MapDataTableToDictionary<TK,T>(
			DataTable            sourceTable,
			IDictionary<TK,T>     destDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			params object[]      parameters)
		{
			MapSourceListToDestinationList(
				CreateDataTableMapper          (sourceTable,    DataRowVersion.Default),
				CreateDictionaryListMapper<TK,T>(destDictionary, keyFieldNameOrIndex, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		public Dictionary<TK,T> MapDataTableToDictionary<TK,T>(
			DataTable            sourceTable,
			NameOrIndexParameter keyFieldNameOrIndex,
			params object[]      parameters)
		{
			Dictionary<TK,T> destDictionary = new Dictionary<TK,T>();

			MapSourceListToDestinationList(
				CreateDataTableMapper          (sourceTable,    DataRowVersion.Default),
				CreateDictionaryListMapper<TK,T>(destDictionary, keyFieldNameOrIndex, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		#endregion

		#region MapDataTableToDictionary (Index)

		public IDictionary MapDataTableToDictionary(
			DataTable       sourceTable,
			IDictionary     destDictionary,
			MapIndex        index,
			Type            destObjectType,
			params object[] parameters)
		{
			MapSourceListToDestinationList(
				CreateDataTableMapper     (sourceTable,    DataRowVersion.Default),
				CreateDictionaryListMapper(destDictionary, index, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public Hashtable MapDataTableToDictionary(
			DataTable       sourceTable,
			MapIndex        index,
			Type            destObjectType,
			params object[] parameters)
		{
			Hashtable destDictionary = new Hashtable();

			MapSourceListToDestinationList(
				CreateDataTableMapper     (sourceTable,    DataRowVersion.Default),
				CreateDictionaryListMapper(destDictionary, index, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary<CompoundValue,T> MapDataTableToDictionary<T>(
			DataTable                    sourceTable,
			IDictionary<CompoundValue,T> destDictionary,
			MapIndex                     index,
			params object[]              parameters)
		{
			MapSourceListToDestinationList(
				CreateDataTableMapper        (sourceTable,    DataRowVersion.Default),
				CreateDictionaryListMapper<T>(destDictionary, index, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		public Dictionary<CompoundValue,T> MapDataTableToDictionary<T>(
			DataTable       sourceTable,
			MapIndex        index,
			params object[] parameters)
		{
			Dictionary<CompoundValue,T> destDictionary = new Dictionary<CompoundValue,T>();

			MapSourceListToDestinationList(
				CreateDataTableMapper        (sourceTable,    DataRowVersion.Default),
				CreateDictionaryListMapper<T>(destDictionary, index, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		#endregion

#endif

		#endregion

		#region DataReader

		#region MapDataReaderToList

		public IList MapDataReaderToList(
			IDataReader     reader,
			IList           list,
			Type            destObjectType,
			params object[] parameters)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateObjectListMapper    (list, GetObjectMapper(destObjectType)),
				parameters);

			return list;
		}

		public IList MapDataReaderToList(
			IDataReader     reader,
			Type            destObjectType,
			params object[] parameters)
		{
			IList list = new List<object>();

			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateObjectListMapper    (list, GetObjectMapper(destObjectType)),
				parameters);

			return list;
		}

        //NOTE changed to virtual
		public virtual IList<T> MapDataReaderToList<T>(
			IDataReader     reader,
			IList<T>        list,
			params object[] parameters)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateObjectListMapper    ((IList)list, GetObjectMapper(typeof(T))),
				parameters);

			return list;
		}

		public List<T> MapDataReaderToList<T>(
			IDataReader     reader,
			params object[] parameters)
		{
			List<T> list = new List<T>();

			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateObjectListMapper    (list, GetObjectMapper(typeof(T))),
				parameters);

			return list;
		}

		#endregion

		#region MapDataReaderToScalarList

		public IList MapDataReaderToScalarList(
			IDataReader          reader,
			NameOrIndexParameter nameOrIndex,
			IList                list,
			Type                 type)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader, nameOrIndex),
				CreateScalarDestinationListMapper(list,   type),
				null);

			return list;
		}

		public IList MapDataReaderToScalarList(
			IDataReader          reader,
			NameOrIndexParameter nameOrIndex,
			Type                 type)
		{
			IList list = new List<object>();

			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader, nameOrIndex),
				CreateScalarDestinationListMapper(list,   type),
				null);

			return list;
		}

		public IList<T> MapDataReaderToScalarList<T>(
			IDataReader          reader,
			NameOrIndexParameter nameOrIndex,
			IList<T>             list)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader, nameOrIndex),
				CreateScalarDestinationListMapper(list),
				null);

			return list;
		}

		public List<T> MapDataReaderToScalarList<T>(
			IDataReader          reader,
			NameOrIndexParameter nameOrIndex)
		{
			List<T> list = new List<T>();

			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader, nameOrIndex),
				CreateScalarDestinationListMapper(list),
				null);

			return list;
		}

		#endregion

		#region MapDataReaderToDataTable

#if !SILVERLIGHT

		public DataTable MapDataReaderToDataTable(
			IDataReader reader,
			DataTable   destTable)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateDataTableMapper     (destTable, DataRowVersion.Default),
				null);

			return destTable;
		}

		[SuppressMessage("Microsoft.Globalization", "CA1306:SetLocaleForDataTypes")]
		public DataTable MapDataReaderToDataTable(IDataReader reader)
		{
			DataTable destTable = new DataTable();

			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateDataTableMapper     (destTable, DataRowVersion.Default),
				null);

			return destTable;
		}

#endif

		#endregion

		#region MapDataReaderToDictionary

		public IDictionary MapDataReaderToDictionary(
			IDataReader          reader,
			IDictionary          destDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			Type                 destObjectType,
			params object[]      parameters)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateDictionaryListMapper(destDictionary, keyFieldNameOrIndex, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary MapDataReaderToDictionary(
			IDataReader          reader,
			NameOrIndexParameter keyFieldNameOrIndex,
			Type                 destObjectType,
			params object[]      parameters)
		{
			IDictionary dest = new Dictionary<object,object>();

			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateDictionaryListMapper(dest, keyFieldNameOrIndex, GetObjectMapper(destObjectType)),
				parameters);

			return dest;
		}

		public IDictionary<TK,T> MapDataReaderToDictionary<TK,T>(
			IDataReader          reader,
			IDictionary<TK,T>     destDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			Type                 destObjectType,
			params object[]      parameters)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper     (reader),
				CreateDictionaryListMapper<TK,T>(destDictionary, keyFieldNameOrIndex, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary<TK,T> MapDataReaderToDictionary<TK,T>(
			IDataReader          reader,
			IDictionary<TK,T>     destDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			params object[]      parameters)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper     (reader),
				CreateDictionaryListMapper<TK,T>(destDictionary, keyFieldNameOrIndex, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		public Dictionary<TK,T> MapDataReaderToDictionary<TK,T>(
			IDataReader          reader,
			NameOrIndexParameter keyFieldNameOrIndex,
			params object[]      parameters)
		{
			Dictionary<TK,T> dest = new Dictionary<TK,T>();

			MapSourceListToDestinationList(
				CreateDataReaderListMapper     (reader),
				CreateDictionaryListMapper<TK,T>(dest, keyFieldNameOrIndex, GetObjectMapper(typeof(T))),
				parameters);

			return dest;
		}

		#endregion

		#region MapDataReaderToDictionary (Index)

		public IDictionary MapDataReaderToDictionary(
			IDataReader     reader,
			IDictionary     destDictionary,
			MapIndex        index,
			Type            destObjectType,
			params object[] parameters)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateDictionaryListMapper(destDictionary, index, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary MapDataReaderToDictionary(
			IDataReader     reader,
			MapIndex        index,
			Type            destObjectType,
			params object[] parameters)
		{
			IDictionary destDictionary = new Dictionary<object,object>();

			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateDictionaryListMapper(destDictionary, index, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary<CompoundValue,T> MapDataReaderToDictionary<T>(
			IDataReader                  reader,
			IDictionary<CompoundValue,T> destDictionary,
			MapIndex                     index,
			Type                         destObjectType,
			params object[]              parameters)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateDictionaryListMapper(destDictionary, index, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary<CompoundValue,T> MapDataReaderToDictionary<T>(
			IDataReader                  reader,
			IDictionary<CompoundValue,T> destDictionary,
			MapIndex                     index,
			params object[]              parameters)
		{
			MapSourceListToDestinationList(
				CreateDataReaderListMapper(reader),
				CreateDictionaryListMapper(destDictionary, index, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		public Dictionary<CompoundValue,T> MapDataReaderToDictionary<T>(
			IDataReader     reader,
			MapIndex        index,
			params object[] parameters)
		{
			Dictionary<CompoundValue,T> destDictionary = new Dictionary<CompoundValue,T>();

			MapSourceListToDestinationList(
				CreateDataReaderListMapper   (reader),
				CreateDictionaryListMapper<T>(destDictionary, index, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		#endregion

		#endregion

		#region Dictionary

		#region MapDictionaryToList

		public IList MapDictionaryToList(
			IDictionary     sourceDictionary,
			IList           destList,
			Type            destObjectType,
			params object[] parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceDictionary.Values.GetEnumerator()),
				CreateObjectListMapper(destList, GetObjectMapper(destObjectType)),
				parameters);

			return destList;
		}

		public IList MapDictionaryToList(
			IDictionary     sourceDictionary,
			Type            destObjectType,
			params object[] parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			IList destList = new List<object>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceDictionary.Values.GetEnumerator()),
				CreateObjectListMapper(destList, GetObjectMapper(destObjectType)),
				parameters);

			return destList;
		}

		public List<T> MapDictionaryToList<T>(
			IDictionary     sourceDictionary,
			List<T>         destList,
			params object[] parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceDictionary.Values.GetEnumerator()),
				CreateObjectListMapper(destList, GetObjectMapper(typeof(T))),
				parameters);

			return destList;
		}

		public List<T> MapDictionaryToList<T>(
			IDictionary     sourceDictionary,
			params object[] parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			List<T> destList = new List<T>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceDictionary.Values.GetEnumerator()),
				CreateObjectListMapper(destList, GetObjectMapper(typeof(T))),
				parameters);

			return destList;
		}

		#endregion

		#region MapDictionaryToDataTable

#if !SILVERLIGHT

		public DataTable MapDictionaryToDataTable(
			IDictionary sourceDictionary,
			DataTable   destTable)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceDictionary.Values.GetEnumerator()),
				CreateDataTableMapper (destTable, DataRowVersion.Default),
				null);

			return destTable;
		}

		[SuppressMessage("Microsoft.Globalization", "CA1306:SetLocaleForDataTypes")]
		public DataTable MapDictionaryToDataTable(IDictionary sourceDictionary)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			DataTable destTable = new DataTable();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper(sourceDictionary.Values.GetEnumerator()),
				CreateDataTableMapper (destTable, DataRowVersion.Default),
				null);

			return destTable;
		}

#endif

		#endregion

		#region MapDictionaryToDictionary

		public IDictionary MapDictionaryToDictionary(
			IDictionary          sourceDictionary,
			IDictionary          destDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			Type                 destObjectType,
			params object[]      parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper    (sourceDictionary.Values.GetEnumerator()),
				CreateDictionaryListMapper(destDictionary, keyFieldNameOrIndex, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary MapDictionaryToDictionary(
			IDictionary          sourceDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			Type                 destObjectType,
			params object[]      parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			IDictionary dest = new Dictionary<object,object>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper    (sourceDictionary.Values.GetEnumerator()),
				CreateDictionaryListMapper(dest, keyFieldNameOrIndex, GetObjectMapper(destObjectType)),
				parameters);

			return dest;
		}

		public IDictionary<TK,T> MapDictionaryToDictionary<TK,T>(
			IDictionary          sourceDictionary,
			IDictionary<TK,T>     destDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			params object[]      parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper         (sourceDictionary.Values.GetEnumerator()),
				CreateDictionaryListMapper<TK,T>(destDictionary, keyFieldNameOrIndex, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		public Dictionary<TK,T> MapDictionaryToDictionary<TK,T>(
			IDictionary          sourceDictionary,
			NameOrIndexParameter keyFieldNameOrIndex,
			params object[]      parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			Dictionary<TK,T> dest = new Dictionary<TK,T>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper         (sourceDictionary.Values.GetEnumerator()),
				CreateDictionaryListMapper<TK,T>(dest, keyFieldNameOrIndex, GetObjectMapper(typeof(T))),
				parameters);

			return dest;
		}

		#endregion

		#region MapDictionaryToDictionary (Index)

		public IDictionary MapDictionaryToDictionary(
			IDictionary     sourceDictionary,
			IDictionary     destDictionary,
			MapIndex        index,
			Type            destObjectType,
			params object[] parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper    (sourceDictionary.Values.GetEnumerator()),
				CreateDictionaryListMapper(destDictionary, index, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary MapDictionaryToDictionary(
			IDictionary     sourceDictionary,
			MapIndex        index,
			Type            destObjectType,
			params object[] parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			IDictionary destDictionary = new Dictionary<object,object>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper    (sourceDictionary.Values.GetEnumerator()),
				CreateDictionaryListMapper(destDictionary, index, GetObjectMapper(destObjectType)),
				parameters);

			return destDictionary;
		}

		public IDictionary<CompoundValue,T> MapDictionaryToDictionary<T>(
			IDictionary                  sourceDictionary,
			IDictionary<CompoundValue,T> destDictionary,
			MapIndex                     index,
			params object[]              parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			MapSourceListToDestinationList(
				CreateEnumeratorMapper       (sourceDictionary.Values.GetEnumerator()),
				CreateDictionaryListMapper<T>(destDictionary, index, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		public Dictionary<CompoundValue,T> MapDictionaryToDictionary<T>(
			IDictionary     sourceDictionary,
			MapIndex        index,
			params object[] parameters)
		{
			if (sourceDictionary == null) throw new ArgumentNullException("sourceDictionary");

			Dictionary<CompoundValue,T> destDictionary = new Dictionary<CompoundValue,T>();

			MapSourceListToDestinationList(
				CreateEnumeratorMapper       (sourceDictionary.Values.GetEnumerator()),
				CreateDictionaryListMapper<T>(destDictionary, index, GetObjectMapper(typeof(T))),
				parameters);

			return destDictionary;
		}

		#endregion

		#endregion

		#region MapToResultSet

		public void MapResultSets(MapResultSet[] resultSets)
		{
			var initTable = new Dictionary<object,object>();
			var context   = new InitContext();

			object lastContainer = null;

			context.MappingSchema = this;

			try
			{
				PrepareRelarions(resultSets);

				// Map relations.
				//
				foreach (MapResultSet rs in resultSets)
				{
					if (rs.Relations == null)
						continue;

					ObjectMapper masterMapper = GetObjectMapper(rs.ObjectType);

					foreach (MapRelation r in rs.Relations)
					{
						MemberAccessor ma = masterMapper.TypeAccessor[r.ContainerName];

						if (ma == null)
							throw new MappingException(string.Format(Resources.MapIndex_BadField,
								masterMapper.TypeAccessor.OriginalType.Name, r.ContainerName));

						// Map.
						//
						var slave        = r.SlaveResultSet;
						var slaveMapper  = GetObjectMapper(r.SlaveResultSet.ObjectType);
						var indexedLists = rs.GetIndex(this, r.MasterIndex);

						foreach (object o in slave.List)
						{
							object key = r.SlaveIndex.GetValueOrIndex(slaveMapper, o);

							if (IsNull(key))
								continue;

							IList masterList;

							if (!indexedLists.TryGetValue(key, out masterList))
								continue;

							foreach (object master in masterList)
							{
								ISupportMapping msm = master as ISupportMapping;

								if (msm != null)
								{
									if (initTable.ContainsKey(master) == false)
									{
										msm.BeginMapping(context);
										initTable.Add(master, msm);
									}
								}

								object container = ma.GetValue(master);

								if (container is IList)
								{
									if (lastContainer != container)
									{
										lastContainer = container;

										ISupportMapping sm = container as ISupportMapping;

										if (sm != null)
										{
											if (initTable.ContainsKey(container) == false)
											{
												sm.BeginMapping(context);
												initTable[container] = sm;
											}
										}
									}

									((IList)container).Add(o);
								}
								else
								{
									ma.SetValue(master, o);
								}
							}
						}
					}
				}
			}
			finally
			{
				foreach (ISupportMapping si in initTable.Values)
					si.EndMapping(context);
			}
		}

		public void MapDataReaderToResultSet(
			IDataReader    reader,
			MapResultSet[] resultSets)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			foreach (MapResultSet rs in resultSets)
			{
				MapDataReaderToList(reader, rs.List, rs.ObjectType, rs.Parameters);

				if (reader.NextResult() == false)
					break;
			}

			MapResultSets(resultSets);
		}

#if !SILVERLIGHT

		public void MapDataSetToResultSet(
			DataSet        dataSet,
			MapResultSet[] resultSets)
		{
			for (int i = 0; i < resultSets.Length && i < dataSet.Tables.Count; i++)
			{
				MapResultSet rs = resultSets[i];

				MapDataTableToList(dataSet.Tables[i], rs.List, rs.ObjectType, rs.Parameters);
			}

			MapResultSets(resultSets);
		}

#endif

		public MapResultSet[] Clone(MapResultSet[] resultSets)
		{
			MapResultSet[] output = new MapResultSet[resultSets.Length];

			for (int i = 0; i < resultSets.Length; i++)
				output[i] = new MapResultSet(resultSets[i]);

			return output;
		}

		private static int GetResultCount(MapNextResult[] nextResults)
		{
			int n = nextResults.Length;

			foreach (MapNextResult nr in nextResults)
				n += GetResultCount(nr.NextResults);

			return n;
		}

		private static int GetResultSets(
			int             current,
			MapResultSet[]  output,
			MapResultSet    master,
			MapNextResult[] nextResults)
		{
			foreach (MapNextResult nr in nextResults)
			{
				output[current] = new MapResultSet(nr.ObjectType);

				master.AddRelation(output[current], nr.SlaveIndex, nr.MasterIndex, nr.ContainerName);

				current += GetResultSets(current + 1, output, output[current], nr.NextResults);
			}

			return current;
		}

		public MapResultSet[] ConvertToResultSet(
			Type                   masterType,
			params MapNextResult[] nextResults)
		{
			MapResultSet[] output = new MapResultSet[1 + GetResultCount(nextResults)];

			output[0] = new MapResultSet(masterType);

			GetResultSets(1, output, output[0], nextResults);

			return output;
		}

		private void PrepareRelarions(params MapResultSet[] sets)
		{
			foreach (MapResultSet masterSet in sets)
			{
				if (masterSet.Relations != null)
					continue;

				foreach (MapResultSet slaveSet in sets)
				{
					bool isSet;

					List<MapRelationBase> relations
						= MetadataProvider.GetRelations(this, Extensions, masterSet.ObjectType, slaveSet.ObjectType, out isSet);

					if (!isSet)
						continue;

					foreach (MapRelationBase relation in relations)
						masterSet.AddRelation(slaveSet, relation);
				}
			}
		}

		#endregion

		#region GetObjectMapper

		public Func<TSource,TDest> GetObjectMapper<TSource,TDest>()
		{
			return new ExpressionMapper<TSource,TDest>(this).GetMapper();
		}

		public Func<TSource,TDest> GetObjectMapper<TSource,TDest>(bool deepCopy)
		{
			return new ExpressionMapper<TSource,TDest>(this) { DeepCopy = deepCopy }.GetMapper();
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
