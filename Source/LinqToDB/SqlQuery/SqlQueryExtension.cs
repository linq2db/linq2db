using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public class SqlQueryExtension : ISqlExpressionWalkable
	{
		public string?                           Configuration { get; set; }
		public Sql.QueryExtensionScope           Scope         { get; set; }
		public Dictionary<string,ISqlExpression> Arguments     { get; } = new();
		public Type?                             BuilderType   { get; set; }
		public SourceCardinality                 Cardinality   { get; set; }
		public object?                           Parameters    { get; set; }

		public ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext,ISqlExpression,ISqlExpression> func)
		{
			foreach (var argument in Arguments.ToList())
				Arguments[argument.Key] = argument.Value.Walk(options, context, func)!;

			return null;
		}
	}
}
