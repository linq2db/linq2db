using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.MySql
{
	public class MariaDBSqlExpressionConvertVisitor : MySqlSqlExpressionConvertVisitor
	{
		public MariaDBSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		/// <summary>
		/// The ranking functions and <c>LAG</c>/<c>LEAD</c> need an <c>ORDER BY</c> here - <c>No order list in window
		/// specification for 'rank'</c>. <c>ROW_NUMBER</c>, <c>NTILE</c>, the <c>*_VALUE</c> pair and framed aggregates
		/// are left alone, and MySQL proper - which takes a bare <c>OVER ()</c> for every window function - keeps the
		/// base's answer of no requirement at all.
		/// </summary>
		protected override bool IsWindowOrderByRequired(SqlExtendedFunction func)
			=> base.IsWindowOrderByRequired(func)
				|| IsOrderDependentWindowFunction(func.FunctionName);
	}
}
