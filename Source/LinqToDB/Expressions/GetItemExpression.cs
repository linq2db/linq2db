using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Linq.Builder;
	using Mapping;
	using Reflection;

	sealed class GetItemExpression : Expression
	{
		public GetItemExpression(Expression expression, MappingSchema mappingSchema)
		{
			Expression = expression;
			_type       = EagerLoading.GetEnumerableElementType(expression.Type, mappingSchema);
		}

		readonly Type       _type;

		public          Expression     Expression { get; }
		public override Type           Type       => _type;
		public override ExpressionType NodeType   => ExpressionType.Extension;
		public override bool           CanReduce  => true;

		public override Expression Reduce()
		{
			var gi = Methods.Enumerable.First.MakeGenericMethod(_type);

			return Call(null, gi, Expression);
		}
	}
}
