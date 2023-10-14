using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public class SqlUpdateStatement : SqlStatementWithQueryBase
	{
		public override QueryType QueryType          => QueryType.Update;
		public override QueryElementType ElementType => QueryElementType.UpdateStatement;

		public SqlOutputClause? Output { get; set; }

		private SqlUpdateClause? _update;

		public SqlUpdateClause Update
		{
			get => _update ??= new SqlUpdateClause();
			set => _update = value;
		}

		internal bool HasUpdate => _update != null;

		public SqlUpdateStatement(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.AppendTag(Tag)
				.AppendElement(With)
				.AppendLine("UPDATE")
				.AppendElement(Update)
				.AppendLine()
				.AppendElement(SelectQuery)
				.AppendElement(Output);

			return writer;
		}

		public override ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			With?.Walk(options, context, func);
			((ISqlExpressionWalkable?)_update)?.Walk(options, context, func);
			((ISqlExpressionWalkable?)Output)?.Walk(options, context, func);

			SelectQuery = (SelectQuery)SelectQuery.Walk(options, context, func);

			return base.Walk(options, context, func);
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias)
		{
			var result = SelectQuery.GetTableSource(table);
			noAlias = false;

			if (result != null)
				return result;

			if (ReferenceEquals(table, Update.Table))
			{
				noAlias = true;
				return table;
			}

			if (ReferenceEquals(Update.TableSource?.Source, table))
				return Update.TableSource;

			foreach (var item in Update.Items)
			{
				if (item.Expression is SelectQuery q)
				{
					result = q.GetTableSource(table);
					if (result != null)
						return result;
				}
			}

			return null;
		}

		public override bool IsDependedOn(SqlTable table)
		{
			// do not allow to optimize out Update table
			if (Update == null)
				return false;

			return null != Update.Find(table, static (table, e) =>
			{
				return e switch
				{
					SqlTable t => QueryHelper.IsEqualTables(t, table),
					SqlField f => QueryHelper.IsEqualTables(f.Table as SqlTable, table),
					_          => false,
				};
			});
		}

		public void AfterSetAliases()
		{
			if (Output?.OutputColumns != null)
			{
				var columnAliases = new Dictionary<string,string?>();

				foreach (var item in Update.Items)
				{
					switch (item.Column)
					{
						case SqlColumn { Expression : SqlField field } col :
							columnAliases.Add(field.PhysicalName, col.Alias);
							break;
						case SqlField field :
							columnAliases.Add(field.PhysicalName, field.Alias);
							break;
					}
				}

				foreach (var column in Output.OutputColumns)
				{
					switch (column)
					{
						case SqlColumn { Expression : SqlField field } col:
						{
							if (columnAliases.TryGetValue(field.Name, out var alias) && alias != null && alias != col.Alias)
								col.Alias = alias;
							break;
						}
						case SqlField field:
						{
							if (columnAliases.TryGetValue(field.Name, out var alias) && alias != null && alias != field.Alias)
								field.PhysicalName = alias;
							break;
						}
					}
				}
			}
		}
	}
}
