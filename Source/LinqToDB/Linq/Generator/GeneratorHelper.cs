using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Generator
{
	public static class GeneratorHelper
	{
		public class MemberMapping
		{
			private bool _isFullEntity;

			private MemberInfo[] _memberInfos;

			public MemberMapping(Expression selector)
			{
				switch (selector.NodeType)
				{
					case ExpressionType.New:
						{
							var newExpression = (NewExpression)selector;
							var zz = newExpression.Members
								.Select((m, i) => Tuple.Create(m, newExpression.Arguments[i]));
							break;
						}
					case ExpressionType.MemberInit:
						{
							var newExpression = ((MemberInitExpression)selector).NewExpression;
							var bb = newExpression.Members
								.Select((m, i) => Tuple.Create(m, newExpression.Arguments[i]));
							break;
						}
				}				
			}

//			public MemberInfo MapMember(MemberInfo member)
//			{
//
//			}
		}

		public static IEnumerable<Tuple<MemberInfo, Expression>> GetMemberMapping(
			[JetBrains.Annotations.NotNull] Expression expression,
			[JetBrains.Annotations.NotNull] MappingSchema mappingSchema)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));

			switch (expression.NodeType)
			{
				case ExpressionType.New:
					{
						var newExpression = (NewExpression)expression;
						return newExpression.Members
							.Select((m, i) => Tuple.Create(m, newExpression.Arguments[i]));
					}
				case ExpressionType.MemberInit:
					{
						var newExpression = ((MemberInitExpression)expression).NewExpression;
						return newExpression.Members
							.Select((m, i) => Tuple.Create(m, newExpression.Arguments[i]));
					}
			}

			if (expression.Type.IsClassEx())
			{
				var entityDescriptor = mappingSchema.GetEntityDescriptor(expression.Type);

				return entityDescriptor.Columns
					.Select(c => Tuple.Create(c.MemberInfo, (Expression)Expression.MakeMemberAccess(expression, c.MemberInfo)))
					.ToArray();				
			}

			return Enumerable.Empty<Tuple<MemberInfo, Expression>>();
		}
		
	}
}
