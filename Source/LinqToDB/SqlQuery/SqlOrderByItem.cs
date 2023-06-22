using System;

namespace LinqToDB.SqlQuery
{
	public class SqlOrderByItem : IQueryElement
	{
		public SqlOrderByItem(ISqlExpression expression, bool isDescending)
		{
			Expression   = expression;
			IsDescending = isDescending;
		}

		public ISqlExpression Expression   { get; internal set; }
		public bool           IsDescending { get; }

		internal void Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			Expression = Expression.Walk(options, context, func)!;
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region IQueryElement Members

#if DEBUG
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public string DebugText => this.ToDebugString();
#endif

		public QueryElementType ElementType => QueryElementType.OrderByItem;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer.AppendElement(Expression);

			if (IsDescending)
				writer.Append(" DESC");

			return writer;
		}

		#endregion
	}
}
