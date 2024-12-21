using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public class SqlOutputClause : IQueryElement
	{
		List<SqlSetExpression>? _outputItems;

		public SqlTable?             OutputTable   { get; set; }
		public List<ISqlExpression>? OutputColumns { get; set; }

		public bool                   HasOutput      => HasOutputItems || OutputColumns != null;
		public bool                   HasOutputItems => _outputItems                    != null && _outputItems.Count > 0;
		public List<SqlSetExpression> OutputItems
		{
			get => _outputItems ??= new List<SqlSetExpression>();
			set => _outputItems = value;
		}

		public void Modify(SqlTable? outputTable)
		{
			OutputTable   = outputTable;
		}

		#region Overrides

#if OVERRIDETOSTRING
		public override string ToString()
		{
			return this.ToDebugString();
		}
#endif

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.OutputClause;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
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

		#endregion
	}
}
