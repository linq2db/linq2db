using System;
using System.Diagnostics;
using System.Linq.Expressions;

using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Expressions
{
	sealed class SqlAdjustTypeExpression : Expression, IEquatable<SqlAdjustTypeExpression>
	{
		readonly Type          _type;
		public   Expression    Expression    { get; }
		public   MappingSchema MappingSchema { get; }
		public   bool          IsAssociation { get; }

		public SqlAdjustTypeExpression(Expression expression, Type type, MappingSchema mappingSchema)
		{
			_type         = type;
			Expression    = expression;
			MappingSchema = mappingSchema;
		}

		public static Expression AdjustType(Expression expression, Type type, MappingSchema mappingSchema)
		{
			if (expression is SqlAdjustTypeExpression adjust)
			{
				if (expression.Type == type)
					return expression;

				return AdjustType(adjust.Expression, type, mappingSchema);
			}

			return new SqlAdjustTypeExpression(expression, type, mappingSchema);
		}

		public override bool CanReduce => true;

		public override Expression Reduce()
		{
			return ExpressionBuilder.AdjustType(Expression, Type, MappingSchema);
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => _type;

		public override string ToString()
		{
			return $"AdjustType({Expression}, {Type.Name})";
		}

		public Expression Update(Expression expression)
		{
			if (ReferenceEquals(Expression, expression))
				return this;

			return AdjustType(expression, Type, MappingSchema);
		}

		public bool Equals(SqlAdjustTypeExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return _type.Equals(other._type) && ExpressionEqualityComparer.Instance.Equals(Expression, other.Expression);
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

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((SqlAdjustTypeExpression)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_type, ExpressionEqualityComparer.Instance.GetHashCode(Expression));
		}

		public static bool operator ==(SqlAdjustTypeExpression? left, SqlAdjustTypeExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SqlAdjustTypeExpression? left, SqlAdjustTypeExpression? right)
		{
			return !Equals(left, right);
		}

		[DebuggerStepThrough]
		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlAdjustTypeExpression(this);
			return base.Accept(visitor);
		}

	}
}
