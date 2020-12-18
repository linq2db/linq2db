using System;

namespace LinqToDB.SqlQuery
{
	public class WalkOptions
	{
		public WalkOptions()
		{
		}

		public WalkOptions(bool skipColumns)
		{
			SkipColumns = skipColumns;
		}

		public bool SkipColumns;
		public bool SkipColumnDeclaration;
		public bool ProcessParent;
	}

	public interface ISqlExpressionWalkable
	{
		ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func);
	}
}
