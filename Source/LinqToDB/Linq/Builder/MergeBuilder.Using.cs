using LinqToDB.Extensions;
using LinqToDB.SqlQuery;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		internal class Using : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				if (methodCall.Method.IsGenericMethod)
				{
					var genericMethod = methodCall.Method.GetGenericMethodDefinition();
					return  LinqExtensions.UsingMethodInfo1 == genericMethod
						 || LinqExtensions.UsingMethodInfo2 == genericMethod;
				}

				return false;
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				if (LinqExtensions.UsingMethodInfo1 == methodCall.Method.GetGenericMethodDefinition())
				{
					var sourceContext      = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));
					var source             = new TableLikeQueryContext(sourceContext);
					mergeContext.Sequences = new IBuildContext[] { mergeContext.Sequence, source };
				}
				else
				{
					var enumerableBuildInfo = new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery());

					var type = FindType(builder, enumerableBuildInfo);
					var sourceContext = new EnumerableContext(builder, enumerableBuildInfo,
						enumerableBuildInfo.SelectQuery, type,
						builder.ConvertToSql(buildInfo.Parent, enumerableBuildInfo.Expression));

					var source = new TableLikeQueryContext(sourceContext);
					mergeContext.Sequences = new IBuildContext[] { mergeContext.Sequence, source };
				}

				return mergeContext;
			}

			static Type FindType(ExpressionBuilder builder, BuildInfo buildInfo)
			{
				var expression = buildInfo.Expression;

				switch (expression.NodeType)
				{
					case ExpressionType.Constant:
						{
							var c = (ConstantExpression)expression;

							var type = c.Value.GetType();

							if (typeof(EnumerableQuery<>).IsSameOrParentOf(type))
							{
								// Avoiding collision with TableBuilder
								var elementType = type.GetGenericArguments(typeof(EnumerableQuery<>))![0];
								if (!builder.MappingSchema.IsScalarType(elementType))
									break;

								return elementType;
							}

							if (typeof(IEnumerable<>).IsSameOrParentOf(type))
								return type.GetGenericArguments(typeof(IEnumerable<>))![0];

							if (typeof(Array).IsSameOrParentOf(type))
								return type.GetElementType()!;

							break;
						}

					case ExpressionType.NewArrayInit:
						{
							var newArray = (NewArrayExpression)expression;
							if (newArray.Expressions.Count > 0)
								return newArray.Expressions[0].Type;
							break;
						}

					case ExpressionType.Parameter:
						{
							break;
						}
				}

				throw new InvalidOperationException();
			}

			protected override SequenceConvertInfo? Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
			{
				return null;
			}
		}
	}
}
