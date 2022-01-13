using System;

namespace LinqToDB.SqlQuery
{
	public class WalkOptions
	{
		private WalkOptions(bool skipColumns, bool skipColumnDeclaration, bool processParent)
		{
			SkipColumns           = skipColumns;
			SkipColumnDeclaration = skipColumnDeclaration;
			ProcessParent         = processParent;
		}

		// TODO: unused (not set). remove?
		public readonly bool SkipColumns;
		public readonly bool SkipColumnDeclaration;
		public readonly bool ProcessParent;

		public static readonly WalkOptions Default                   = new (false, false, false);
		public static readonly WalkOptions WithSkipColumnDeclaration = new (false, true,  false);
		public static readonly WalkOptions WithSkipColumns           = new (true,  false, false);
		public static readonly WalkOptions WithProcessParent         = new (false, false, true);
	}

	public interface ISqlExpressionWalkable
	{
		ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression,ISqlExpression> func);
	}
}
