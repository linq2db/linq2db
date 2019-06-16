namespace LinqToDB.ServiceModel
{
	using LinqToDB.Common;
	using LinqToDB.Expressions;
	using LinqToDB.Extensions;
	using Mapping;
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq.Expressions;

	/// <summary>
	/// Implements conversions support between raw values and string to support de-/serialization of remote data context
	/// query AST and result values.
	/// </summary>
	internal class SerializationConverter
	{
		private static readonly Type _stringType = typeof(string);
		private static readonly IDictionary<object, Func<object, string>> _serializeConverters   = new ConcurrentDictionary<object, Func<object, string>>();
		private static readonly IDictionary<object, Func<string, object>> _deserializeConverters = new ConcurrentDictionary<object, Func<string, object>>();

		public static string Serialize(MappingSchema ms, object value)
		{
			if (value is string stringValue)
				return stringValue;

			var from = value.GetType();

			// don't see much sense to have multiple schema-dependent serialziation logic for same type
			// otherwise we should care about converters cleanup
			var key  = from;

			if (!_serializeConverters.TryGetValue(key, out var converter))
			{
				Type enumType = null;
				if (from.IsEnum)
				{
					enumType = from;
					from     = Enum.GetUnderlyingType(from);
				}

				var li = ms.GetConverter(new DbDataType(from), new DbDataType(_stringType), true);
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

				converter = ex.Compile();

				_serializeConverters[key] = converter;
			}

			return converter(value);
		}

		public static object Deserialize(MappingSchema ms, Type to, string value)
		{
			if (value == null)
				return null;

			if (to == _stringType)
				return value;

			to = to.ToNullableUnderlying();

			// don't see much sense to have multiple schema-dependent serialziation logic for same type
			// otherwise we should care about converters cleanup
			var key = to;

			if (!_deserializeConverters.TryGetValue(key, out var converter))
			{
				Type enumType = null;
				if (to.IsEnum)
				{
					enumType = to;
					to       = Enum.GetUnderlyingType(to);
				}

				var li = ms.GetConverter(new DbDataType(_stringType), new DbDataType(to), true);

				var b  = li.CheckNullLambda.Body;
				var ps = li.CheckNullLambda.Parameters;

				if (enumType != null)
					b = Expression.Convert(b, enumType);

				var p  = Expression.Parameter(_stringType, "p");
				var ex = Expression.Lambda<Func<string, object>>(
					Expression.Convert(b, typeof(object)).Transform(e => e == ps[0] ? p : e),
					p);

				converter = ex.Compile();

				_deserializeConverters[key] = converter;
			}

			return converter(value);
		}
	}
}
