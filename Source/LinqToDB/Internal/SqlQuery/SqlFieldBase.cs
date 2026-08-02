using System;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Abstract base class for table-bound SQL field references: <see cref="SqlField"/> and <see cref="SqlCteTableField"/>.
	/// Contains common properties shared by field references that are resolved against an <see cref="ISqlTableSource"/>.
	/// </summary>
	public abstract class SqlFieldBase : SqlExpressionBase
	{
		public virtual DbDataType Type { get; set; }
		public virtual string     Name { get; set; } = null!;

		/// <summary>
		/// The <see cref="ISqlNamedTable"/> this field belongs to, if any.
		/// Returns <see langword="null"/> for fields bound to a non-named source (e.g. a <see cref="SelectQuery"/>).
		/// </summary>
		public abstract ISqlNamedTable? NamedTable { get; }

		public override Type? SystemType => Type.SystemType;
		public override int   Precedence => LinqToDB.SqlQuery.Precedence.Primary;

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return ReferenceEquals(this, other);
		}
	}
}
