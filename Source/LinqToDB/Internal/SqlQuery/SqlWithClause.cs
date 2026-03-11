using System;
using System.Collections.Generic;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlWithClause : QueryElement
	{
		public override QueryElementType ElementType => QueryElementType.WithClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
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

					using (writer.IndentScope())
						writer.AppendElement(cte);

					if (cte.Fields.Count > 3)
					{
						writer.AppendLine();
						writer.AppendLine("(");

						using (writer.IndentScope())
						{
							var firstField = true;
							foreach (var field in cte.Fields)
							{
								if (!firstField)
									writer.AppendLine(",");
								firstField = false;
								writer.AppendElement(field);
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
							writer.AppendElement(field);
						}

						writer.AppendLine(")");
					}
					else
					{
						writer.Append(' ');
					}

					using (writer.IndentScope())
					{
						writer.AppendLine("AS");
						writer.AppendLine("(");

						using (writer.IndentScope())
						{
							writer.AppendElement(cte.Body!);
						}

						writer.AppendLine();
						writer.Append(')');
					}
					
				}

				writer.AppendLine();

			}

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);
			
			foreach (var clause in Clauses)
				hash.Add(clause.GetElementHashCode());

			return hash.ToHashCode();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlWithClause(this);

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
	}
}
