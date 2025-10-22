using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.AggregateExecute))]
	sealed class AggregateExecuteBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceExpression = methodCall.Arguments[0];
			var sequenceArgument = builder.BuildAggregationRootExpression(sequenceExpression);

			IBuildContext? sequence            = null;
			IBuildContext? placeholderSequence = null;
			var            isSimple            = false;

			if (buildInfo.IsSubQuery)
			{
				var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, sequenceArgument) { IsAggregation = true, CreateSubQuery = false });

				if (buildResult.BuildContext == null)
					return buildResult;

				sequence            = buildResult.BuildContext;
				placeholderSequence = sequence;

				var aggregationExpr = builder.BuildTraverseExpression(SequenceHelper.CreateRef(sequence));

				if (aggregationExpr is ContextRefExpression { BuildContext: GroupByBuilder.GroupByContext groupByContext })
				{
					isSimple            = true;
					placeholderSequence = groupByContext.SubQuery;
				}
				else
				{
					sequence = null;
				}
			}

			if (sequence == null)
			{
				var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, sequenceArgument, new SelectQuery()) { CreateSubQuery = true });

				if (buildResult.BuildContext == null)
					return buildResult;

				sequence = new SubQueryContext(buildResult.BuildContext);
				sequence = new AggregateRootContext(sequence, sequenceExpression);
				placeholderSequence = sequence;
			}

			var lambda = methodCall.Arguments[1].UnwrapLambda();

			var aggregateBody = SequenceHelper.PrepareBody(lambda, sequence);

			var translated = builder.BuildSqlExpression(placeholderSequence, aggregateBody, BuildPurpose.Sql, BuildFlags.None);

			if (translated is MarkerExpression { MarkerType: MarkerType.AggregationFallback } marker)
			{
				// Fallback chaining to allow other builders to process the method call
				return builder.TryBuildSequence(new BuildInfo(buildInfo, marker.InnerExpression));
			}

			if (translated is not SqlPlaceholderExpression placeholder)
			{
				if (translated is SqlErrorExpression)
					return BuildSequenceResult.Error(translated);
				return BuildSequenceResult.Error(methodCall);
			}

			var context = new AggregateExecuteContext(sequenceExpression, sequence, lambda.Body.Type)
			{
				Placeholder = placeholder,
				OuterJoinParentQuery = isSimple ? null : buildInfo.Parent?.SelectQuery
			};

			return BuildSequenceResult.FromContext(context);
		}

		public sealed class AggregateRootContext : PassThroughContext
		{
			public Expression SequenceExpression { get; }

			public AggregateRootContext(IBuildContext context, Expression sequenceExpression) : base(context)
			{
				SequenceExpression = sequenceExpression;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new AggregateRootContext(context.CloneContext(Context), context.CloneExpression(SequenceExpression));
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsAggregationRoot() || flags.IsRoot() || flags.IsTraverse() || flags.IsSubquery())
					return path;

				return base.MakeExpression(path, flags);
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				if (!buildInfo.IsSubQuery)
					return this;

				if (buildInfo.IsAggregation && !buildInfo.CreateSubQuery)
					return this;

				if (!SequenceHelper.IsSameContext(expression, this))
					return null;

				var expr = SequenceExpression;

				var parentContext = buildInfo.Parent ?? this;

				expr = Builder.UpdateNesting(parentContext, expr);

				var buildResult = Builder.TryBuildSequence(new BuildInfo(buildInfo, expr) { IsAggregation = false, CreateSubQuery = false});

				return buildResult.BuildContext;
			}
		}

		sealed class AggregateExecuteContext : PassThroughContext
		{
			public Expression SequenceExpression { get; }

			public AggregateExecuteContext(
				Expression    sequenceExpression,
				IBuildContext sequence,
				Type          returnType)
				: base(sequence, sequence.SelectQuery)
			{
				SequenceExpression = sequenceExpression;
				_returnType        = returnType;
			}

			readonly Type _returnType;

			public SqlPlaceholderExpression Placeholder = null!;
			public SelectQuery? OuterJoinParentQuery { get; set; }

			SqlJoinedTable? _joinedTable;

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<object>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Type ElementType => _returnType;

			void CreateWeakOuterJoin(SelectQuery parentQuery, SelectQuery selectQuery)
			{
				if (_joinedTable == null)
				{
					var join = selectQuery.OuterApply();
					join.JoinedTable.IsWeak = true;

					_joinedTable = join.JoinedTable;

					parentQuery.From.Tables[0].Joins.Add(join.JoinedTable);

					Placeholder = Builder.UpdateNesting(parentQuery, Placeholder);
				}
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsRoot() || flags.IsTraverse() || flags.IsAggregationRoot())
					return path;

				if (OuterJoinParentQuery != null)
				{
					CreateWeakOuterJoin(OuterJoinParentQuery, SelectQuery);
				}

				var result = (Expression)Placeholder;

				// ite means that this context was created during aggregation function translation
				if (Placeholder.Sql is SqlValue)
				{
					result = base.MakeExpression(path, flags);
				}

				return result;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new AggregateExecuteContext(context.CloneExpression(SequenceExpression), context.CloneContext(Context), ElementType)
				{
					Placeholder = context.CloneExpression(Placeholder),
					OuterJoinParentQuery = context.CloneElement(OuterJoinParentQuery),
					_joinedTable = context.CloneElement(_joinedTable),
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
