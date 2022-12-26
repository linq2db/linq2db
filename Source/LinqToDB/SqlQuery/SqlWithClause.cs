using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public class SqlWithClause : IQueryElement, ISqlExpressionWalkable
	{
		public QueryElementType ElementType => QueryElementType.WithClause;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			var writer = new SqlTextWriter(sb);
			if (Clauses.Count > 0)
			{
				var first = true;

				foreach (var cte in Clauses)
				{
					if (first)
					{
						writer.Append("WITH ");

						first = false;
					}
					else
					{
						writer.Append(',').AppendLine();
					}

					using (writer.WithScope())
						writer.Append(cte, dic);

					if (cte.Fields.Count > 3)
					{
						writer.AppendLine();
						writer.AppendLine("(");

						using (writer.WithScope())
						{
							var firstField = true;
							foreach (var field in cte.Fields)
							{
								if (!firstField)
									writer.AppendLine(",");
								firstField = false;
								writer.Append(field, dic);
							}
						}

						writer.AppendLine();
						writer.AppendLine(")");
					}
					else if (cte.Fields.Count > 0)
					{
						writer.Append(" (");

						var firstField = true;
						foreach (var field in cte.Fields)
						{
							if (!firstField)
								writer.Append(", ");
							firstField = false;
							writer.Append(field, dic);
						}
						writer.AppendLine(")");
					}
					else
					{
						writer.Append(' ');
					}

					writer.AppendLine("AS");
					writer.AppendLine("(");

					using (writer.WithScope())
					{
						writer.Append(cte.Body!, dic);
					}

					writer.AppendLine();
					writer.Append(')');
				}

				writer.AppendLine();

			}

			writer.AppendLine("--------");

			return sb;
		}

		public List<CteClause> Clauses { get; set; } = new List<CteClause>();

		public ISqlTableSource? GetTableSource(ISqlTableSource table)
		{
			foreach (var cte in Clauses)
			{
				var ts = cte.Body!.GetTableSource(table);
				if (ts != null)
					return ts;
			}

			return null;
		}

		public void WalkQueries<TContext>(TContext context, Func<TContext, SelectQuery, SelectQuery> func)
		{
			foreach (var c in Clauses)
			{
				if (c.Body != null)
					c.Body = func(context, c.Body);
			}
		}

		public ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			for (var index = 0; index < Clauses.Count; index++)
				Clauses[index].Walk(options, context, func);

			return null;
		}
	}
}
