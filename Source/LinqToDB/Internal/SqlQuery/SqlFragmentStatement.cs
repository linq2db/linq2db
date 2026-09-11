using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// A statement that renders a single raw SQL expression (typically a <see cref="SqlFragment"/> built via
	/// <c>factory.Fragment</c>) as a full command — e.g. provider-specific auxiliary DDL (identity sequence/trigger
	/// creation, identity reset). The owning scenario step's <c>Kind</c> decides execution (non-query / scalar); the
	/// builder renders it generically.
	/// </summary>
	/// <remarks>
	/// Produced server-side during scenario render (never present in a client-serialized statement), so it is
	/// intentionally not handled by <c>LinqServiceSerializer</c> — remote serialization of it would throw.
	/// </remarks>
	public sealed class SqlFragmentStatement : SqlStatement
	{
		/// <summary>Creates a fragment statement that renders <paramref name="expression"/> as a full command.</summary>
		public SqlFragmentStatement(ISqlExpression expression)
		{
			Expression = expression;
		}

		/// <summary>The raw SQL expression rendered as the command body.</summary>
		public ISqlExpression Expression { get; set; }

		public override QueryType        QueryType   => QueryType.Fragment;
		public override QueryElementType ElementType => QueryElementType.SqlFragmentStatement;

		public override bool IsParameterDependent
		{
			get => false;
			set {}
		}

		public override SelectQuery? SelectQuery { get => null; set {} }

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			return writer.AppendElement(Expression).AppendLine();
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias)
		{
			noAlias = false;
			return null;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(base.GetElementHashCode(), Expression.GetElementHashCode());
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlFragmentStatement(this);
	}
}
