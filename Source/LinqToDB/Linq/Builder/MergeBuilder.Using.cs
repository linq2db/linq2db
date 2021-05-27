using LinqToDB.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal class Using : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = {UsingMethodInfo1, UsingMethodInfo2};

			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsSameGenericMethod(_supportedMethods);
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				if (UsingMethodInfo1 == methodCall.Method.GetGenericMethodDefinition())
				{
					var sourceContext         = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));
					var source                = new TableLikeQueryContext(sourceContext);
					mergeContext.Sequences    = new IBuildContext[] { mergeContext.Sequence, source };
					mergeContext.Merge.Source = source.Source;
				}
				else
				{
					var enumerableBuildInfo = new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery());

					var type = FindType(builder, enumerableBuildInfo);
					var sourceContext = new EnumerableContext(builder, enumerableBuildInfo,
						enumerableBuildInfo.SelectQuery, type,
						builder.ConvertToSql(buildInfo.Parent, enumerableBuildInfo.Expression));

					var source                = new TableLikeQueryContext(sourceContext);
					mergeContext.Sequences    = new IBuildContext[] { mergeContext.Sequence, source };
					mergeContext.Merge.Source = source.Source;
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
