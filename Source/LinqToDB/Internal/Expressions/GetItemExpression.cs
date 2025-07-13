﻿using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Expressions
{
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
