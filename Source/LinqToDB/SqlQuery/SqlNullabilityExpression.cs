﻿using System;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using Mapping;

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

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

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

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
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
