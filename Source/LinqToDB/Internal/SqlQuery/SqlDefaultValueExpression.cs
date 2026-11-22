using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// The default a reader substitutes for a NULL in a column that cannot be NULL as declared, read by a calculation of the
	/// projection. A provider writes it as the value, or as the least value of the column's type where the type cannot hold it.
	/// </summary>
	public sealed class SqlDefaultValueExpression : SqlExpressionBase
	{
		/// <summary>
		/// Creates the default of a column of type <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The column's type.</param>
		/// <param name="value">The default the reader substitutes for a NULL.</param>
		public SqlDefaultValueExpression(DbDataType type, object? value)
		{
			Type  = type;
			Value = value;
		}

		/// <summary>
		/// The column's type.
		/// </summary>
		public DbDataType Type  { get; }

		/// <summary>
		/// The default the reader substitutes for a NULL.
		/// </summary>
		public object?    Value { get; }

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.SqlDefaultValueExpression;
		public override int              Precedence  => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type?            SystemType  => Type.SystemType;

		public override bool CanBeNullable(NullabilityContext nullability) => false;

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			return
				other is SqlDefaultValueExpression defaultValue
				&& Type.Equals(defaultValue.Type)
				&& ((Value == null && defaultValue.Value == null) || (Value != null && Value.Equals(defaultValue.Value)))
				&& comparer(this, other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(
				Type,
				Value
			);
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				ElementType,
				Type,
				Value
			);
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.DebugAppendUniqueId(this);

			return writer
				.Append("DEFAULT(")
				.Append(Value)
				.Append(')');
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlDefaultValueExpression(this);

		#endregion
	}
}
