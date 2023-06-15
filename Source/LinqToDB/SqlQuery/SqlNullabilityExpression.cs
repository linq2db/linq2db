using System;

namespace LinqToDB.SqlQuery
{
	public class SqlNullabilityExpression : ISqlExpression
	{
		readonly bool           _isNullable;
		public           ISqlExpression SqlExpression { get; private set; }

		public SqlNullabilityExpression(ISqlExpression sqlExpression, bool isNullable)
		{
			SqlExpression = sqlExpression;
			_isNullable   = isNullable;
		}

		public static ISqlExpression ApplyNullability(ISqlExpression sqlExpression, NullabilityContext nullability)
		{
			if (sqlExpression is SqlNullabilityExpression)
				return sqlExpression;

			return new SqlNullabilityExpression(sqlExpression, nullability.CanBeNull(sqlExpression));
		}

		public static ISqlExpression ApplyNullability(ISqlExpression sqlExpression, bool canBeNull)
		{
			if (sqlExpression is SqlNullabilityExpression nullabilityExpression)
			{
				if (nullabilityExpression.CanBeNull == canBeNull)
					return nullabilityExpression;

				return new SqlNullabilityExpression(nullabilityExpression.SqlExpression, canBeNull);
			}

			return new SqlNullabilityExpression(sqlExpression, canBeNull);
		}

		public void Modify(ISqlExpression sqlExpression)
		{
			SqlExpression = sqlExpression;
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			SqlExpression = SqlExpression.Walk(options, context, func) ?? throw new InvalidOperationException();
			return func(context, this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		internal static Func<ISqlExpression,ISqlExpression,bool> DefaultComparer = (x, y) => true;

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			if (other == null)
				return false;

			return SqlExpression.Equals(other, DefaultComparer);
		}

		#endregion

		#region ISqlExpression Members

		public bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (ElementType != other.ElementType)
				return false;

			return SqlExpression.Equals(((SqlNullabilityExpression)other).SqlExpression, comparer);
		}

		public bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public bool  CanBeNull  => _isNullable;
		public int   Precedence => SqlExpression.Precedence;
		public Type? SystemType => SqlExpression.SystemType;

		public override int GetHashCode()
		{
			return SqlExpression.GetHashCode();
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SqlNullabilityExpression;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer
				.Append('(')
				.AppendElement(SqlExpression)
				.Append(")");

			if (CanBeNull)
				writer.Append("?");

			return writer;
		}

		#endregion

	}
}
