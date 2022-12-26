using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlOutputClause : IQueryElement, ISqlExpressionWalkable
	{
		private List<SqlSetExpression>? _outputItems;

		public SqlTable?             InsertedTable { get; set; }
		public SqlTable?             DeletedTable  { get; set; }
		public SqlTable?             OutputTable   { get; set; }
		public List<ISqlExpression>? OutputColumns { get; set; }

		public bool                   HasOutput      => HasOutputItems || OutputColumns != null;
		public bool                   HasOutputItems => _outputItems != null && _outputItems.Count > 0;
		public List<SqlSetExpression> OutputItems    => _outputItems ??= new List<SqlSetExpression>();

		#region Overrides

#if OVERRIDETOSTRING
		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}
#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression? ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext,ISqlExpression,ISqlExpression> func)
		{
			((ISqlExpressionWalkable?)DeletedTable )?.Walk(options, context, func);
			((ISqlExpressionWalkable?)InsertedTable)?.Walk(options, context, func);
			((ISqlExpressionWalkable?)OutputTable  )?.Walk(options, context, func);

			if (HasOutputItems)
				foreach (var t in OutputItems)
					((ISqlExpressionWalkable)t).Walk(options, context, func);

			if (OutputColumns != null)
			{
				for (var i = 0; i < OutputColumns.Count; i++)
				{
					OutputColumns[i] = OutputColumns[i].Walk(options, context, func) ?? throw new InvalidOperationException();
				}
			}

			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.OutputClause;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			var writer = new SqlTextWriter(sb);
			writer.AppendLine()
				.AppendLine("OUTPUT");

			if (HasOutput)
			{

				using (writer.WithScope())
				{
					var first = true;

					if (HasOutputItems)
					{
						foreach (var oi in OutputItems)
						{
							if (!first)
								writer.AppendLine(',');
							first = false;

							writer.Append(oi.Expression!, dic);
						}

						writer.AppendLine();
					}
				}


				if (OutputColumns != null)
				{
					using (writer.WithScope())
					{
						var first = true;

						foreach (var expr in OutputColumns)
						{
							if (!first)
								writer.AppendLine(',');

							first = false;

							writer.Append(expr, dic);
						}
					}

					writer.AppendLine();
				}

				if (OutputTable != null)
				{
					writer.Append("INTO ")
						.AppendLine(OutputTable.TableName.Name)
						.AppendLine('(');

					using (writer.WithScope())
					{
						var firstColumn = true;
						if (HasOutputItems)
						{
							foreach (var oi in OutputItems)
							{
								if (!firstColumn)
									writer.AppendLine(',');
								firstColumn = false;

								writer.Append(oi.Column, dic);
							}
						}

						writer.AppendLine();
					}

					writer.AppendLine(")");
				}
			}

			return sb;
		}

		#endregion
	}
}
