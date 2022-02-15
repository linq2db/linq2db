using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	using Common;
	using LinqToDB.Common.Internal.Cache;
	using LinqToDB.Expressions;
	using Mapping;

	static class DataReaderWrapCache
	{
		static readonly MemoryCache<(Type dataReaderType, string schemaId)> _readerMappings = new (new ());

		static DataReaderWrapCache()
		{
			Query.CacheCleaners.Enqueue(() => _readerMappings.Clear());
		}

		internal static DbDataReader TryUnwrapDataReader(MappingSchema mappingSchema, DbDataReader dataReader)
		{
			var converter = _readerMappings.GetOrCreate(
				(dataReaderType: dataReader.GetType(), schemaId: mappingSchema.ConfigurationID),
				mappingSchema,
				static (entry, ms) =>
				{
					var expr = ms.GetConvertExpression(entry.Key.dataReaderType, typeof(DbDataReader), false, false);

					if (expr != null)
					{
						var param = Expression.Parameter(typeof(DbDataReader));
						expr      = Expression.Lambda(expr.GetBody(Expression.Convert(param, entry.Key.dataReaderType)), param);

						return (Func<DbDataReader, DbDataReader>)expr.CompileExpression();
					}

					return null;
				});

			return converter?.Invoke(dataReader) ?? dataReader;
		}
	}
}
