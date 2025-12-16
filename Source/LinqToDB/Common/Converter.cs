using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Data.Linq;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Xml;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Conversion;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Expressions.ExpressionVisitors;
using LinqToDB.Mapping;

namespace LinqToDB.Common
{
	/// <summary>
	/// Type conversion manager.
	/// </summary>
	[PublicAPI]
	public static class Converter
	{
		static readonly ConcurrentDictionary<(Type from, Type to), LambdaExpression> _expressions = new ();

		static XmlDocument CreateXmlDocument(string str)
		{
			var xml = new XmlDocument() { XmlResolver = null };

			using var reader = XmlReader.Create(new StringReader(str), new XmlReaderSettings() { XmlResolver = null });
			xml.Load(reader);

			return xml;
		}

		static Converter()
		{
			SetConverter<string,         char>          (v => v.Length == 0 ? '\0' : v[0]);
			SetConverter<string,         Binary>        (v => new Binary(Convert.FromBase64String(v)));
			SetConverter<Binary,         string>        (v => Convert.ToBase64String(v.ToArray()));
			SetConverter<Binary,         byte[]>        (v => v.ToArray());
			SetConverter<bool,           decimal>       (v => v ? 1m : 0m);
			SetConverter<DateTimeOffset, DateTime>      (v => v.LocalDateTime);
			SetConverter<string,         XmlDocument>   (v => CreateXmlDocument(v));
			SetConverter<string,         byte[]>        (v => Convert.FromBase64String(v));
			SetConverter<byte[],         string>        (v => Convert.ToBase64String(v));
			SetConverter<TimeSpan,       DateTime>      (v => DateTime.MinValue + v);
			SetConverter<DateTime,       TimeSpan>      (v => v - DateTime.MinValue);
			SetConverter<string,         DateTime>      (v => DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault));
			SetConverter<char,           bool>          (v => ToBoolean(v));
			SetConverter<string,         bool>          (v => v.Length == 1 ? ToBoolean(v[0]) : bool.Parse(v));

#if SUPPORTS_DATEONLY
			SetConverter<DateTime,       DateOnly>      (v => DateOnly.FromDateTime(v));
			SetConverter<DateOnly,       DateTime>      (v => v.ToDateTime(TimeOnly.MinValue));

			// use DateTime.Parse() because db processing may return strings that are full date/time.
			SetConverter<string,         DateOnly>      (v => DateOnly.FromDateTime(DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.None)));
#endif

			SetConverter<byte  , BitArray>(v => new BitArray(new byte[] { v }));
			SetConverter<sbyte , BitArray>(v => new BitArray(new byte[] { unchecked((byte)v) }));
			SetConverter<short , BitArray>(v => new BitArray(BitConverter.GetBytes(v)));
			SetConverter<ushort, BitArray>(v => new BitArray(BitConverter.GetBytes(v)));
			SetConverter<int   , BitArray>(v => new BitArray(BitConverter.GetBytes(v)));
			SetConverter<uint  , BitArray>(v => new BitArray(BitConverter.GetBytes(v)));
			SetConverter<long  , BitArray>(v => new BitArray(BitConverter.GetBytes(v)));
			SetConverter<ulong , BitArray>(v => new BitArray(BitConverter.GetBytes(v)));
		}

		static bool ToBoolean(char ch)
		{
			return ch switch
			{
				// Allow int <=> Char <=> Boolean
				'\x0' or '0' or 'n' or 'N' or 'f' or 'F' => false,
				// Allow int <=> Char <=> Boolean
				'\x1' or '1' or 'y' or 'Y' or 't' or 'T' => true,

				_ => throw new InvalidCastException("Invalid cast from System.String to System.Bool"),
			};
		}

		/// <summary>
		/// Sets custom converter from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source conversion type.</typeparam>
		/// <typeparam name="TTo">Target conversion type.</typeparam>
		/// <param name="expr">Converter expression.</param>
		public static void SetConverter<TFrom,TTo>(Expression<Func<TFrom,TTo>> expr)
		{
			_expressions[(typeof(TFrom), typeof(TTo))] = expr;
		}

		/// <summary>
		/// Tries to get converter from <paramref name="from"/> to <paramref name="to"/> type.
		/// </summary>
		/// <param name="from">Source conversion type.</param>
		/// <param name="to">Target conversion type.</param>
		/// <returns>Conversion expression or null, of converter not found.</returns>
		internal static LambdaExpression? GetConverter(Type from, Type to)
		{
			_expressions.TryGetValue((from, to), out var l);
			return l;
		}

		static readonly ConcurrentDictionary<object,Func<object,object>> _converters = new ();

		/// <summary>
		/// Converts value to <paramref name="toConvertType"/> type.
		/// </summary>
		/// <param name="value">Value to convert.</param>
		/// <param name="toConvertType">Target conversion type.</param>
		/// <param name="mappingSchema">Optional mapping schema.</param>
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for details.</param>
		/// <returns>Converted value.</returns>
		public static object? ChangeType(object? value, Type toConvertType, MappingSchema? mappingSchema = null, ConversionType conversionType = ConversionType.Common)
		{
			if (value.IsNullValue())
				return mappingSchema == null ?
					DefaultValue.GetValue(toConvertType) :
					mappingSchema.GetDefaultValue(toConvertType);

			var from = value.GetType();
			if (from == toConvertType)
				return value;

			var to   = toConvertType;
			var key  = new { from, to };

			var converters = mappingSchema == null ? _converters : mappingSchema.Converters;

			if (!converters.TryGetValue(key, out var l))
			{
				var li = mappingSchema != null
					? mappingSchema.GetConverter(new DbDataType(from), new DbDataType(to), true, conversionType)!
					: ConvertInfo.Default.Get(from, to, conversionType) ?? ConvertInfo.Default.Create(mappingSchema, from, to, conversionType);

				var b  = li.CheckNullLambda.Body;
				var ps = li.CheckNullLambda.Parameters;

				var p  = Expression.Parameter(typeof(object), "p");
				var ex = Expression.Lambda<Func<object,object>>(
					Expression.Convert(
						b.Transform(
							(mappingSchema, ps, p),
							static (context, e) =>
								e == context.ps[0] ?
									Expression.Convert(context.p, e.Type) :
								IsDefaultValuePlaceHolder(e) ?
									new DefaultValueExpression(context.mappingSchema, e.Type) :
									e),
						typeof(object)),
					p);

				l = ex.CompileExpression();

				converters[key] = l;
			}

			return l(value);
		}

		static class ExprHolder<T>
		{
			public static readonly ConcurrentDictionary<Type,Func<object,T>> Converters = new ();
		}

		/// <summary>
		/// Converts value to <typeparamref name="T"/> type.
		/// </summary>
		/// <typeparam name="T">Target conversion type.</typeparam>
		/// <param name="value">Value to convert.</param>
		/// <param name="mappingSchema">Optional mapping schema.</param>
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for details.</param>
		/// <returns>Converted value.</returns>
		public static T ChangeTypeTo<T>(object? value, MappingSchema? mappingSchema = null, ConversionType conversionType = ConversionType.Common)
		{
			if (value.IsNullValue())
				return mappingSchema == null ?
					DefaultValue<T>.Value :
					(T)mappingSchema.GetDefaultValue(typeof(T))!;

			if (value.GetType() == typeof(T))
				return (T)value;

			var from = value.GetType();
			var to   = typeof(T);

			if (!ExprHolder<T>.Converters.TryGetValue(from, out var l))
			{
				var li = ConvertInfo.Default.Get(from, to, conversionType) ?? ConvertInfo.Default.Create(mappingSchema, from, to, conversionType);
				var b  = li.CheckNullLambda.Body;
				var ps = li.CheckNullLambda.Parameters;

				var p  = Expression.Parameter(typeof(object), "p");
				var ex = Expression.Lambda<Func<object,T>>(
					b.Transform(
						(ps, p, mappingSchema),
						static (context, e) =>
							e == context.ps[0] ?
								Expression.Convert (context.p, e.Type) :
								IsDefaultValuePlaceHolder(e) ?
									new DefaultValueExpression(context.mappingSchema, e.Type) :
									e),
					p);

				l = ex.CompileExpression();

				ExprHolder<T>.Converters[from] = l;
			}

			return l(value);
		}

		/// <summary>
		/// Returns true, if expression value is <see cref="DefaultValueExpression"/> or
		/// <code>
		/// DefaultValue&lt;T&gt;.Value
		/// </code>
		/// </summary>
		/// <param name="expr">Expression to inspect.</param>
		/// <returns><see langword="true"/>, if expression represents default value.</returns>
		internal static bool IsDefaultValuePlaceHolder(Expression expr)
		{
			if (expr is MemberExpression me)
			{
				if (me.Member.Name == "Value" && me.Member.DeclaringType!.IsGenericType)
					return me.Member.DeclaringType.GetGenericTypeDefinition() == typeof(DefaultValue<>);
			}

			return expr is DefaultValueExpression;
		}

		internal static readonly FindVisitor<object?> IsDefaultValuePlaceHolderVisitor = FindVisitor<object?>.Create(IsDefaultValuePlaceHolder);

		/// <summary>
		/// Returns type, to which provided enumeration values should be mapped.
		/// </summary>
		/// <param name="mappingSchema">Current mapping schema</param>
		/// <param name="enumType">Enumeration type.</param>
		/// <returns>Underlying mapping type.</returns>
		public static Type? GetDefaultMappingFromEnumType(MappingSchema mappingSchema, Type enumType)
		{
			return ConvertBuilder.GetDefaultMappingFromEnumType(mappingSchema, enumType);
		}

		internal static bool TryConvertToString(object? value, out string? str)
		{
			if (value is null)
			{
				str = null;
				return true;
			}

			if (value is string stringValue)
			{
				str = stringValue;
				return true;
			}

			if (value is IConvertible convertible)
			{
				try
				{
					str = convertible.ToString(null);
					return true;
				}
				catch
				{
				}
			}

			str = null;
			return false;
		}
	}
}
