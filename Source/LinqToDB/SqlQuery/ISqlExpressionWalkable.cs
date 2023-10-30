using System;

namespace LinqToDB.SqlQuery
{
	// TODO:WAITFIX: ISqlExpressionWalkable will be removed
	public sealed class WalkOptions
	{
		private WalkOptions()
		{
		}

		public static readonly WalkOptions Default = new ();
	}

	public interface ISqlExpressionWalkable
	{
		ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression,ISqlExpression> func);
	}
}
