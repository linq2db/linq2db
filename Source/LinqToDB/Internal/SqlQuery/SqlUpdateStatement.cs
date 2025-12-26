using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlUpdateStatement : SqlStatementWithQueryBase
	{
		public override QueryType QueryType          => QueryType.Update;
		public override QueryElementType ElementType => QueryElementType.UpdateStatement;

		public SqlOutputClause? Output { get; set; }

		public SqlUpdateClause Update
		{
			get => field ??= new();
			set;
		}

		public SqlUpdateStatement(SelectQuery? selectQuery) : base(selectQuery)
		{
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.AppendTag(Tag)
				.AppendElement(With)
				.AppendLine("UPDATE")
				.AppendElement(Update)
				.AppendLine("--- query ---")
				.AppendElement(SelectQuery)
				.AppendElement(Output);

			return writer;
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias)
		{
			var result = SelectQuery.GetTableSource(table);
			noAlias = false;

			if (result != null)
				return result;

			if (ReferenceEquals(Update.TableSource?.Source, table))
				return Update.TableSource;

			if (ReferenceEquals(table, Update.Table))
			{
				noAlias = true;
				return table;
			}

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

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				base.GetElementHashCode(),
				Output?.GetElementHashCode(),
				Update?.GetElementHashCode()
			);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlUpdateStatement(this);
	}
}
