#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;

	class LoadWithBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(Methods.LinqToDB.LoadWith, Methods.LinqToDB.ThenLoadSingle,
				Methods.LinqToDB.ThenLoadMultiple, 
				Methods.LinqToDB.LoadWithQueryMany,
				Methods.LinqToDB.LoadWithQuerySingle,
				Methods.LinqToDB.ThenLoadMultipleFunc);
		}

		TableBuilder.TableContext GetTableContext(IBuildContext ctx, BuildInfo buildInfo)
		{
			var table = ctx as TableBuilder.TableContext;
			if (table == null)
			{
				if (ctx is SelectContext selectContext)
				{
					var body = selectContext.Body.Unwrap();
					if (body != null)
					{
						ctx = selectContext.GetContext(body, 0, buildInfo);

						if (ctx != null)
							table = GetTableContext(ctx, buildInfo);
					}
				}
			}

			return table;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();

			var associations = GetAssociations(builder, selector.Body.Unwrap())
				.Reverse()
				.Select(m => Tuple.Create(m, (Expression)null))
				.ToArray();

			if (associations.Length == 0)
				throw new LinqToDBException($"Unable to retrieve properties path for LoadWith/ThenLoad. Path: '{selector.Body}'");

			var memberInfo = associations[0].Item1;
			var table = GetTableContext(sequence, buildInfo);
			if (table == null)
				throw new LinqToDBException(
					$"Unable to find table information for LoadWith. Consider moving LoadWith closer to GetTable<{memberInfo.DeclaringType.Name}>() method.");

			if (methodCall.IsSameGenericMethod(Methods.LinqToDB.ThenLoadSingle, Methods.LinqToDB.ThenLoadMultiple, Methods.LinqToDB.ThenLoadMultipleFunc))
			{
				if (!(table.LoadWith?.Count > 0))
					throw new LinqToDBException($"ThenLoad function should be followed after LoadWith. Can not find previous property for '{selector.Body}'.");

				var lastPath = table.LoadWith[table.LoadWith.Count - 1];
				associations = Array<Tuple<MemberInfo, Expression>>.Append(lastPath, associations);

				if (methodCall.Arguments.Count == 3)
				{
					var lastElement = associations[associations.Length - 1];
					associations[associations.Length - 1] = Tuple.Create(lastElement.Item1, methodCall.Arguments[2]);
				}

				// append to the last member chain
				table.LoadWith[table.LoadWith.Count - 1] = associations;
			}
			else
			{
				if (table.LoadWith == null)
					table.LoadWith = new List<Tuple<MemberInfo, Expression>[]>();

				if (methodCall.Arguments.Count == 3)
				{
					var lastElement = associations[associations.Length - 1];
					associations[associations.Length - 1] = Tuple.Create(lastElement.Item1, methodCall.Arguments[2]);
				}

				table.LoadWith.Add(associations);
			}
			return sequence;
		}

		static IEnumerable<MemberInfo> GetAssociations(ExpressionBuilder builder, Expression expression)
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
							var cexpr = (MethodCallExpression)expression;

							if (cexpr.Method.IsSqlPropertyMethodEx())
							{
								foreach (var assoc in GetAssociations(builder, builder.ConvertExpression(expression)))
									yield return assoc;

								yield break;
							}


							if (lastMember == null)
								goto default;
							
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

							if (lastMember.ReflectedType != mtype.GetItemType())
								goto default;

							expression = expr;

							break;
						}

					case ExpressionType.MemberAccess :
						{
							var mexpr  = (MemberExpression)expression;
							var member = lastMember = mexpr.Member;
							var attr   = builder.MappingSchema.GetAttribute<AssociationAttribute>(member.ReflectedType, member);

							if (attr == null)
								throw new LinqToDBException(
									string.Format("Member '{0}' is not an association.", expression));

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
							if (expression is GetItemExpression getItemExpression)
							{
								expression = getItemExpression.Expression;
								break;
							}

							goto default;
						}

					case ExpressionType.Convert      :
						{
							expression = ((UnaryExpression)expression).Operand;
							break;
						}

					default :
						{
							throw new LinqToDBException(
								string.Format("Expression '{0}' is not an association.", expression));
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
