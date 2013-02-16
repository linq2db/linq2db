using System;
using System.Collections.Concurrent;
using System.Data.Linq;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Xml;

namespace LinqToDB.Common
{
	using Expressions;
	using Mapping;

	public static class Converter
	{
		static readonly ConcurrentDictionary<object,LambdaExpression> _expressions = new ConcurrentDictionary<object,LambdaExpression>();

		static XmlDocument CreateXmlDocument(string str)
		{
			var xml = new XmlDocument();
			xml.LoadXml(str);
			return xml;
		}

		static Converter()
		{
			SetConverter<string,         Binary>     (v => new Binary(Convert.FromBase64String(v)));
			SetConverter<Binary,         string>     (v => Convert.ToBase64String(v.ToArray()));
			SetConverter<Binary,         byte[]>     (v => v.ToArray());
			SetConverter<bool,           decimal>    (v => v ? 1m : 0m);
			SetConverter<DateTimeOffset, DateTime>   (v => v.LocalDateTime);
			SetConverter<string,         XmlDocument>(v => CreateXmlDocument(v));
			SetConverter<string,         byte[]>     (v => Convert.FromBase64String(v));
			SetConverter<byte[],         string>     (v => Convert.ToBase64String(v));
			SetConverter<TimeSpan,       DateTime>   (v => DateTime.MinValue + v);
			SetConverter<DateTime,       TimeSpan>   (v => v - DateTime.MinValue);
			SetConverter<string,         DateTime>   (v => DateTime.Parse(v, null, DateTimeStyles.NoCurrentDateDefault));
			SetConverter<char,           bool>       (v => ToBoolean(v));
			SetConverter<string,         bool>       (v => v.Length == 1 ? ToBoolean(v[0]) : bool.Parse(v));
		}

		static bool ToBoolean(char ch)
		{
			switch (ch)
			{
				case '\x0' : // Allow int <=> Char <=> Boolean
				case   '0' :
				case   'n' :
				case   'N' :
				case   'f' :
				case   'F' : return false;

				case '\x1' : // Allow int <=> Char <=> Boolean
				case   '1' :
				case   'y' :
				case   'Y' :
				case   't' :
				case   'T' : return true;
			}

			throw new InvalidCastException("Invalid cast from System.String to System.Bool");
		}

		public static void SetConverter<TFrom,TTo>(Expression<Func<TFrom,TTo>> expr)
		{
			_expressions[new { from = typeof(TFrom), to = typeof(TTo) }] = expr;
		}

		internal static LambdaExpression GetConverter(Type from, Type to)
		{
			LambdaExpression l;
			_expressions.TryGetValue(new { from, to }, out l);
			return l;
		}

		static readonly ConcurrentDictionary<object,Func<object,object>> _converters = new ConcurrentDictionary<object,Func<object,object>>();

		public static object ChangeType(object value, Type conversionType, MappingSchema mappingSchema = null)
		{
			if (value == null)
				return mappingSchema == null ?
					DefaultValue.GetValue(conversionType) :
					mappingSchema.GetDefaultValue(conversionType);

			if (value.GetType() == conversionType)
				return value;

			var from = value.GetType();
			var to   = conversionType;
			var key  = new { from, to };

			var converters = mappingSchema == null ? _converters : mappingSchema.Converters;

			Func<object,object> l;

			if (!converters.TryGetValue(key, out l))
			{
				var li =
					ConvertInfo.Default.Get   (               value.GetType(), to) ??
					ConvertInfo.Default.Create(mappingSchema, value.GetType(), to);

				var b  = li.CheckNullLambda.Body;
				var ps = li.CheckNullLambda.Parameters;

				var p  = Expression.Parameter(typeof(object), "p");
				var ex = Expression.Lambda<Func<object,object>>(
					Expression.Convert(
						b.Transform(e =>
							e == ps[0] ?
								Expression.Convert(p, e.Type) :
							IsDefaultValuePlaceHolder(e) ? 
								new DefaultValueExpression(mappingSchema, e.Type) :
								e),
						typeof(object)),
					p);

				l = ex.Compile();

				converters[key] = l;
			}

			return l(value);
		}

		static class ExprHolder<T>
		{
			public static readonly ConcurrentDictionary<Type,Func<object,T>> Converters = new ConcurrentDictionary<Type,Func<object,T>>();
		}

		public static T ChangeTypeTo<T>(object value, MappingSchema mappingSchema = null)
		{
			if (value == null)
				return DefaultValue<T>.Value;

			if (value.GetType() == typeof(T))
				return (T)value;

			var from = value.GetType();
			var to   = typeof(T);

			Func<object,T> l;

			if (!ExprHolder<T>.Converters.TryGetValue(from, out l))
			{
				var li = ConvertInfo.Default.Get(value.GetType(), to) ?? ConvertInfo.Default.Create(mappingSchema, value.GetType(), to);
				var b  = li.CheckNullLambda.Body;
				var ps = li.CheckNullLambda.Parameters;

				var p  = Expression.Parameter(typeof(object), "p");
				var ex = Expression.Lambda<Func<object,T>>(
					b.Transform(e =>
						e == ps[0] ?
							Expression.Convert (p, e.Type) :
							IsDefaultValuePlaceHolder(e) ?
								new DefaultValueExpression(mappingSchema, e.Type) :
								e),
					p);

				l = ex.Compile();

				ExprHolder<T>.Converters[from] = l;
			}

			return l(value);
		}

		internal static bool IsDefaultValuePlaceHolder(Expression expr)
		{
			if (expr is MemberExpression)
			{
				var me = (MemberExpression)expr;

				if (me.Member.Name == "Value" && me.Member.DeclaringType.IsGenericType)
					return me.Member.DeclaringType.GetGenericTypeDefinition() == typeof(DefaultValue<>);
			}

			return expr is DefaultValueExpression;
		}

		public static Type GetDefaultMappingFromEnumType(MappingSchema mappingSchema, Type enumType)
		{
			return ConvertBuilder.GetDefaultMappingFromEnumType(mappingSchema, enumType);
		}
	}
}
