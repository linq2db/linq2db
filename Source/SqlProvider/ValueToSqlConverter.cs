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
			_converters = converters ?? Array<ValueToSqlConverter>.Empty;
		}

		internal void SetDefauls()
		{
			if (_converters.Length == 0)
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
		}

		readonly ValueToSqlConverter[]                         _converters;
		readonly Dictionary<Type,Action<StringBuilder,object>> _converterDictionary = new Dictionary<Type,Action<StringBuilder,object>>();

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

		interface INullableValueReader
		{
			object GetValue(object value);
		}

		class NullableValueReader<T> : INullableValueReader where T : struct
		{
			public object GetValue(object value)
			{
				return ((T?)value).Value;
			}
		}

		static readonly Dictionary<Type,INullableValueReader> _nullableValueReader = new Dictionary<Type,INullableValueReader>();

		public StringBuilder Convert(StringBuilder stringBuilder, object value)
		{
			if (value == null)
				return stringBuilder.Append("NULL");

			var type = value.GetType();

			Action<StringBuilder,object> converter = null;

			if (_converterDictionary.Count > 0)
			{
				switch (type.GetTypeCodeEx())
				{
					case TypeCode.DBNull   : return stringBuilder.Append("NULL");
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
					default                : _converterDictionary.TryGetValue(type, out converter); break;
				}
			}

			if (converter == null)
			{
				if (_converters.Length > 0)
				{
					foreach (var valueConverter in _converters)
						if (valueConverter.HasConverter(type))
							return valueConverter.Convert(stringBuilder, value);
				}

				if (type.IsGenericTypeEx() && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					type = type.GetGenericArgumentsEx()[0];

					if (type.IsEnumEx())
					{
						lock (_nullableValueReader)
						{
							INullableValueReader reader;

							if (_nullableValueReader.TryGetValue(type, out reader) == false)
							{
								reader = (INullableValueReader)Activator.CreateInstance(typeof(NullableValueReader<>).MakeGenericType(type));
								_nullableValueReader.Add(type, reader);
							}

							value = reader.GetValue(value);
						}
					}
				}
			}

			if (converter != null)
			{
				converter(stringBuilder, value);
				return stringBuilder;
			}

			return stringBuilder.Append(value);
		}

		public void SetConverter(Type type, Action<StringBuilder,object> converter)
		{
			if (converter == null)
			{
				if (_converterDictionary.ContainsKey(type))
					_converterDictionary.Remove(type);
			}
			else
			{
				_converterDictionary[type] = converter;

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

		public bool HasConverter(Type type)
		{
			if (_converterDictionary.ContainsKey(type))
				return true;

			foreach (var valueConverter in _converters)
				if (valueConverter.HasConverter(type))
					return true;

			return false;
		}
	}
}
