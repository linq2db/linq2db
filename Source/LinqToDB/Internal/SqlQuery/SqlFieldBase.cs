using System;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Abstract base class for all SQL field types: <see cref="SqlField"/>, <see cref="SqlCteField"/>, and <see cref="SqlCteTableField"/>.
	/// Contains common properties shared across all field kinds.
	/// </summary>
	public abstract class SqlFieldBase : SqlExpressionBase
	{
		public virtual DbDataType Type { get; set; }
		public virtual string     Name { get; set; } = null!;

		public override Type? SystemType => Type.SystemType;
		public override int   Precedence => LinqToDB.SqlQuery.Precedence.Primary;

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return ReferenceEquals(this, other);
		}
	}
}
