using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SQLite
{
	public static class SQLiteHints
	{
		public static class TableHint
		{
			public const string NotIndexed = "NOT INDEXED";

			[Sql.Function]
			public static string IndexedBy(string value)
			{
				return "INDEXED BY " + value;
			}
		}

		[ExpressionMethod(nameof(IndexedByImpl))]
		public static ITable<TSource> IndexedBy<TSource>(this ITable<TSource> table, string indexName)
			where TSource : notnull
		{
			return table.TableHint(TableHint.IndexedBy(indexName));
		}

		static Expression<Func<ITable<TSource>,string,ITable<TSource>>> IndexedByImpl<TSource>()
			where TSource : notnull
		{
			return (table, indexName) => table.TableHint(TableHint.IndexedBy(indexName));
		}

		[ExpressionMethod(nameof(NotIndexedImpl))]
		public static ITable<TSource> NotIndexed<TSource>(this ITable<TSource> table)
			where TSource : notnull
		{
			return table.TableHint(TableHint.NotIndexed);
		}

		static Expression<Func<ITable<TSource>,ITable<TSource>>> NotIndexedImpl<TSource>()
			where TSource : notnull
		{
			return table => table.TableHint(TableHint.NotIndexed);
		}
	}
}
