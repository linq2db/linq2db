using System;
using System.Linq;

namespace LinqToDB.Internal.SqlQuery
{
	public class SqlNullabilityExpression : SqlExpressionBase
	{
		readonly bool           _isNullable;
		public   ISqlExpression SqlExpression { get; private set; }

		public SqlNullabilityExpression(ISqlExpression sqlExpression, bool isNullable)
		{
			SqlExpression = sqlExpression;
			_isNullable   = isNullable;
		}

		public static ISqlExpression ApplyNullability(ISqlExpression sqlExpression, NullabilityContext nullability)
		{
			return sqlExpression switch
			{
				SqlNullabilityExpression => sqlExpression,
				SqlSearchCondition       => sqlExpression,
				SqlRowExpression row     => new SqlRowExpression(row.Values.Select(v => ApplyNullability(v, nullability)).ToArray()),
				_ => new SqlNullabilityExpression(sqlExpression, nullability.CanBeNull(sqlExpression))
			};
		}

		public static ISqlExpression ApplyNullability(ISqlExpression sqlExpression, bool canBeNull)
		{
			switch (sqlExpression)
			{
				case SqlSearchCondition:
					return sqlExpression;
				case SqlRowExpression row:
					return new SqlRowExpression(row.Values.Select(v => ApplyNullability(v, canBeNull)).ToArray());

				case SqlNullabilityExpression nullabilityExpression
						when nullabilityExpression.CanBeNull == canBeNull:
					return nullabilityExpression;
				case SqlNullabilityExpression nullabilityExpression:
					return new SqlNullabilityExpression(nullabilityExpression.SqlExpression, canBeNull);
					
				default:
					return new SqlNullabilityExpression(sqlExpression, canBeNull);
			}
		}

		public void Modify(ISqlExpression sqlExpression)
		{
			SqlExpression = sqlExpression;
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (other is not SqlNullabilityExpression otherNullability
				|| CanBeNull != otherNullability.CanBeNull)
				return false;

			return SqlExpression.Equals(((SqlNullabilityExpression)other).SqlExpression, comparer);
		}

		public override bool CanBeNullable(NullabilityContext nullability) => nullability.CanBeNull(this);

		public          bool             CanBeNull   => _isNullable;
		public override int              Precedence  => SqlExpression.Precedence;
		public override Type?            SystemType  => SqlExpression.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlNullabilityExpression;

		public override int GetHashCode()
		{
			// ReSharper disable once NonReadonlyMemberInGetHashCode
			return SqlExpression.GetHashCode();
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				//.DebugAppendUniqueId(this)
				.Append("{")
				.AppendElement(SqlExpression)
				.Append("}");

			if (CanBeNull)
				writer.Append("?");

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);
			hash.Add(CanBeNull);
			hash.Add(SqlExpression.GetElementHashCode());

			return hash.ToHashCode();
		}
	}
}
