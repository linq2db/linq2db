using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using Common;
	using Extensions;

	class ValueToSqlConverter
	{
		public ValueToSqlConverter(params ValueToSqlConverter[] converters)
		{
			_baseConverters = converters ?? Array<ValueToSqlConverter>.Empty;
		}

		internal void SetDefauls()
		{
			SetConverter(typeof(Boolean),  (sb,v) => sb.Append((bool)v ? "1" : "0"));
			SetConverter(typeof(Char),     (sb,v) => BuildChar(sb, (char)v));
			SetConverter(typeof(SByte),    (sb,v) => sb.Append((SByte) v));
			SetConverter(typeof(Byte),     (sb,v) => sb.Append((Byte)  v));
			SetConverter(typeof(Int16),    (sb,v) => sb.Append((Int16) v));
			SetConverter(typeof(UInt16),   (sb,v) => sb.Append((UInt16)v));
			SetConverter(typeof(Int32),    (sb,v) => sb.Append((Int32) v));
			SetConverter(typeof(UInt32),   (sb,v) => sb.Append((UInt32)v));
			SetConverter(typeof(Int64),    (sb,v) => sb.Append((Int64) v));
			SetConverter(typeof(UInt64),   (sb,v) => sb.Append((UInt64)v));
			SetConverter(typeof(Single),   (sb,v) => sb.Append(((float)  v).ToString(_numberFormatInfo)));
			SetConverter(typeof(Double),   (sb,v) => sb.Append(((double) v).ToString(_numberFormatInfo)));
			SetConverter(typeof(Decimal),  (sb,v) => sb.Append(((decimal)v).ToString(_numberFormatInfo)));
			SetConverter(typeof(DateTime), (sb,v) => BuildDateTime(sb, (DateTime)v));
			SetConverter(typeof(String),   (sb,v) => BuildString  (sb, v.ToString()));
			SetConverter(typeof(Guid),     (sb,v) => sb.Append('\'').Append(v).Append('\''));
		}

		readonly ValueToSqlConverter[]                           _baseConverters;
		readonly Dictionary<Type,  Action<StringBuilder,object>> _basicConverters    = new Dictionary<Type,  Action<StringBuilder,object>>();
		readonly Dictionary<object,Action<StringBuilder,object>> _dataTypeConverters = new Dictionary<object,Action<StringBuilder,object>>();

		Action<StringBuilder,object> _booleanConverter;
		Action<StringBuilder,object> _charConverter;
		Action<StringBuilder,object> _sByteConverter;
		Action<StringBuilder,object> _byteConverter;
		Action<StringBuilder,object> _int16Converter;
		Action<StringBuilder,object> _uInt16Converter;
		Action<StringBuilder,object> _int32Converter;
		Action<StringBuilder,object> _uInt32Converter;
		Action<StringBuilder,object> _int64Converter;
		Action<StringBuilder,object> _uInt64Converter;
		Action<StringBuilder,object> _singleConverter;
		Action<StringBuilder,object> _doubleConverter;
		Action<StringBuilder,object> _decimalConverter;
		Action<StringBuilder,object> _dateTimeConverter;
		Action<StringBuilder,object> _stringConverter;

		static readonly NumberFormatInfo _numberFormatInfo = new NumberFormatInfo
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

		public bool TryConvert(StringBuilder stringBuilder, DataType dataType, object value)
		{
			if (dataType != DataType.Undefined && _dataTypeConverters.Count > 0)
			{
				if (value == null)
				{
					stringBuilder.Append("NULL");
					return true;
				}

				Action<StringBuilder,object> converter;

				if (_dataTypeConverters.TryGetValue(new { type = value.GetType(), dataType = dataType }, out converter))
				{
					if (converter != null)
					{
						converter(stringBuilder, value);
						return true;
					}
				}
			}

			return TryConvert(stringBuilder, value);
		}

		public bool TryConvert(StringBuilder stringBuilder, object value)
		{
			if (value == null)
			{
				stringBuilder.Append("NULL");
				return true;
			}

			var type = value.GetType();

			Action<StringBuilder,object> converter = null;

			if (_basicConverters.Count > 0 && !type.IsEnum)
			{
				switch (type.GetTypeCodeEx())
				{
					case TypeCode.DBNull   : stringBuilder.Append("NULL");   return true;
					case TypeCode.Boolean  : converter = _booleanConverter;  break;
					case TypeCode.Char     : converter = _charConverter;     break;
					case TypeCode.SByte    : converter = _sByteConverter;    break;
					case TypeCode.Byte     : converter = _byteConverter;     break;
					case TypeCode.Int16    : converter = _int16Converter;    break;
					case TypeCode.UInt16   : converter = _uInt16Converter;   break;
					case TypeCode.Int32    : converter = _int32Converter;    break;
					case TypeCode.UInt32   : converter = _uInt32Converter;   break;
					case TypeCode.Int64    : converter = _int64Converter;    break;
					case TypeCode.UInt64   : converter = _uInt64Converter;   break;
					case TypeCode.Single   : converter = _singleConverter;   break;
					case TypeCode.Double   : converter = _doubleConverter;   break;
					case TypeCode.Decimal  : converter = _decimalConverter;  break;
					case TypeCode.DateTime : converter = _dateTimeConverter; break;
					case TypeCode.String   : converter = _stringConverter;   break;
					default                : _basicConverters.TryGetValue(type, out converter); break;
				}
			}

			if (converter != null)
			{
				converter(stringBuilder, value);
				return true;
			}

			if (_baseConverters.Length > 0)
				foreach (var valueConverter in _baseConverters)
					if (valueConverter.TryConvert(stringBuilder, value))
						return true;

			return false;
		}

		public StringBuilder Convert(StringBuilder stringBuilder, object value)
		{
			if (!TryConvert(stringBuilder, value))
				stringBuilder.Append(value);
			return stringBuilder;
		}

		public StringBuilder Convert(StringBuilder stringBuilder, DataType dataType, object value)
		{
			if (!TryConvert(stringBuilder, dataType, value))
				stringBuilder.Append(value);
			return stringBuilder;
		}

		public void SetConverter(Type type, Action<StringBuilder,object> converter)
		{
			if (converter == null)
			{
				if (_basicConverters.ContainsKey(type))
					_basicConverters.Remove(type);
			}
			else
			{
				_basicConverters[type] = converter;

				switch (type.GetTypeCodeEx())
				{
					case TypeCode.Boolean  : _booleanConverter  = converter; return;
					case TypeCode.Char     : _charConverter     = converter; return;
					case TypeCode.SByte    : _sByteConverter    = converter; return;
					case TypeCode.Byte     : _byteConverter     = converter; return;
					case TypeCode.Int16    : _int16Converter    = converter; return;
					case TypeCode.UInt16   : _uInt16Converter   = converter; return;
					case TypeCode.Int32    : _int32Converter    = converter; return;
					case TypeCode.UInt32   : _uInt32Converter   = converter; return;
					case TypeCode.Int64    : _int64Converter    = converter; return;
					case TypeCode.UInt64   : _uInt64Converter   = converter; return;
					case TypeCode.Single   : _singleConverter   = converter; return;
					case TypeCode.Double   : _doubleConverter   = converter; return;
					case TypeCode.Decimal  : _decimalConverter  = converter; return;
					case TypeCode.DateTime : _dateTimeConverter = converter; return;
					case TypeCode.String   : _stringConverter   = converter; return;
				}
			}
		}

		public void SetConverter(Type type, DataType dataType, Action<StringBuilder,object> converter)
		{
			var key = new { type, dataType };

			if (converter == null)
			{
				if (_dataTypeConverters.ContainsKey(key))
					_dataTypeConverters.Remove(key);
			}
			else
			{
				_dataTypeConverters[key] = converter;
			}
		}
	}
}
