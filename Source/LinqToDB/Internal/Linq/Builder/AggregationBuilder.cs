using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Average", "Min", "Max", "Sum", "Count", "LongCount")]
	[BuildsMethodCall("AverageAsync", "MinAsync", "MaxAsync", "SumAsync", "CountAsync", "LongCountAsync", 
		CanBuildName = nameof(CanBuildAsyncMethod))]
	sealed class AggregationBuilder : MethodCallBuilder
	{
		enum AggregationType
		{
			Count,
			Min,
			Max,
			Sum,
			Average,
			Custom
		}

		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable();

		public static bool CanBuildAsyncMethod(MethodCallExpression call)
			=> call.IsAsyncExtension();

		static Type ExtractTaskType(Type taskType)
		{
			return taskType.GetGenericArguments()[0];
		}

		public static Expression BuildAggregateExecuteExpression(MethodCallExpression methodCall)
		{
			if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));

			var elementType = TypeHelper.GetEnumerableElementType(methodCall.Arguments[0].Type);
			var sourceParam = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "source");
			var resultType  = methodCall.Type;

			Type[] typeArguments = methodCall.Method.IsGenericMethod
				? methodCall.Method.GetGenericArguments().Length == 2 ? [elementType, resultType] : [elementType]
				: [];

			var aggregationBody = Expression.Call(typeof(Enumerable), methodCall.Method.Name,
				typeArguments,
				[sourceParam, ..methodCall.Arguments.Skip(1).Select(a => a.Unwrap())]
			);

			var aggregationLambda = Expression.Lambda(aggregationBody, sourceParam);

			var executeExpression = Expression.Call(typeof(LinqExtensions), nameof(LinqExtensions.AggregateExecute), [elementType, resultType], methodCall.Arguments[0], aggregationLambda);

			return executeExpression;
		}

		static AggregationType GetAggregationType(MethodCallExpression methodCallExpression, out int argumentsCount, out string functionName, out Type returnType)
		{
			AggregationType aggregationType;
			argumentsCount = methodCallExpression.Arguments.Count;
			returnType     = methodCallExpression.Method.ReturnType;

			switch (methodCallExpression.Method.Name)
			{
				case "Count":
				case "LongCount":
				{
					aggregationType = AggregationType.Count;
					functionName    = "COUNT";
					break;
				}
				case "LongCountAsync":
				{
					--argumentsCount;
					returnType      = typeof(long);
					aggregationType = AggregationType.Count;
					functionName    = "COUNT";
					break;
				}
				case "CountAsync":
				{
					--argumentsCount;
					returnType      = typeof(int);
					aggregationType = AggregationType.Count;
					functionName    = "COUNT";
					break;
				}
				case "Min":
				{
					aggregationType = AggregationType.Min;
					functionName    = "MIN";
					break;
				}
				case "MinAsync":
				{
					--argumentsCount;
					returnType      = ExtractTaskType(returnType);
					aggregationType = AggregationType.Min;
					functionName    = "MIN";
					break;
				}
				case "Max":
				{
					aggregationType = AggregationType.Max;
					functionName    = "MAX";
					break;
				}
				case "MaxAsync":
				{
					--argumentsCount;
					returnType      = ExtractTaskType(returnType);
					aggregationType = AggregationType.Max;
					functionName    = "MAX";
					break;
				}
				case "Sum":
				{
					aggregationType = AggregationType.Sum;
					functionName    = "SUM";
					break;
				}
				case "SumAsync":
				{
					--argumentsCount;
					returnType      = ExtractTaskType(returnType);
					aggregationType = AggregationType.Sum;
					functionName    = "SUM";
					break;
				}
				case "Average":
				{
					aggregationType = AggregationType.Average;
					functionName    = "AVG";
					break;
				}
				case "AverageAsync":
				{
					--argumentsCount;
					returnType      = ExtractTaskType(returnType);
					aggregationType = AggregationType.Average;
					functionName    = "AVG";
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(methodCallExpression), methodCallExpression.Method.Name, "Invalid aggregation function");
			}

			return aggregationType;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var newMethodCall = methodCall;

			if (!buildInfo.IsSubQuery)
			{
				var aggregator = BuildAggregateExecuteExpression(newMethodCall);

				var result = builder.TryBuildSequence(new BuildInfo(buildInfo, aggregator));

				if (result.BuildContext == null)
					return result;

				AggregationType aggregationType = GetAggregationType(
					newMethodCall,
					out int argumentsCount,
					out string functionName,
					out Type returnType);

				var aggregationContext = new AggregationFinalizerContext(buildInfo.Parent, result.BuildContext!, aggregationType, functionName, returnType);

				return BuildSequenceResult.FromContext(aggregationContext);
			}

			return BuildSequenceResult.NotSupported();
		}

		sealed class AggregationFinalizerContext : SequenceContextBase
		{
			public AggregationFinalizerContext(
				IBuildContext?  parent,
				IBuildContext   sequence,
				AggregationType aggregationType,
				string          methodName,
				Type            returnType)
				: base(parent, sequence, null)
			{
				_returnType      = returnType;
				_aggregationType = aggregationType;
				_methodName      = methodName;
			}

			readonly AggregationType _aggregationType;
			readonly string          _methodName;
			readonly Type            _returnType;

			SqlPlaceholderExpression? Placeholder { get; set; }
			
			static TValue CheckNullValue<TValue>(TValue? maybeNull, string context)
				where TValue : struct
			{
				if (maybeNull is null)
					throw new InvalidOperationException(
						$"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
				return maybeNull.Value;
			}

			Expression GenerateNullCheckIfNeeded(Expression expression)
			{
				// in LINQ Min, Max, Avg aggregates throw exception on empty set(so Sum and Count are exceptions which return 0)
				if (
					_aggregationType != AggregationType.Sum
					&& _aggregationType != AggregationType.Count
					&& !expression.Type.IsNullableType()
					)
				{
					var checkExpression = expression;

					if (expression.Type.IsValueType && !expression.Type.IsNullable())
					{
						checkExpression = Expression.Convert(expression, expression.Type.AsNullable());
					}

					expression = Expression.Call(
						typeof(AggregationFinalizerContext),
						nameof(CheckNullValue),
						[_returnType],
						checkExpression,
						Expression.Constant(_methodName)
					);
				}

				return expression;
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				expr = GenerateNullCheckIfNeeded(expr);

				var mapper = Builder.BuildMapper<object>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Type ElementType => _returnType;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (!SequenceHelper.IsSameContext(path, this))
					return path;

				if (flags.HasFlag(ProjectFlags.Root))
					return path;

				if (Placeholder == null)
				{
					var sequenceRef = SequenceHelper.CreateRef(Sequence);

					var builtExpression = Builder.BuildSqlExpression(Parent, sequenceRef);
					if (builtExpression is SqlPlaceholderExpression placeholder)
						Placeholder = placeholder;
					else
						return path;
				}

				Expression result = Placeholder;

				// We do not need this check for UNION/UNION ALL queries
				if (flags.IsExpression() && !flags.IsForSetProjection())
					result = GenerateNullCheckIfNeeded(result);

				return result;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new AggregationFinalizerContext(null, context.CloneContext(Sequence), _aggregationType, _methodName, _returnType)
				{
					Placeholder = context.CloneExpression(Placeholder),
				};
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				return null;
			}

			public override bool IsSingleElement => true;
		}
	}
}
