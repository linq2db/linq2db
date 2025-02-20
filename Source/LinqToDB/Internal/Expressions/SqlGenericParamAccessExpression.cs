using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Internal.Expressions
{
	public class SqlGenericParamAccessExpression : Expression, IEquatable<SqlGenericParamAccessExpression>
	{
		public Expression    Constructor   { get; }
		public ParameterInfo ParameterInfo { get; }
		public int           ParamIndex    => ParameterInfo.Position;
		public Type          ParamType     => ParameterInfo.ParameterType;

		public SqlGenericParamAccessExpression(Expression constructor, ParameterInfo parameterInfo)
		{
			Constructor        = constructor;
			ParameterInfo = parameterInfo;
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ParamType;

		public SqlGenericParamAccessExpression Update(Expression constructor)
		{
			if (ReferenceEquals(Constructor, constructor))
				return this;
			return new SqlGenericParamAccessExpression(constructor, ParameterInfo);
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

			return ExpressionEqualityComparer.Instance.Equals(Constructor, other.Constructor) && ParameterInfo.Equals(other.ParameterInfo);
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
				var hashCode = ExpressionEqualityComparer.Instance.GetHashCode(Constructor);
				hashCode = (hashCode * 397) ^ ParameterInfo.GetHashCode();
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

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlGenericParamAccessExpression(this);
			return base.Accept(visitor);
		}

	}
}
