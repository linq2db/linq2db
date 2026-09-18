using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Column definition inside a <see cref="CteClause"/> (stored in <see cref="CteClause.Fields"/>).
	/// Represents CTE schema, not a table-bound field reference — runtime references to a CTE's
	/// columns go through <see cref="SqlCteTableField"/> instead.
	/// </summary>
	[DebuggerDisplay("CteField({Name}, {Type})")]
	public sealed class SqlCteField : SqlExpressionBase
	{
		public SqlCteField(DbDataType type, string? name)
		{
			Type = type;
			Name = name!;
		}

		public SqlCteField(SqlCteField field)
		{
			Type   = field.Type;
			Name   = field.Name;
			Column = field.Column;
		}

		public DbDataType Type { get; set; }
		public string     Name { get; set; } = null!;

		/// <summary>
		/// Direct reference to the corresponding column in <see cref="CteClause.Body"/>.<see cref="SelectQuery.Select"/>.<see cref="SqlSelectClause.Columns"/>.
		/// Can be null during recursive CTE construction when the body is not yet built.
		/// </summary>
		public SqlColumn? Column { get; set; }

		public override Type? SystemType => Type.SystemType;
		public override int   Precedence => LinqToDB.SqlQuery.Precedence.Primary;

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			=> ReferenceEquals(this, other);

		public override bool CanBeNullable(NullabilityContext nullability)
			=> Column?.CanBeNullable(nullability) ?? true;

		public override QueryElementType ElementType => QueryElementType.SqlCteField;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append("CteField(")
				.Append(Name)
				.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			// Identity-based hash to match reference-equality semantics in Equals.
			// Name is mutable (aliasing/unique-name passes update it), so it cannot
			// participate in the hash without violating the hash contract for instances
			// kept in hash-based collections via ISqlExpressionEqualityComparer.
			return RuntimeHelpers.GetHashCode(this);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlCteField(this);
	}
}
