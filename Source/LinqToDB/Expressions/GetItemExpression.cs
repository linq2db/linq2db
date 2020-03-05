#nullable disable
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Extensions;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;

namespace LinqToDB.Expressions
{
	class GetItemExpression : Expression
	{
		public GetItemExpression(Expression expression, MappingSchema mappingSchema)
		{
			_expression = expression;
			_type       = EagerLoading.GetEnumerableElementType(expression.Type, mappingSchema);
		}

		readonly Expression _expression;
		readonly Type       _type;

		public          Expression     Expression => _expression;
		public override Type           Type       => _type;
		public override ExpressionType NodeType   => ExpressionType.Extension;
		public override bool           CanReduce  => true;

		public override Expression Reduce()
		{
			var mi = MemberHelper.MethodOf(() => Enumerable.First<string>(null));
			var gi = mi.GetGenericMethodDefinition().MakeGenericMethod(_type);

			return Call(null, gi, _expression);
		}
	}
}
