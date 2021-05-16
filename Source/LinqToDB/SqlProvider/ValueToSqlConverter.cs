using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using Common;
	using Extensions;
	using SqlQuery;

	using ConverterType = Action<StringBuilder,SqlQuery.SqlDataType,object>;

	public class ValueToSqlConverter
	{
		public ValueToSqlConverter()
		{
			BaseConverters = Array<ValueToSqlConverter>.Empty;
		}

		public ValueToSqlConverter(params ValueToSqlConverter[]? converters)
		{
			BaseConverters = converters ?? Array<ValueToSqlConverter>.Empty;
		}

		public bool CanConvert(Type type)
		{
			type = type.ToNullableUnderlying();

			if (_converters.ContainsKey(type))
				return true;

			for (var i = 0; i < BaseConverters.Length; i++)
				if (BaseConverters[i].CanConvert(type))
					return true;

			return false;
		}

		internal void SetDefaults()
		{
			SetConverter(typeof(bool),       (sb,dt,v) => sb.Append((bool)v       ? "1" : "0"));
			SetConverter(typeof(char),       (sb,dt,v) => BuildChar(sb, (char)  v));
			SetConverter(typeof(sbyte),      (sb,dt,v) => sb.Append((sbyte)     v));
			SetConverter(typeof(byte),       (sb,dt,v) => sb.Append((byte)      v));
			SetConverter(typeof(short),      (sb,dt,v) => sb.Append((short)     v));
			SetConverter(typeof(ushort),     (sb,dt,v) => sb.Append((ushort)    v));
			SetConverter(typeof(int),        (sb,dt,v) => sb.Append((int)       v));
			SetConverter(typeof(uint),       (sb,dt,v) => sb.Append((uint)      v));
			SetConverter(typeof(long),       (sb,dt,v) => sb.Append((long)      v));
			SetConverter(typeof(ulong),      (sb,dt,v) => sb.Append((ulong)     v));
			SetConverter(typeof(float),      (sb,dt,v) => sb.Append(((float)    v). ToString("G9", _numberFormatInfo)));
			SetConverter(typeof(double),     (sb,dt,v) => sb.Append(((double)   v). ToString("G17", _numberFormatInfo)));
			SetConverter(typeof(decimal),    (sb,dt,v) => sb.Append(((decimal)v).   ToString(_numberFormatInfo)));
			SetConverter(typeof(DateTime),   (sb,dt,v) => BuildDateTime(sb, (DateTime)v));
			SetConverter(typeof(string),     (sb,dt,v) => BuildString  (sb, v.ToString()!));
			SetConverter(typeof(Guid),       (sb,dt,v) => sb.Append('\'').Append(v).Append('\''));

			SetConverter(typeof(SqlBoolean), (sb,dt,v) => sb.Append((SqlBoolean)v ? "1" : "0"));
			SetConverter(typeof(SqlByte),    (sb,dt,v) => sb.Append(((SqlByte)   v).ToString()));
			SetConverter(typeof(SqlInt16),   (sb,dt,v) => sb.Append(((SqlInt16)  v).ToString()));
			SetConverter(typeof(SqlInt32),   (sb,dt,v) => sb.Append(((SqlInt32)  v).ToString()));
			SetConverter(typeof(SqlInt64),   (sb,dt,v) => sb.Append(((SqlInt64)  v).ToString()));
			SetConverter(typeof(SqlSingle),  (sb,dt,v) => sb.Append(((SqlSingle) v).ToString()));
			SetConverter(typeof(SqlDouble),  (sb,dt,v) => sb.Append(((SqlDouble) v).ToString()));
			SetConverter(typeof(SqlDecimal), (sb,dt,v) => sb.Append(((SqlDecimal)v).ToString()));
			SetConverter(typeof(SqlMoney),   (sb,dt,v) => sb.Append(((SqlMoney)  v).ToString()));
			SetConverter(typeof(SqlDateTime),(sb,dt,v) => BuildDateTime(sb, (DateTime)(SqlDateTime)v));
			SetConverter(typeof(SqlString),  (sb,dt,v) => BuildString  (sb, v.ToString()!));
			SetConverter(typeof(SqlChars),   (sb,dt,v) => BuildString  (sb, ((SqlChars)v).ToSqlString().ToString()));
			SetConverter(typeof(SqlGuid),    (sb,dt,v) => sb.Append('\'').Append(v).Append('\''));
		}

		internal readonly ValueToSqlConverter[] BaseConverters;

		readonly Dictionary<Type,ConverterType> _converters = new ();

		ConverterType? _booleanConverter;
		ConverterType? _charConverter;
		ConverterType? _sByteConverter;
		ConverterType? _byteConverter;
		ConverterType? _int16Converter;
		ConverterType? _uInt16Converter;
		ConverterType? _int32Converter;
		ConverterType? _uInt32Converter;
		ConverterType? _int64Converter;
		ConverterType? _uInt64Converter;
		ConverterType? _singleConverter;
		ConverterType? _doubleConverter;
		ConverterType? _decimalConverter;
		ConverterType? _dateTimeConverter;
		ConverterType? _stringConverter;

		static readonly NumberFormatInfo _numberFormatInfo = new ()
		{
			CurrencyDecimalDigits    = NumberFormatInfo.InvariantInfo.CurrencyDecimalDigits,
			CurrencyDecimalSeparator = NumberFormatInfo.InvariantInfo.CurrencyDecimalSeparator,
			CurrencyGroupSeparator   = NumberFormatInfo.InvariantInfo.CurrencyGroupSeparator,
			CurrencyGroupSizes       = NumberFormatInfo.InvariantInfo.CurrencyGroupSizes,
			CurrencyNegativePattern  = NumberFormatInfo.InvariantInfo.CurrencyNegativePattern,
			CurrencyPositivePattern  = NumberFormatInfo.InvariantInfo.CurrencyPositivePattern,
			CurrencySymbol           = NumberFormatInfo.InvariantInfo.CurrencySymbol,
			NaNSymbol                = NumberFormatInfo.InvariantInfo.NaNSymbol,
			NegativeInfinitySymbol   = NumberFormatInfo.InvariantInfo.NegativeInfinitySymbol,
			NegativeSign             = NumberFormatInfo.InvariantInfo.NegativeSign,
			NumberDecimalDigits      = NumberFormatInfo.InvariantInfo.NumberDecimalDigits,
			NumberDecimalSeparator   = ".",
			NumberGroupSeparator     = NumberFormatInfo.InvariantInfo.NumberGroupSeparator,
			NumberGroupSizes         = NumberFormatInfo.InvariantInfo.NumberGroupSizes,
			NumberNegativePattern    = NumberFormatInfo.InvariantInfo.NumberNegativePattern,
			PercentDecimalDigits     = NumberFormatInfo.InvariantInfo.PercentDecimalDigits,
			PercentDecimalSeparator  = ".",
			PercentGroupSeparator    = NumberFormatInfo.InvariantInfo.PercentGroupSeparator,
			PercentGroupSizes        = NumberFormatInfo.InvariantInfo.PercentGroupSizes,
			PercentNegativePattern   = NumberFormatInfo.InvariantInfo.PercentNegativePattern,
			PercentPositivePattern   = NumberFormatInfo.InvariantInfo.PercentPositivePattern,
			PercentSymbol            = NumberFormatInfo.InvariantInfo.PercentSymbol,
			PerMilleSymbol           = NumberFormatInfo.InvariantInfo.PerMilleSymbol,
			PositiveInfinitySymbol   = NumberFormatInfo.InvariantInfo.PositiveInfinitySymbol,
			PositiveSign             = NumberFormatInfo.InvariantInfo.PositiveSign,
		};

		static void BuildString(StringBuilder stringBuilder, string value)
		{
			stringBuilder
				.Append('\'')
				.Append(value.Replace("'", "''"))
				.Append('\'');
		}

		static void BuildChar(StringBuilder stringBuilder, char value)
		{
			stringBuilder.Append('\'');

			if (value == '\'') stringBuilder.Append("''");
			else               stringBuilder.Append(value);

			stringBuilder.Append('\'');
		}

		static void BuildDateTime(StringBuilder stringBuilder, DateTime value)
		{
			var format = "'{0:yyyy-MM-dd HH:mm:ss.fff}'";

			if (value.Millisecond == 0)
			{
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
					"'{0:yyyy-MM-dd}'" :
					"'{0:yyyy-MM-dd HH:mm:ss}'";
			}

			stringBuilder.AppendFormat(format, value);
		}

		public bool TryConvert(StringBuilder stringBuilder, object? value)
		{
			if (value == null || value is INullable nullable && nullable.IsNull)
			{
				stringBuilder.Append("NULL");
				return true;
			}

			return TryConvertImpl(stringBuilder, new SqlDataType(value.GetType()), value, true);
		}

		public bool TryConvert(StringBuilder stringBuilder, SqlDataType dataType, object? value)
		{
			return TryConvertImpl(stringBuilder, dataType, value, true);
		}

		bool TryConvertImpl(StringBuilder stringBuilder, SqlDataType dataType, object? value, bool tryBase)
		{
			if (value == null || value is INullable nullable && nullable.IsNull)
			{
				stringBuilder.Append("NULL");
				return true;
			}

			var type = value.GetType();

			ConverterType? converter = null;

			if (_converters.Count > 0)
			{
				if (!_converters.TryGetValue(type, out converter))
				{
					switch (type.GetTypeCodeEx())
					{
						case TypeCode.DBNull  : stringBuilder.Append("NULL")  ; return true;
						case TypeCode.Boolean : converter = _booleanConverter ; break;
						case TypeCode.Char    : converter = _charConverter    ; break;
						case TypeCode.SByte   : converter = _sByteConverter   ; break;
						case TypeCode.Byte    : converter = _byteConverter    ; break;
						case TypeCode.Int16   : converter = _int16Converter   ; break;
						case TypeCode.UInt16  : converter = _uInt16Converter  ; break;
						case TypeCode.Int32   : converter = _int32Converter   ; break;
						case TypeCode.UInt32  : converter = _uInt32Converter  ; break;
						case TypeCode.Int64   : converter = _int64Converter   ; break;
						case TypeCode.UInt64  : converter = _uInt64Converter  ; break;
						case TypeCode.Single  : converter = _singleConverter  ; break;
						case TypeCode.Double  : converter = _doubleConverter  ; break;
						case TypeCode.Decimal : converter = _decimalConverter ; break;
						case TypeCode.DateTime: converter = _dateTimeConverter; break;
						case TypeCode.String  : converter = _stringConverter  ; break;
					}
				}
			}

			if (converter != null)
			{
				converter(stringBuilder, dataType, value);
				return true;
			}

			if (tryBase && BaseConverters.Length > 0)
				foreach (var valueConverter in BaseConverters)
					if (valueConverter.TryConvertImpl(stringBuilder, dataType, value, false))
						return true;

			return false;
		}

		public StringBuilder Convert(StringBuilder stringBuilder, object? value)
		{
			if (!TryConvert(stringBuilder, value))
				throw new LinqToDBException($"Cannot convert value of type {value?.GetType()} to SQL");

			return stringBuilder;
		}

		public StringBuilder Convert(StringBuilder stringBuilder, SqlDataType dataType, object? value)
		{
			if (!TryConvert(stringBuilder, dataType, value))
				throw new LinqToDBException($"Cannot convert value of type {value?.GetType()} to SQL");

			return stringBuilder;
		}

		public void SetConverter(Type type, ConverterType? converter)
		{
			if (converter == null)
			{
				if (_converters.ContainsKey(type))
					_converters.Remove(type);
			}
			else
			{
				_converters[type] = converter;

				if (!type.IsEnum)
				{
					switch (type.GetTypeCodeEx())
					{
						case TypeCode.Boolean : _booleanConverter  = converter; return;
						case TypeCode.Char    : _charConverter     = converter; return;
						case TypeCode.SByte   : _sByteConverter    = converter; return;
						case TypeCode.Byte    : _byteConverter     = converter; return;
						case TypeCode.Int16   : _int16Converter    = converter; return;
						case TypeCode.UInt16  : _uInt16Converter   = converter; return;
						case TypeCode.Int32   : _int32Converter    = converter; return;
						case TypeCode.UInt32  : _uInt32Converter   = converter; return;
						case TypeCode.Int64   : _int64Converter    = converter; return;
						case TypeCode.UInt64  : _uInt64Converter   = converter; return;
						case TypeCode.Single  : _singleConverter   = converter; return;
						case TypeCode.Double  : _doubleConverter   = converter; return;
						case TypeCode.Decimal : _decimalConverter  = converter; return;
						case TypeCode.DateTime: _dateTimeConverter = converter; return;
						case TypeCode.String  : _stringConverter   = converter; return;
					}
				}
			}
		}
	}
}
