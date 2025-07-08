using System;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{

	public class SqlCastExpression : SqlExpressionBase
	{
		public SqlCastExpression(ISqlExpression expression, DbDataType toType, SqlDataType? fromType, bool isMandatory = false)
		{
			Expression  = expression;
			ToType      = toType;
			FromType    = fromType;
			IsMandatory = isMandatory;
		}

		public DbDataType     ToType    { get; private set; }
		public DbDataType     Type        => ToType;
		public ISqlExpression Expression  { get; private set; }
		public SqlDataType?   FromType    { get; private set; }
		public bool           IsMandatory { get; }

		public override int              Precedence  => SqlQuery.Precedence.Primary;
		public override Type             SystemType  => ToType.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlCast;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append("CAST(")
				.AppendElement(Expression)
				.Append(" AS ")
				.Append(ToType)
				.Append(")");

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ToType);
			hash.Add(Expression.GetElementHashCode());
			if (FromType != null)
				hash.Add(FromType.GetElementHashCode());
			hash.Add(IsMandatory);
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			if (!(other is SqlCastExpression otherCast))
				return false;

			return ToType.Equals(otherCast.ToType) && Expression.Equals(otherCast.Expression, comparer);
		}

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return Expression.CanBeNullable(nullability);
		}

		public SqlCastExpression MakeMandatory()
		{
			if (IsMandatory)
				return this;
			return new SqlCastExpression(Expression, ToType, FromType, true);
		}

		public SqlCastExpression WithExpression(ISqlExpression expression)
		{
			if (ReferenceEquals(expression, Expression))
				return this;
			return new SqlCastExpression(expression, ToType, FromType, IsMandatory);
		}

		public SqlCastExpression WithToType(DbDataType toType)
		{
			if (toType == ToType)
				return this;
			return new SqlCastExpression(Expression, toType, FromType, IsMandatory);
		}

		public void Modify(DbDataType toType, ISqlExpression expression, SqlDataType? fromType)
		{
			ToType     = toType;
			Expression = expression;
			FromType   = fromType;
		}
	}

}
