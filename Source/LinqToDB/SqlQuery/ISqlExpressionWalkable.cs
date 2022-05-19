using System;

namespace LinqToDB.SqlQuery;

public class WalkOptions
{
	private WalkOptions(bool skipColumnDeclaration, bool processParent)
	{
		SkipColumnDeclaration = skipColumnDeclaration;
		ProcessParent         = processParent;
	}

	public readonly bool SkipColumnDeclaration;
	public readonly bool ProcessParent;

	public static readonly WalkOptions Default                   = new (false, false);
	public static readonly WalkOptions WithSkipColumnDeclaration = new (true, false);
	public static readonly WalkOptions WithProcessParent         = new (false, true);
}

public interface ISqlExpressionWalkable
{
	ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression,ISqlExpression> func);
}
