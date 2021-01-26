#if NETFRAMEWORK
using System;
using System.Linq.Expressions;

namespace LinqToDB.ServiceModel
{
	using Common;
	using Common.Internal.Cache;
	using Expressions;
	using Extensions;
	using Mapping;

	/// <summary>
	/// Implements conversions support between raw values and string to support de-/serialization of remote data context
	/// query AST and result values.
	/// </summary>
	internal class SerializationConverter
	{
		static readonly Type _stringType = typeof(string);
		static readonly MemoryCache _serializeConverters   = new MemoryCache(new MemoryCacheOptions());
		static readonly MemoryCache _deserializeConverters = new MemoryCache(new MemoryCacheOptions());

		public static void ClearCaches()
		{
			_serializeConverters.Compact(1);
			_deserializeConverters.Compact(1);
		}

		public static string Serialize(MappingSchema ms, object value)
		{
			if (value is string stringValue)
				return stringValue;

			var from = value.GetType();

			var key = new { from, ms.ConfigurationID };

			var converter = _serializeConverters.GetOrCreate(key, o =>
			{
				o.SlidingExpiration = Configuration.Linq.CacheSlidingExpiration;

				Type? enumType = null;

				var li = ms.GetConverter(new DbDataType(from), new DbDataType(_stringType), false);
				if (li == null && from.IsEnum)
				{
					enumType = from;
					from     = Enum.GetUnderlyingType(from);
				}

				if (li == null)
					li = ms.GetConverter(new DbDataType(from), new DbDataType(_stringType), true)!;

				var b  = li.CheckNullLambda.Body;
				var ps = li.CheckNullLambda.Parameters;

				var p = Expression.Parameter(typeof(object), "p");
				var ex = Expression.Lambda<Func<object, string>>(
					b.Transform(e =>
						e == ps[0]
							? Expression.Convert(
								enumType != null ? Expression.Convert(p, enumType) : (Expression)p,
								e.Type)
							: e),
					p);

				return ex.Compile();
			});

			return converter(value);
		}

		public static object? Deserialize(MappingSchema ms, Type to, string? value)
		{
			if (value == null)
				return null;

			if (to == _stringType)
				return value;

			to = to.ToNullableUnderlying();

			var key = new { to, ms.ConfigurationID };

			var converter = _deserializeConverters.GetOrCreate(key, o =>
			{
				o.SlidingExpiration = Configuration.Linq.CacheSlidingExpiration;

				Type? enumType = null;

				var li = ms.GetConverter(new DbDataType(_stringType), new DbDataType(to), false);
				if (li == null && to.IsEnum)
				{
					var type = Converter.GetDefaultMappingFromEnumType(ms, to);
					if (type != null)
					{
						if (type == typeof(int) || type == typeof(long))
							enumType = to;
						to = type;
					}	
					else
					{
						enumType = to;
						to = Enum.GetUnderlyingType(to);
					}
				}

				if (li == null)
					li = ms.GetConverter(new DbDataType(_stringType), new DbDataType(to), true)!;

				var b  = li.CheckNullLambda.Body;
				var ps = li.CheckNullLambda.Parameters;

				if (enumType != null)
					b = Expression.Convert(b, enumType);

				var p  = Expression.Parameter(_stringType, "p");
				var ex = Expression.Lambda<Func<string, object>>(
					Expression.Convert(b, typeof(object)).Transform(e => e == ps[0] ? p : e),
					p);

				return ex.Compile();
			});

			return converter(value);
		}
	}
}
#endif
