using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public class SqlGenericParamAccessExpression : Expression, IEquatable<SqlGenericParamAccessExpression>
	{
		public Expression Constructor { get; }
		public int        ParamIndex  { get; }
		public Type       ParamType   { get; }

		public SqlGenericParamAccessExpression(Expression constructor, int paramIndex, Type paramType)
		{
			Constructor = constructor;
			ParamIndex  = paramIndex;
			ParamType   = paramType;
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ParamType;

		public SqlGenericParamAccessExpression Update(Expression constructor)
		{
			if (ReferenceEquals(Constructor, constructor))
				return this;
			return new SqlGenericParamAccessExpression(constructor, ParamIndex, ParamType);
		}

		public override string ToString()
		{
			return $"{Constructor}[[{ParamIndex}]]";
		}

		public bool Equals(SqlGenericParamAccessExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Constructor.Equals(other.Constructor) && ParamIndex == other.ParamIndex && ParamType.Equals(other.ParamType);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((SqlGenericParamAccessExpression)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Constructor.GetHashCode();
				hashCode = (hashCode * 397) ^ ParamIndex;
				hashCode = (hashCode * 397) ^ ParamType.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(SqlGenericParamAccessExpression? left, SqlGenericParamAccessExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SqlGenericParamAccessExpression? left, SqlGenericParamAccessExpression? right)
		{
			return !Equals(left, right);
		}
	}
}
