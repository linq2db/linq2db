using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using System;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Common;
	using Reflection;

	class LoadWithBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("LoadWith", "ThenLoad");
		}

		static void CheckFilterFunc(Type expectedType, Type filterType, MappingSchema mappingSchema)
		{
			var propType = expectedType;
			if (EagerLoading.IsEnumerableType(expectedType, mappingSchema))
				propType = EagerLoading.GetEnumerableElementType(expectedType, mappingSchema);
			var itemType = filterType.GetGenericArguments()[0].GetGenericArguments()[0];
			if (propType != itemType)
				throw new LinqException("Invalid filter function usage.");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var selector = (LambdaExpression)methodCall.Arguments[1].Unwrap();

			var associations = ExtractAssociations(builder, selector.Body)
				.Reverse()
				.ToArray();

			if (associations.Length == 0)
				throw new LinqToDBException($"Unable to retrieve properties path for LoadWith/ThenLoad. Path: '{selector.Body}'");

			var memberInfo = associations[0].MemberInfo;
			var table = GetTableContext(sequence);
			if (table == null)
				throw new LinqToDBException(
					$"Unable to find table information for LoadWith. Consider moving LoadWith closer to GetTable<{memberInfo.DeclaringType.Name}>() method.");

			if (methodCall.Method.Name == "ThenLoad")
			{
				if (!(table.LoadWith?.Count > 0))
					throw new LinqToDBException($"ThenLoad function should be followed after LoadWith. Can not find previous property for '{selector.Body}'.");

				var lastPath = table.LoadWith[table.LoadWith.Count - 1];
				associations = Array<LoadWithInfo>.Append(lastPath, associations);

				if (methodCall.Arguments.Count == 3)
				{
					var lastElement = associations[associations.Length - 1];
					lastElement.FilterFunc = (Expression?)methodCall.Arguments[2];
					CheckFilterFunc(lastElement.MemberInfo.GetMemberType(), lastElement.FilterFunc!.Type, builder.MappingSchema);
				}

				// append to the last member chain
				table.LoadWith[table.LoadWith.Count - 1] = associations;
			}
			else
			{
				if (table.LoadWith == null)
					table.LoadWith = new List<LoadWithInfo[]>();

				if (methodCall.Arguments.Count == 3)
				{
					var lastElement = associations[associations.Length - 1];
					lastElement.FilterFunc = (Expression?)methodCall.Arguments[2];
					CheckFilterFunc(lastElement.MemberInfo.GetMemberType(), lastElement.FilterFunc!.Type, builder.MappingSchema);
				}

				table.LoadWith.Add(associations);
			}
			return sequence;
		}

		TableBuilder.TableContext? GetTableContext(IBuildContext ctx)
		{
			var table = ctx as TableBuilder.TableContext;
			if (table == null)
			{
				var isTableResult = ctx.IsExpression(null, 0, RequestFor.Table);
				if (isTableResult.Result)
					table = isTableResult.Context as TableBuilder.TableContext;
			}

			return table;
		}

		static IEnumerable<LoadWithInfo> ExtractAssociations(ExpressionBuilder builder,
			Expression expression)
		{
			expression = expression.Unwrap();
			var currentExpression = expression;

			while (currentExpression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)currentExpression;
				if (mc.IsQueryable())
					currentExpression = mc.Arguments[0];
				else
					break;
			}

			LambdaExpression? filterExpression = null;
			if (currentExpression != expression)
			{
				var parameter  = Expression.Parameter(currentExpression.Type, "e");

				var body   = expression.Replace(currentExpression, parameter);
				var lambda = Expression.Lambda(body, parameter);

				filterExpression = lambda;
			}

			foreach (var member in GetAssociations(builder, currentExpression))
			{
				yield return new LoadWithInfo(member) { MemberFilter = filterExpression };
				filterExpression = null;
			}
		}

		static IEnumerable<MemberInfo> GetAssociations(ExpressionBuilder builder, Expression expression)
		{
			MemberInfo? lastMember = null;

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

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}
	}
}
