namespace LinqToDB
{
	/// <summary>
	/// NULL handling for <see cref="LinqExtensions.Unpivot{TSource,TValue,TResult}(System.Linq.IQueryable{TSource},UnpivotNulls,System.Linq.Expressions.Expression{System.Func{TSource,string,TValue,TResult}},System.Linq.Expressions.Expression{System.Func{TSource,TValue}},System.Linq.Expressions.Expression{System.Func{TSource,TValue}}[])"/>.
	/// </summary>
	public enum UnpivotNulls
	{
		/// <summary>Rows whose value cell is NULL are excluded (the SQL <c>UNPIVOT</c> default).</summary>
		ExcludeNulls = 0,

		/// <summary>Rows whose value cell is NULL are kept (the SQL <c>INCLUDE NULLS</c> semantics).</summary>
		IncludeNulls = 1,
	}
}
