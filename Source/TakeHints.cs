using System;

namespace LinqToDB
{
	/// <summary>
	/// Hints for Take
	/// <see cref="LinqExtensions.Take{TSource}(IQueryable{TSource}, int, TakeHints)"/>
	/// <see cref="LinqExtensions.Take{TSource}(IQueryable{TSource}, System.Linq.Expressions.Expression{Func{int}}, TakeHints)"/>.
	/// </summary>
	[Flags]
	public enum TakeHints
	{
		/// <summary>
		/// SELECT TOP 10 PERCENT.
		/// </summary>
		Percent = 1,
		/// <summary>
		/// SELECT TOP 10 WITH TIES.
		/// </summary>
		WithTies = 2
	}
}
