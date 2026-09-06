using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// A database object name (table / sequence / trigger / field) that renders with a specific
	/// <see cref="ConvertType"/>. Lets a <see cref="SqlFragment"/> — and thus a <see cref="SqlFragmentStatement"/> —
	/// carry a correctly-quoted derived name (e.g. an identity sequence <c>SIDENTITY_&lt;table&gt;</c>) whose quoting
	/// is only available in the SQL builder.
	/// </summary>
	/// <remarks>
	/// Produced server-side during scenario render (never present in a client-serialized statement), so it is
	/// intentionally not handled by <c>LinqServiceSerializer</c> — remote serialization of it would throw.
	/// </remarks>
	public sealed class SqlObjectNameExpression : SqlExpressionBase
	{
		/// <summary>Creates an object-name expression for <paramref name="name"/> rendered with <paramref name="convertType"/>.</summary>
		public SqlObjectNameExpression(SqlObjectName name, ConvertType convertType, TableOptions tableOptions = TableOptions.NotSet, bool insideStringLiteral = false)
		{
			Name                = name;
			ConvertType         = convertType;
			TableOptions        = tableOptions;
			InsideStringLiteral = insideStringLiteral;
		}

		/// <summary>The database object name to render.</summary>
		public SqlObjectName Name         { get; }
		/// <summary>How the builder should quote/convert <see cref="Name"/> (table, sequence, trigger, …).</summary>
		public ConvertType   ConvertType  { get; }
		/// <summary>Table options influencing the rendered name (e.g. temporary-table qualification).</summary>
		public TableOptions  TableOptions { get; }

		/// <summary>
		/// Whether the fragment holding this placeholder puts it inside a SQL string literal, as Oracle's
		/// <c>EXECUTE IMMEDIATE '…'</c> blocks do. Identifier quoting alone does not survive that position: an
		/// apostrophe in the name would close the enclosing literal and the rest would be parsed as code, so the
		/// builder additionally escapes the rendered name for literal-body position. The literal's own delimiters
		/// stay in the fragment's format string - only the body is escaped.
		/// </summary>
		public bool          InsideStringLiteral { get; }

		public override QueryElementType ElementType => QueryElementType.SqlObjectNameExpression;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.DebugAppendUniqueId(this);
			return writer.Append(Name.Name);
		}

		public override string ToString() => this.ToDebugString();

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Name);
			hash.Add(ConvertType);
			hash.Add(TableOptions);
			hash.Add(InsideStringLiteral);
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			return other is SqlObjectNameExpression expr
				&& Name.Equals(expr.Name)
				&& ConvertType == expr.ConvertType
				&& TableOptions == expr.TableOptions
				&& InsideStringLiteral == expr.InsideStringLiteral
				&& comparer(this, expr);
		}

		public override bool CanBeNullable(NullabilityContext nullability) => false;

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlObjectNameExpression(this);

		public override int   Precedence => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type? SystemType => null;
	}
}
