using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlQueryExtension : IQueryElement, ISqlExpressionWalkable
	{
		public SqlQueryExtension()
		{
		}

		/// <summary>
		/// Gets optional configuration, to which extension should be applied.
		/// </summary>
		public string?                            Configuration      { get; init; }
		/// <summary>
		/// Gets extension apply scope/location.
		/// </summary>
		public Sql.QueryExtensionScope            Scope              { get; init; }
		/// <summary>
		/// Gets extension arguments.
		/// </summary>
		public required Dictionary<string,ISqlExpression>  Arguments { get; init; }
		/// <summary>
		/// Gets optional extension builder type. Must implement <see cref="ISqlQueryExtensionBuilder"/> or <see cref="ISqlTableExtensionBuilder"/> interface.
		/// </summary>
		public Type?                              BuilderType        { get; init; }

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
