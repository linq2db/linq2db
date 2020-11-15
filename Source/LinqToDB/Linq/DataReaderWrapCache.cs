using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Linq
{
	internal static class DataReaderWrapCache
	{
		private static readonly ConcurrentDictionary<Tuple<Type, MappingSchema>, Func<IDataReader, IDataReader>?> _readerMappings = new ConcurrentDictionary<Tuple<Type, MappingSchema>, Func<IDataReader, IDataReader>?>();

		static DataReaderWrapCache()
		{
			Query.CacheCleaners.Enqueue(() => _readerMappings.Clear());
		}

		internal static IDataReader TryUnwrapDataReader(MappingSchema mappingSchema, IDataReader dataReader)
		{
			var converter = _readerMappings.GetOrAdd(Tuple.Create(dataReader.GetType(), mappingSchema), key =>
			{
				var expr = key.Item2.GetConvertExpression(key.Item1, typeof(IDataReader), false, false);
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
