using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public sealed class SqlValidateExpression : Expression
	{
		public SqlPlaceholderExpression                   SqlPlaceholder { get; }
		public Func<SqlPlaceholderExpression, Expression> Validator      { get; }

		public SqlValidateExpression(SqlPlaceholderExpression sqlPlaceholder, Func<SqlPlaceholderExpression, Expression> validator)
		{
			SqlPlaceholder = sqlPlaceholder;
			Validator      = validator;
		}

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override bool           CanReduce => true;
		public override Type           Type      => SqlPlaceholder.Type;

		public override Expression Reduce() => Validator(SqlPlaceholder);

		public SqlValidateExpression Update(Expression sqlPlaceholder)
		{
			if (ReferenceEquals(SqlPlaceholder, sqlPlaceholder))
			{
				return this;
			}

			return new SqlValidateExpression(SqlPlaceholder, Validator);
		}

		bool Equals(SqlValidateExpression other)
		{
			return SqlPlaceholder.Equals(other.SqlPlaceholder) && Validator.Equals(other.Validator);
		}

		public override bool Equals(object? obj)
		{
			return ReferenceEquals(this, obj) || obj is SqlValidateExpression other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(SqlPlaceholder, Validator);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlValidateExpression(this);
			return base.Accept(visitor);
		}

		public override string ToString()
		{
			return $"V({SqlPlaceholder})";
		}
	}
}
