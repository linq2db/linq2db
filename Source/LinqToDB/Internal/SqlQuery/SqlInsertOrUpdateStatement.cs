using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlInsertOrUpdateStatement: SqlStatementWithQueryBase
	{
		public override QueryType QueryType          => QueryType.InsertOrUpdate;
		public override QueryElementType ElementType => QueryElementType.InsertOrUpdateStatement;

		private SqlInsertClause? _insert;
		public  SqlInsertClause   Insert
		{
			get => _insert ??= new SqlInsertClause();
			set => _insert = value;
		}

		private SqlUpdateClause? _update;
		public  SqlUpdateClause   Update
		{
			get => _update ??= new SqlUpdateClause();
			set => _update = value;
		}

		/// <summary>
		/// Optional predicate attached to the UPDATE branch of an upsert (e.g. the
		/// <c>WHERE</c> on <c>ON CONFLICT ... DO UPDATE SET ... WHERE &lt;cond&gt;</c> in
		/// PostgreSQL / SQLite, or the <c>WHEN MATCHED AND &lt;cond&gt;</c> in a
		/// MERGE-based emitter). <see langword="null"/> when the upsert has no conditional
		/// update gate. Populated by <c>UpsertBuilder</c> from
		/// <c>.Update(v =&gt; v.When(...))</c>.
		/// </summary>
		public SqlSearchCondition? UpdateWhere { get; set; }

		public SqlInsertOrUpdateStatement(SelectQuery? selectQuery) : base(selectQuery)
		{
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.AppendLine("/* insert or update */")
				.AppendElement(Insert)
				.AppendElement(Update)
				.AppendLine("--- query ---")
				.AppendElement(SelectQuery);

			return writer;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				base.GetElementHashCode(),
				_insert?.GetElementHashCode(),
				_update?.GetElementHashCode(),
				UpdateWhere?.GetElementHashCode()
			);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlInsertOrUpdateStatement(this);

		public override ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias)
		{
			if (Equals(_update?.Table, table))
			{
				noAlias = true;
				return table;
			}

			noAlias = false;
			if (Equals(_insert?.Into, table))
				return table;

			return SelectQuery!.GetTableSource(table);
		}
	}
}
