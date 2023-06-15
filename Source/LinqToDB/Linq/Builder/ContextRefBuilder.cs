using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using LinqToDB.Extensions;

	sealed class ContextRefBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		static ProjectFlags GetRootProjectFlags(BuildInfo buildInfo)
		{
			var flags = buildInfo.IsAggregation ? ProjectFlags.AggregationRoot : ProjectFlags.Root;
			
			/*
			if (buildInfo.CreateSubQuery)
				flags |= ProjectFlags.Subquery;

			*/
			return buildInfo.GetFlags(flags);
		}

		Expression CalcBuildContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			if (buildInfo.Parent == null || buildInfo.Expression is ContextRefExpression)
				return buildInfo.Expression;

			var root = builder.MakeExpression(buildInfo.Parent, buildInfo.Expression, GetRootProjectFlags(buildInfo));
			if (ExpressionEqualityComparer.Instance.Equals(root, buildInfo.Expression))
			{
				if (root is ContextRefExpression)
					return root;

				var newExpression = builder.MakeExpression(buildInfo.Parent, root, buildInfo.CreateSubQuery ? ProjectFlags.Subquery : ProjectFlags.Expand);
				newExpression = builder.RemoveNullPropagation(newExpression, true);

				return newExpression;
			}

			return root;
		}

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = CalcBuildContext(builder, new BuildInfo(buildInfo, buildInfo.Expression) {IsTest = true});

			if (!ReferenceEquals(root, buildInfo.Expression))
				return builder.IsSequence(new BuildInfo(buildInfo, root) {IsTest = true});

			if (root is not ContextRefExpression contextRef)
				return false;

			var enumerableType = typeof(IEnumerable<>).GetGenericType(contextRef.Type);
			if (enumerableType == null)
				return false;

			if (!contextRef.Type.IsEnumerableType(contextRef.BuildContext.ElementType))
				return false;

			return true;
		}

		public IBuildContext? BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = CalcBuildContext(builder, buildInfo);

			if (root is not ContextRefExpression contextRef)
				return builder.TryBuildSequence(new BuildInfo(buildInfo, root));

			var context = contextRef.BuildContext;

			if (!buildInfo.CreateSubQuery)
				return context;

			var elementContext = context.GetContext(buildInfo.Expression, buildInfo);
			if (elementContext != null)
				return elementContext;

			return context;
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = CalcBuildContext(builder, buildInfo);

			if (root is not ContextRefExpression contextRef)
				return builder.IsSequence(new BuildInfo(buildInfo, root));

			return contextRef.BuildContext.GetContext(buildInfo.Expression, buildInfo) != null;
		}

		public Expression Expand(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			throw new NotImplementedException();
		}
	}
}
