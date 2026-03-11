using System;
using System.Collections.Generic;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlOutputClause : QueryElement
	{
		List<SqlSetExpression>? _outputItems;

		public SqlTable?             OutputTable   { get; set; }
		public List<ISqlExpression>? OutputColumns { get; set; }

		public bool                   HasOutput      => HasOutputItems || OutputColumns != null;
		public bool                   HasOutputItems => _outputItems                    != null && _outputItems.Count > 0;
		public List<SqlSetExpression> OutputItems
		{
			get => _outputItems ??= [];
			set => _outputItems = value;
		}

		public void Modify(SqlTable? outputTable)
		{
			OutputTable   = outputTable;
		}

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.OutputClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.AppendLine()
				.AppendLine("OUTPUT");

			if (HasOutput)
			{

				using (writer.IndentScope())
				{
					var first = true;

					if (HasOutputItems)
					{
						foreach (var oi in OutputItems)
						{
							if (!first)
								writer.AppendLine(',');
							first = false;

							writer.AppendElement(oi.Expression);
						}

						writer.AppendLine();
					}
				}

				if (OutputColumns != null)
				{
					using (writer.IndentScope())
					{
						var first = true;

						foreach (var expr in OutputColumns)
						{
							if (!first)
								writer.AppendLine(',');

							first = false;

							writer.AppendElement(expr);
						}
					}

					writer.AppendLine();
				}

				if (OutputTable != null)
				{
					writer.Append("INTO ")
						.AppendLine(OutputTable.TableName.Name)
						.AppendLine('(');

					using (writer.IndentScope())
					{
						var firstColumn = true;
						if (HasOutputItems)
						{
							foreach (var oi in OutputItems)
							{
								if (!firstColumn)
									writer.AppendLine(',');
								firstColumn = false;

								writer.AppendElement(oi.Column);
							}
						}

						writer.AppendLine();
					}

					writer.AppendLine(")");
				}
			}

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);
			hash.Add(OutputTable?.GetElementHashCode());

			if (OutputColumns != null)
			{
				foreach (var column in OutputColumns)
					hash.Add(column.GetElementHashCode());
			}

			if (HasOutputItems)
			{
				foreach (var item in OutputItems)
					hash.Add(item.GetElementHashCode());
			}

			return hash.ToHashCode();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlOutputClause(this);

		#endregion
	}
}
