using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;

	class LoadWithBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("LoadWith");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var table    = (TableBuilder.TableContext)sequence;
			var selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();

			if (table.LoadWith == null)
				table.LoadWith = new List<MemberInfo[]>();

			table.LoadWith.Add(GetAssosiations(builder, selector.Body.Unwrap()).Reverse().ToArray());

			return sequence;
		}

		static IEnumerable<MemberInfo> GetAssosiations(ExpressionBuilder builder, Expression expression)
		{
			MemberInfo lastMember = null;

			for (;;)
			{
				switch (expression.NodeType)
				{
					case ExpressionType.Parameter :
						if (lastMember == null)
							goto default;
						yield break;

					case ExpressionType.Call      :
						{
							if (lastMember == null)
								goto default;

							var cexpr = (MethodCallExpression)expression;
							var expr  = cexpr.Object;

							if (expr == null)
							{
								if (cexpr.Arguments.Count == 0)
									goto default;

								expr = cexpr.Arguments[0];
							}

							if (expr.NodeType != ExpressionType.MemberAccess)
								goto default;

							var member = ((MemberExpression)expr).Member;
							var mtype  = member.GetMemberType();

							if (lastMember.ReflectedTypeEx() != mtype.GetItemType())
								goto default;

							expression = expr;

							break;
						}

					case ExpressionType.MemberAccess :
						{
							var mexpr  = (MemberExpression)expression;
							var member = lastMember = mexpr.Member;
							var attr   = builder.MappingSchema.GetAttribute<AssociationAttribute>(member);

							if (attr == null)
								throw new LinqToDBException(
									string.Format("Member '{0}' is not an assosiation.", expression));

							yield return member;

							expression = mexpr.Expression;

							break;
						}

					case ExpressionType.ArrayIndex   :
						{
							expression = ((BinaryExpression)expression).Left;
							break;
						}

					case ExpressionType.Extension    :
						{
							if (expression is GetItemExpression)
							{
								expression = ((GetItemExpression)expression).Expression;
								break;
							}

							goto default;
						}

					default :
						{
							throw new LinqToDBException(
								string.Format("Expression '{0}' is not an assosiation.", expression));
						}
				}
			}
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}
	}
}
