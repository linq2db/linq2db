using LinqToDB.Linq;

namespace LinqToDB
{
	public partial class Sql
	{
		/// <summary>
		/// Forces LINQ translator to generate in <b>ORDER BY</b> with ordinal position, instead of SQL Expression.
		/// <p/>
		/// Can be used in LINQ query as top level order expression.
		/// For example the following two similar queries
		/// <code>
		/// query = query
		///		.OrderBy(x => Sql.OrderIndex(x.Field2))
		///		.ThenBy(x => x.Field2);
		/// </code>
		/// <code>
		/// query =	from q in query
		///		orderby Sql.OrderIndex(q.Field2), q.Field1
		///		select q;
		/// </code>
		/// Should generate thw following SQL:
		/// <code>
		/// SELECT
		///    t.Field1,
		///    t.Field2
		/// FROM SomeTable t
		/// ORDER BY 2, t.Field1
		/// </code>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="expression"></param>
		/// <returns>The same <paramref name="expression"/> when calling directly.</returns>
		/// <exception cref="LinqException">Exception is throw when used not in OrderBy/ThenBy extension methods.</exception>
		public static T OrderIndex<T>(T expression) => expression;
	}
}
