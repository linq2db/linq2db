using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Linq.Builder;
	using Mapping;

	class SqlAdjustTypeExpression: Expression
	{
		readonly Type          _type;
		public   Expression    Expression    { get; }
		public   MappingSchema MappingSchema { get; }

		public SqlAdjustTypeExpression(Expression expression, Type type, MappingSchema mappingSchema)
		{
			_type         = type;
			Expression    = expression;
			MappingSchema = mappingSchema;
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

			return new SqlAdjustTypeExpression(expression, Type, MappingSchema);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlAdjustTypeExpression(this);
			return base.Accept(visitor);
		}

	}
}
