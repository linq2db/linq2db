using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using LinqToDB.Common.Internal.Cache;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Linq
{
	internal static class DataReaderWrapCache
	{
		private static readonly MemoryCache _readerMappings = new MemoryCache(new MemoryCacheOptions());

		static DataReaderWrapCache()
		{
			Query.CacheCleaners.Enqueue(() => _readerMappings.Clear());
		}

		internal static IDataReader TryUnwrapDataReader(MappingSchema mappingSchema, IDataReader dataReader)
		{
			var converter = _readerMappings.GetOrCreate(
				Tuple.Create(dataReader.GetType(), mappingSchema.ConfigurationID),
				mappingSchema,
				static (entry, key, ms) =>
				{
					var expr = ms.GetConvertExpression(key.Item1, typeof(IDataReader), false, false);
					if (expr != null)
					{
						var param = Expression.Parameter(typeof(IDataReader));
						expr      = Expression.Lambda(expr.GetBody(Expression.Convert(param, key.Item1)), param);

						return (Func<IDataReader, IDataReader>)expr.Compile();
					}

					return null;
				});

			return converter?.Invoke(dataReader) ?? dataReader;
		}
	}
}
