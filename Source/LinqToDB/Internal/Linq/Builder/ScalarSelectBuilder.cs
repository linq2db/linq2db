using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Select", nameof(LinqExtensions.QueryBatch))]
	sealed class ScalarSelectBuilder : MethodCallBuilder
	{

		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.LinqToDB.Select) || IsQueryBatch(call);

		// Every arity: the tuple overloads reach the builder as written when nested in a query or compiled.
		internal static bool IsQueryBatch(MethodCallExpression call)
			=> call.Method.DeclaringType == typeof(LinqExtensions) && call.Method.Name is nameof(LinqExtensions.QueryBatch);

		public override bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo) => true;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var selectQuery = buildInfo.SelectQuery;
			var body        = methodCall.Arguments.Count == 2
				? methodCall.Arguments[1].UnwrapLambda().Body
				: LinqExtensions.NewQueryBatchTuple(methodCall.Type, methodCall.Arguments.Skip(1).Select(static a => a.UnwrapLambda().Body));

			if (buildInfo.IsSubQuery || !IsQueryBatch(methodCall))
				return BuildSequenceResult.FromContext(new ScalarSelectContext(builder.GetTranslationModifier(), builder, buildInfo.Parent, body, selectQuery));

			builder.CombinedCommandsRequested = true;

			// Where nothing is combined, the configured strategy stays: it may read the collections in fewer statements. It is
			// set on the root context alone, which makes the collections explicit loads without overriding inner markers.
			if (!QueryRunner.CanCombineCommands(builder.DataContext, out _))
			{
				var modifier   = builder.GetTranslationModifier();
				var configured = modifier.EagerLoadingStrategy ?? builder.DataContext.Options.LinqOptions.DefaultEagerLoadingStrategy;

				return BuildSequenceResult.FromContext(new ScalarSelectContext(modifier.WithEagerLoadingStrategy(configured), builder, null, body, selectQuery));
			}

			// The Default strategy is the one that combines, so it is pushed build-wide (not temporary): nested
			// collections resolve their strategy from the builder, and a configured default or an inner With*LoadStrategy
			// marker would otherwise split them back out of the batch.
			builder.PushTranslationModifier(builder.GetTranslationModifier().WithEagerLoadingStrategy(EagerLoadingStrategy.Default), false);

			var context = new ScalarSelectContext(builder.GetTranslationModifier(), builder, null, body, selectQuery);

			builder.PopTranslationModifier();

			return BuildSequenceResult.FromContext(context);
		}

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		sealed class ScalarSelectContext : BuildContextBase
		{
			public override MappingSchema MappingSchema   => Builder.MappingSchema;
			public override Expression    Expression      => Body;

			// db.Select(() => expr) is a single scalar row (SELECT expr) — it must scalarize to (SELECT expr)
			// when used in a scalar context (e.g. a window ORDER BY key), not be treated as a collection.
			public override bool          IsSingleElement => true;

			public Expression Body { get; }

			public ScalarSelectContext(TranslationModifier translationModifier, ExpressionBuilder builder, IBuildContext? parent, Expression body, SelectQuery selectQuery)
				: base(translationModifier, builder, body.Type, selectQuery)
			{
				Parent = parent;
				Body   = body;

				// Keep the wrapping SELECT: once MakeExpression materializes the body as (SELECT body), the optimizer
				// would otherwise inline a no-FROM scalar subquery back to a bare constant — invalid as a window ORDER BY
				// key on SQL Server. Set here (not in the builder) so the flag survives Clone(), which does not copy it.
				SelectQuery.DoNotRemove = true;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this))
				{
					var expression = Body.Unwrap();

					// When projected for SQL, promote the body to a column of THIS context's SelectQuery and return a
					// placeholder pointing to the whole SelectQuery — a (SELECT body) scalar subquery — instead of
					// inlining the body. Inlining a constant body yields a bare constant, invalid as a window ORDER BY
					// key on SQL Server. Owner is null: the outer UpdateNesting anchors it.
					if (flags.IsSql())
					{
						var bodySql = Builder.BuildSqlExpression(this, expression);
						if (bodySql is SqlPlaceholderExpression)
						{
							Builder.ToColumns(this, bodySql);
							return ExpressionBuilder.CreatePlaceholder((SelectQuery?)null, SelectQuery, path);
						}
					}

					// Nested, the body is a value of the enclosing row: built here, its subqueries would hang off this
					// SelectQuery, which is never embedded, and read the enclosing query's columns instead.
					if (Parent != null && flags.IsExpression())
						return Builder.BuildExpression(Parent, expression);

					return expression;
				}

				return path;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new ScalarSelectContext(TranslationModifier, Builder, context.CloneContext(Parent), context.CloneExpression(Body), context.CloneElement(SelectQuery));
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);

				SetElementSelection(query);
			}

			// One row by construction; a compiled query reads it as an element.
			public override void SetElementSelection<T>(Query<T> query)
			{
				query.GetElement = (db, expr, preambles) =>
					query.GetResultEnumerable(db, expr, preambles).First();

				query.GetElementAsync = async (db, expr, preambles, token) =>
					await query.GetResultEnumerable(db, expr, preambles).FirstAsync(token).ConfigureAwait(false);
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}
		}
	}
}
