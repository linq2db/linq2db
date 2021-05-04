using System;
using System.Data;
using System.Linq.Expressions;
using LinqToDB.Common;
using LinqToDB.Common.Internal.Cache;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Linq
{
	internal static class DataReaderWrapCache
	{
		private static readonly MemoryCache<(Type dataReaderType, string schemaId)> _readerMappings = new (new ());

		static DataReaderWrapCache()
		{
			Query.CacheCleaners.Enqueue(() => _readerMappings.Clear());
		}

		internal static IDataReader TryUnwrapDataReader(MappingSchema mappingSchema, IDataReader dataReader)
		{
			var converter = _readerMappings.GetOrCreate(
				(dataReaderType: dataReader.GetType(), schemaId: mappingSchema.ConfigurationID),
				mappingSchema,
				static (entry, ms) =>
				{
					var expr = ms.GetConvertExpression(entry.Key.dataReaderType, typeof(IDataReader), false, false);
					if (expr != null)
					{
						var param = Expression.Parameter(typeof(IDataReader));
						expr      = Expression.Lambda(expr.GetBody(Expression.Convert(param, entry.Key.dataReaderType)), param);

						return (Func<IDataReader, IDataReader>)expr.CompileExpression();
					}

					return null;
				});

			return converter?.Invoke(dataReader) ?? dataReader;
		}
	}
}
