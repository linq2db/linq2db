using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public class SqlQueryExtension : IQueryElement, ISqlExpressionWalkable
	{
		public SqlQueryExtension()
		{
		}

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

#if DEBUG
		public string           DebugText   => this.ToDebugString();
#endif

		public QueryElementType ElementType => QueryElementType.SqlQueryExtension;
		public QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			return writer.Append("extension");
		}
	}
}
