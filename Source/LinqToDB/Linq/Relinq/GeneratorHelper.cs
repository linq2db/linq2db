using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq.Relinq.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Relinq
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

		public static IEnumerable<MemberMappingInfo> GetMemberMapping(
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
							.Select((m, i) => new MemberMappingInfo(m, newExpression.Arguments[i]));
					}
				case ExpressionType.MemberInit:
					{
						var memberInitExpression = ((MemberInitExpression)expression);
						return memberInitExpression.Bindings
							.Where(b => b.BindingType == MemberBindingType.Assignment)
							.Select(b => new MemberMappingInfo(((MemberAssignment)b).Member, ((MemberAssignment)b).Expression));
					}
			}

			if (expression is UnifiedNewExpression newUnified)
			{
				return newUnified.Members;
			}

			if (!mappingSchema.IsScalarType(expression.Type))
			{
				var entityDescriptor = mappingSchema.GetEntityDescriptor(expression.Type);

				if (entityDescriptor.Columns.Count > 0)
					return entityDescriptor.Columns
						.Select(c => new MemberMappingInfo(c.MemberInfo, Expression.MakeMemberAccess(expression, c.MemberInfo)))
						.ToArray();				

				return expression.Type.GetProperties().Select(p =>
					new MemberMappingInfo(p, Expression.MakeMemberAccess(expression, p)));

			}

			return Enumerable.Empty<MemberMappingInfo>();
		}

		public static IEnumerable<Expression> GetGroupByKeys(
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
							.SelectMany((m, i) => GetGroupByKeys(newExpression.Arguments[i], mappingSchema));
					}
				case ExpressionType.MemberInit:
					{
						var memberInitExpression = ((MemberInitExpression)expression);
						return memberInitExpression.Bindings
							.Where(b => b.BindingType == MemberBindingType.Assignment)
							.SelectMany(b => GetGroupByKeys(((MemberAssignment)b).Expression, mappingSchema));
					}
			}

			if (expression is UnifiedNewExpression2 newUnified)
			{
				return newUnified.Members.SelectMany(m => GetGroupByKeys(m.Item2, mappingSchema));
			}

			if (!mappingSchema.IsScalarType(expression.Type))
			{
				var entityDescriptor = mappingSchema.GetEntityDescriptor(expression.Type);

				if (entityDescriptor.Columns.Count > 0)
					return entityDescriptor.Columns
						.Select(c => (Expression)Expression.MakeMemberAccess(expression, c.MemberInfo))
						.ToArray();				

				return expression.Type.GetProperties().Select(p =>
					Expression.MakeMemberAccess(expression, p));

			}

			return new[] { expression };
		}

		public static bool IsNullConstant(Expression expr)
		{
			return expr.NodeType == ExpressionType.Constant && ((ConstantExpression)expr).Value == null;
		}

		public static Expression RemoveNullPropagation(Expression expr)
		{
			if (expr == null)
				return null;

			switch (expr.NodeType)
			{
				case ExpressionType.Conditional:
					var conditional = (ConditionalExpression)expr;
					if (conditional.Test.NodeType == ExpressionType.NotEqual)
					{
						var binary = (BinaryExpression)conditional.Test;
						if (IsNullConstant(binary.Right))
						{
							if (IsNullConstant(conditional.IfFalse))
							{
								return conditional.IfTrue.Transform(e => RemoveNullPropagation(e));
							}
						}
					}
					else if (conditional.Test.NodeType == ExpressionType.Equal)
					{
						var binary = (BinaryExpression)conditional.Test;
						if (IsNullConstant(binary.Right))
						{
							if (IsNullConstant(conditional.IfTrue))
							{
								return conditional.IfFalse.Transform(e => RemoveNullPropagation(e));
							}
						}
					}
					break;
			}

			return expr;
		}

		
	}
}
