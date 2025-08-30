using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsExpression(ExpressionType.Constant, ExpressionType.Call, ExpressionType.MemberAccess, ExpressionType.NewArrayInit)]
	sealed class EnumerableBuilder : ISequenceBuilder
	{
		public static bool CanBuild(Expression expr, ExpressionBuilder builder)
		{
			if (expr.NodeType == ExpressionType.NewArrayInit)
				return true;

			if (!typeof(IEnumerable<>).IsSameOrParentOf(expr.Type))
				return false;

			if (typeof(IEnumerable<>).GetGenericType(expr.Type) is null)
				return false;

			return expr.NodeType switch
			{
				ExpressionType.MemberAccess => CanBuildMemberChain(((MemberExpression)expr).Expression),
				ExpressionType.Constant     => ((ConstantExpression)expr).Value is IEnumerable,
				ExpressionType.Call         => builder.CanBeEvaluatedOnClient(expr),
				_ => false,
			};

			static bool CanBuildMemberChain(Expression? expr)
			{
				while (expr is { NodeType: ExpressionType.MemberAccess })
					expr = ((MemberExpression)expr).Expression;
				
				return expr is null or { NodeType: ExpressionType.Constant };
			}
		}

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var collectionType = typeof(IEnumerable<>).GetGenericType(buildInfo.Expression.Type) ??
			                     throw new InvalidOperationException();

			if (buildInfo.Expression is NewArrayExpression)
			{
				if (buildInfo.Parent == null)
					return BuildSequenceResult.Error(buildInfo.Expression);

				var expressions = ((NewArrayExpression)buildInfo.Expression).Expressions.Select(e =>
						builder.UpdateNesting(buildInfo.Parent!, builder.BuildExtractExpression(buildInfo.Parent, e)))
					.ToArray();
				
				var dynamicContext = new EnumerableContextDynamic(
					builder.GetTranslationModifier(),
					buildInfo.Parent,
					builder,
					expressions,
					buildInfo.SelectQuery,
					collectionType.GetGenericArguments()[0]);

				return BuildSequenceResult.FromContext(dynamicContext);
			}

			if (builder.CanBeEvaluatedOnClient(buildInfo.Expression))
			{
				var param = builder.ParametersContext.BuildParameter(buildInfo.Parent, buildInfo.Expression, null,
					buildParameterType : ParametersContext.BuildParameterType.InPredicate);

				if (param != null)
				{
					var enumerableContext = new EnumerableContext(builder.GetTranslationModifier(), builder, param, buildInfo.SelectQuery, collectionType.GetGenericArguments()[0]);

					return BuildSequenceResult.FromContext(enumerableContext);
				}
			}

			return BuildSequenceResult.Error(buildInfo.Expression);
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
