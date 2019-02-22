using System;

namespace LinqToDB.Linq.Builder
{
	public enum RequestFor
	{
		/// <summary>
		/// Checks the sequence if the expression is a table or an association.
		/// </summary>
		Table,

		/// <summary>
		/// Checks the sequence if the expression is an association.
		/// </summary>
		Association,

		/// <summary>
		/// Checks the sequence if the expression is a table, an association, new {}, or new MyClass {}.
		/// </summary>
		Object,

		/// <summary>
		/// Checks the sequence if the expression is a group join.
		/// </summary>
		GroupJoin,

		//// <summary>
		//// Checks the sequence if the expression is a field or single value expression.
		//// </summary>
		//Scalar,

		/// <summary>
		/// Checks the sequence if the expression is a field.
		/// </summary>
		Field,

		/// <summary>
		/// Checks the sequence if the expression contains an SQL expression.
		/// </summary>
		Expression,

		/// <summary>
		/// Checks the context if it's a subquery.
		/// </summary>
		SubQuery,

		/// <summary>
		/// Checks the context if it's a root of the expression.
		/// </summary>
		Root,
	}
}
