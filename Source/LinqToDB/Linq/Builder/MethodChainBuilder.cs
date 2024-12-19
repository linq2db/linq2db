using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqToDB.Linq.Builder
{
	using Common.Internal;
	using Extensions;
	using LinqToDB.Expressions;
	using LinqToDB.Expressions.Internal;

	[BuildsExpression(ExpressionType.Call)]
	sealed class MethodChainBuilder : MethodCallBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo buildInfo, ExpressionBuilder builder)
		{
			var methodCall = (MethodCallExpression)expr;

			var functions = Sql.ExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);
			if (functions.Length == 0)
				return false;

			var function = functions[0];
			if (function is { IsAggregate: false, IsWindowFunction: false })
				return false;

			if (typeof(Sql.IQueryableContainer).IsSameOrParentOf(methodCall.Method.ReturnType))
				return false;

			var root = methodCall.SkipMethodChain(builder.MappingSchema, out var isQueryable);

			root = builder.BuildRootExpression(root);

			if (root is ContextRefExpression)
				return true;

			if (isQueryable)
				return true;

			if (ReferenceEquals(root, methodCall))
				return false;

			if (builder.IsSequence(buildInfo.Parent, root))
				return true;

			return false;
		}

		public override bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var functions = Sql.ExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);

			var root = methodCall.SkipMethodChain(builder.MappingSchema, out _);

			// evaluating IQueryableContainer
			while (root.NodeType == ExpressionType.Constant && typeof(Sql.IQueryableContainer).IsSameOrParentOf(root.Type))
			{
				var evaluated = ((Sql.IQueryableContainer)root.EvaluateExpression()!).Query.Expression;
				methodCall = (MethodCallExpression)methodCall.Replace(root, evaluated);
				root       = evaluated.SkipMethodChain(builder.MappingSchema, out _);
			}

			IBuildContext? sequence;

			root = builder.ConvertExpressionTree(root);
			var rootContextref = builder.BuildRootExpression(root) as ContextRefExpression;

			var finalFunction = functions.First();

			if (rootContextref != null)
			{
				sequence = rootContextref.BuildContext;
			}
			else
			{
				var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, root) { CreateSubQuery = true });
				if (buildResult.BuildContext == null)
					return buildResult;
				sequence = buildResult.BuildContext;
			}

			var buildSequence        = sequence;
			var placeholderSelect    = sequence.SelectQuery;
			var placeholderSequence  = sequence;
			var inAggregationContext = true;

			if (!buildInfo.IsSubQuery && finalFunction.IsAggregate)
			{
				// Wrap by subquery to handle aggregate limitations, especially for SQL Server
				//
				sequence            = new SubQueryContext(sequence);
				placeholderSelect   = sequence.SelectQuery;
				placeholderSequence = sequence;

				rootContextref = new ContextRefExpression(root.Type, sequence);
				methodCall     = (MethodCallExpression)methodCall.Replace(root, rootContextref);
			}
			else
			{
				var rootContext = builder.GetRootContext(rootContextref, true);

				inAggregationContext = rootContext != null;

				if (!inAggregationContext)
				{
					rootContextref = new ContextRefExpression(root.Type, sequence);
					methodCall     = (MethodCallExpression)methodCall.Replace(root, rootContextref);
				}

				placeholderSequence = rootContext?.BuildContext ?? sequence;

				if (placeholderSequence is GroupByBuilder.GroupByContext groupCtx)
				{
					placeholderSequence = groupCtx.Element;
					placeholderSelect   = groupCtx.SubQuery.SelectQuery;

					methodCall = (MethodCallExpression)SequenceHelper.ReplaceContext(methodCall, groupCtx, placeholderSequence);
				}
			}

			var sqlExpression = finalFunction.GetExpression(
				(builder, context: placeholderSequence, forselect: placeholderSelect),
				builder.DataContext,
				builder,
				placeholderSelect,
				methodCall,
				static (ctx, e, descriptor, inline) =>
				{
					var result = ctx.builder.ConvertToExtensionSql(ctx.context, e, descriptor, inline);
					result = ctx.builder.UpdateNesting(ctx.forselect, result);
					return result;
				});

			if (sqlExpression is not SqlPlaceholderExpression placeholder)
				return BuildSequenceResult.Error(methodCall);

			builder.RegisterExtensionAccessors(methodCall);

			var context = new ChainContext(buildInfo.Parent, placeholderSequence, methodCall);

			placeholder = placeholder
					.WithPath(methodCall)
					.WithAlias(methodCall.Method.Name);

			if (!inAggregationContext && buildInfo.IsSubQuery)
			{
				var indexed = builder.ToColumns(sequence, placeholder);
				placeholder = ExpressionBuilder.CreatePlaceholder(buildInfo.Parent, sequence.SelectQuery, methodCall, alias: methodCall.Method.Name);
			}
			
			context.Placeholder = placeholder;

			return BuildSequenceResult.FromContext(context);
		}

		internal sealed class ChainContext : SequenceContextBase
		{
			public ChainContext(IBuildContext? parent, IBuildContext sequence, MethodCallExpression methodCall)
				: base(parent, sequence, null)
			{
				MethodCall = methodCall;
				_returnType     = methodCall.Method.ReturnType;
				_methodName     = methodCall.Method.Name;

				if (_returnType.IsGenericType && _returnType.GetGenericTypeDefinition() == typeof(Task<>))
				{
					_returnType = _returnType.GetGenericArguments()[0];
					_methodName = _methodName.Replace("Async", "");
				}
			}

			readonly string _methodName;
			readonly Type   _returnType;

			public SqlPlaceholderExpression Placeholder = null!;
			public MethodCallExpression     MethodCall { get; }

			// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
			static int CheckNullValue(bool isNull, object context)
			{
				if (isNull)
				{
					throw new InvalidOperationException(
						$"Function '{context}' returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
				}

				return 0;
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<object>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Root))
					return path;

				if (flags.IsAggregationRoot() || flags.IsAssociationRoot())
				{
					var corrected = SequenceHelper.CorrectExpression(path, this, Sequence);
					return corrected;
				}

				// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
				if (Placeholder == null)
					return path;

				Expression result = Placeholder;

				if (!_returnType.IsNullableType() && flags.IsExpression())
				{
					result = Expression.Block(
						Expression.Call(null, MemberHelper.MethodOf(() => CheckNullValue(false, null!)),
							new SqlReaderIsNullExpression(Placeholder, false), Expression.Constant(_methodName)),
						Placeholder);
				}

				return result;
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				return null;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new ChainContext(null, context.CloneContext(Sequence), context.CloneExpression(MethodCall))
				{
					Placeholder = context.CloneExpression(Placeholder)
				};
			}

			public override bool IsSingleElement => true;
		}

	}
}
