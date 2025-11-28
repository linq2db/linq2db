using System;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Remote
{
	/// <summary>
	/// Implements conversions support between raw values and string to support de-/serialization of remote data context
	/// query AST and result values.
	/// </summary>
	sealed class SerializationConverter
	{
		static readonly Type                                                       _stringType            = typeof(string);
		static readonly MemoryCache<(Type from, int schemaId),Func<object,string>> _serializeConverters   = new (new ());
		static readonly MemoryCache<(Type to  , int schemaId),Func<string,object>> _deserializeConverters = new (new ());

		public static void ClearCaches()
		{
			_serializeConverters  .Clear();
			_deserializeConverters.Clear();
		}

		public static string Serialize(MappingSchema ms, object value)
		{
			if (value is string stringValue)
				return stringValue;

			var from = value.GetType();

			var converter = _serializeConverters.GetOrCreate(
				(from, ((IConfigurationID)ms).ConfigurationID),
				ms,
				static (o, ms) =>
				{
					var from            = o.Key.from;
					o.SlidingExpiration = LinqToDB.Common.Configuration.Linq.CacheSlidingExpiration;

					Type? enumType = null;

					var li = ms.GetConverter(new(from), new(_stringType), false, ConversionType.Common);

					if (li == null && from.IsEnum)
					{
						enumType = from;
						from     = Enum.GetUnderlyingType(from);
					}

					li ??= ms.GetConverter(new(from), new(_stringType), true, ConversionType.Common)!;

					var b  = li.CheckNullLambda.Body;
					var ps = li.CheckNullLambda.Parameters;

					var p = Expression.Parameter(typeof(object), "p");
					var ex = Expression.Lambda<Func<object, string>>(
						b.Transform(
							(ps, enumType, p),
							static (context, e) =>
								e == context.ps[0]
									? Expression.Convert(
										context.enumType != null ? Expression.Convert(context.p, context.enumType) : context.p,
										e.Type)
									: e),
						p);

					return ex.CompileExpression();
				});

			return converter(value);
		}

		public static object? Deserialize(MappingSchema ms, Type to, string? value)
		{
			if (value == null)
				return null;

			if (to == _stringType)
				return value;

			to = to.UnwrapNullableType();

			var converter = _deserializeConverters.GetOrCreate(
				(to, ((IConfigurationID)ms).ConfigurationID),
				ms,
				static (o, ms) =>
				{
					var to = o.Key.to;
					o.SlidingExpiration = LinqToDB.Common.Configuration.Linq.CacheSlidingExpiration;

					Type? enumType = null;

					var li = ms.GetConverter(new DbDataType(_stringType), new DbDataType(to), false, ConversionType.Common);

					if (li == null && to.IsEnum)
					{
						enumType = to;
						to = Enum.GetUnderlyingType(to);
					}

					li ??= ms.GetConverter(new DbDataType(_stringType), new DbDataType(to), true, ConversionType.Common)!;

					var b  = li.CheckNullLambda.Body;
					var ps = li.CheckNullLambda.Parameters;

					if (enumType != null)
						b = Expression.Convert(b, enumType);

					var p  = Expression.Parameter(_stringType, "p");
					var ex = Expression.Lambda<Func<string, object>>(
						Expression.Convert(b, typeof(object)).Replace(ps[0], p),
						p);

					return ex.CompileExpression();
				});

			return converter(value);
		}
	}
}
