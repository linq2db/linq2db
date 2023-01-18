using System;

namespace LinqToDB.SqlQuery
{
	public class SqlQueryExtension : ISqlExpressionWalkable
	{
		public string?                           Configuration { get; set; }
		public Sql.QueryExtensionScope           Scope         { get; set; }
		public Dictionary<string,ISqlExpression> Arguments     { get; init; } = new();
		public Type?                             BuilderType   { get; set; }

		public ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext,ISqlExpression,ISqlExpression> func)
		{
			foreach (var argument in Arguments.ToList())
				Arguments[argument.Key] = argument.Value.Walk(options, context, func)!;

			return null;
		}
	}
}
