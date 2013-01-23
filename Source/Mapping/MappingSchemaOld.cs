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

#region ReSharper disable
// ReSharper disable SuggestUseVarKeywordEvident
// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable RedundantTypeArgumentsOfMethod
#endregion

using KeyValue = System.Collections.Generic.KeyValuePair<System.Type,System.Type>;
using Convert  = LinqToDB.Common.ConvertOld;

namespace LinqToDB.Mapping
{
	using Extensions;
	using Properties;
	using Reflection;
	using Reflection.Extension;
	using Reflection.MetadataProvider;

	public class MappingSchemaOld
	{
		public MappingSchema NewSchema = MappingSchema.Default;

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

		protected virtual ObjectMapper CreateObjectMapper(Type type)
		{
			var attr = type.GetFirstAttribute<ObjectMapperAttribute>();
			return attr == null ? CreateObjectMapperInstance(type) : attr.ObjectMapper;
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
			//[DebuggerStepThrough] fix me
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

		#region Primitive Types

		[CLSCompliant(false)]
		public virtual SByte ConvertToSByte(object value)
		{
			return
				value is SByte ? (SByte)value :
				value == null ? (SByte)NewSchema.GetDefaultValue(typeof(SByte)) :
					Common.ConvertOld.ToSByte(value);
		}

		public virtual Int16 ConvertToInt16(object value)
		{
			return
				value is Int16? (Int16)value:
				value == null || value is DBNull? (Int16)NewSchema.GetDefaultValue(typeof(Int16)):
					Common.ConvertOld.ToInt16(value);
		}

		public virtual Int32 ConvertToInt32(object value)
		{
			return
				value is Int32? (Int32)value:
				value == null || value is DBNull? (Int32)NewSchema.GetDefaultValue(typeof(Int32)):
					Common.ConvertOld.ToInt32(value);
		}

		public virtual Int64 ConvertToInt64(object value)
		{
			return
				value is Int64? (Int64)value:
				value == null || value is DBNull? (Int64)NewSchema.GetDefaultValue(typeof(Int64)):
					Common.ConvertOld.ToInt64(value);
		}

		public virtual Byte ConvertToByte(object value)
		{
			return
				value is Byte? (Byte)value:
				value == null || value is DBNull? (Byte)NewSchema.GetDefaultValue(typeof(Byte)):
					Common.ConvertOld.ToByte(value);
		}

		[CLSCompliant(false)]
		public virtual UInt16 ConvertToUInt16(object value)
		{
			return
				value is UInt16? (UInt16)value:
				value == null || value is DBNull? (UInt16)NewSchema.GetDefaultValue(typeof(UInt16)):
					Common.ConvertOld.ToUInt16(value);
		}

		[CLSCompliant(false)]
		public virtual UInt32 ConvertToUInt32(object value)
		{
			return
				value is UInt32? (UInt32)value:
				value == null || value is DBNull? (UInt32)NewSchema.GetDefaultValue(typeof(UInt32)):
					Common.ConvertOld.ToUInt32(value);
		}

		[CLSCompliant(false)]
		public virtual UInt64 ConvertToUInt64(object value)
		{
			return
				value is UInt64? (UInt64)value:
				value == null || value is DBNull? (UInt64)NewSchema.GetDefaultValue(typeof(UInt64)):
					Common.ConvertOld.ToUInt64(value);
		}

		public virtual Char ConvertToChar(object value)
		{
			return
				value is Char? (Char)value:
				value == null || value is DBNull? (Char)NewSchema.GetDefaultValue(typeof(Char)):
					Common.ConvertOld.ToChar(value);
		}

		public virtual Single ConvertToSingle(object value)
		{
			return
				value is Single? (Single)value:
				value == null || value is DBNull? (Single)NewSchema.GetDefaultValue(typeof(Single)):
					Common.ConvertOld.ToSingle(value);
		}

		public double DefaultDoubleNullValue { get; set; }

		public virtual Double ConvertToDouble(object value)
		{
			return
				value is Double? (Double)value:
				value == null || value is DBNull? (Double)NewSchema.GetDefaultValue(typeof(Double)):
					Common.ConvertOld.ToDouble(value);
		}

		public virtual Boolean ConvertToBoolean(object value)
		{
			return
				value is Boolean? (Boolean)value:
				value == null || value is DBNull? (bool)NewSchema.GetDefaultValue(typeof(bool)):
					Common.ConvertOld.ToBoolean(value);
		}

		#endregion

		#region Simple Types

		public virtual String ConvertToString(object value)
		{
			return
				value is String? (String)value :
				value == null || value is DBNull? (string)NewSchema.GetDefaultValue(typeof(string)):
					Common.ConvertOld.ToString(value);
		}

		public virtual DateTime ConvertToDateTime(object value)
		{
			return
				value is DateTime? (DateTime)value:
				value == null || value is DBNull? (DateTime)NewSchema.GetDefaultValue(typeof(DateTime)):
					Common.ConvertOld.ToDateTime(value);
		}

		public virtual TimeSpan ConvertToTimeSpan(object value)
		{
			return ConvertToDateTime(value).TimeOfDay;
		}

		public virtual DateTimeOffset ConvertToDateTimeOffset(object value)
		{
			return
				value is DateTimeOffset? (DateTimeOffset)value:
				value == null || value is DBNull? (DateTimeOffset)NewSchema.GetDefaultValue(typeof(DateTimeOffset)):
					Common.ConvertOld.ToDateTimeOffset(value);
		}

		public virtual Binary ConvertToLinqBinary(object value)
		{
			return
				value is Binary ? (Binary)value:
				value is byte[] ? new Binary((byte[])value) : 
				value == null || value is DBNull? (Binary)NewSchema.GetDefaultValue(typeof(Binary)):
					Common.ConvertOld.ToLinqBinary(value);
		}

		public virtual Decimal ConvertToDecimal(object value)
		{
			return
				value is Decimal? (Decimal)value:
				value == null || value is DBNull? (decimal)NewSchema.GetDefaultValue(typeof(decimal)):
					Common.ConvertOld.ToDecimal(value);
		}

		public virtual Guid ConvertToGuid(object value)
		{
			return
				value is Guid? (Guid)value:
				value == null || value is DBNull? (Guid)NewSchema.GetDefaultValue(typeof(Guid)):
					Common.ConvertOld.ToGuid(value);
		}

		public virtual Stream ConvertToStream(object value)
		{
			return
				value is Stream? (Stream)value:
				value == null || value is DBNull? (Stream)NewSchema.GetDefaultValue(typeof(Stream)):
					 Common.ConvertOld.ToStream(value);
		}

#if !SILVERLIGHT

		public virtual XmlReader ConvertToXmlReader(object value)
		{
			return
				value is XmlReader? (XmlReader)value:
				value == null || value is DBNull? (XmlReader)NewSchema.GetDefaultValue(typeof(XmlReader)):
					Common.ConvertOld.ToXmlReader(value);
		}

		public virtual XmlDocument ConvertToXmlDocument(object value)
		{
			return
				value is XmlDocument? (XmlDocument)value:
				value == null || value is DBNull? (XmlDocument)NewSchema.GetDefaultValue(typeof(XmlDocument)):
					Common.ConvertOld.ToXmlDocument(value);
		}

#endif

		public virtual byte[] ConvertToByteArray(object value)
		{
			return
				value is byte[]? (byte[])value:
				value == null || value is DBNull? null:
					Common.ConvertOld.ToByteArray(value);
		}

		public virtual char[] ConvertToCharArray(object value)
		{
			return
				value is char[]? (char[])value:
				value == null || value is DBNull? null:
					Common.ConvertOld.ToCharArray(value);
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

		public virtual object ConvertChangeType(object value, Type conversionType)
		{
			return ConvertChangeType(value, conversionType, conversionType.IsNullable());
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
				if (conversionType.IsNullable())
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
