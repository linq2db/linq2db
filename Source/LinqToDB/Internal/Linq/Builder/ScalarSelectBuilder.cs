using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
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

			if (IsQueryBatch(methodCall))
				ThrowIfPartCreatesTask(body);

			if (buildInfo.IsSubQuery || !IsQueryBatch(methodCall))
				return BuildSequenceResult.FromContext(new ScalarSelectContext(builder.GetTranslationModifier(), builder, buildInfo.Parent, body, selectQuery, isQueryBatch: false));

			builder.CombinedCommandsRequested = true;

			// A batch is new API, so its element operators follow LINQ: an empty First or Single fails. Pushed build-wide
			// (not temporary), so every part and every query nested in one carries it.
			var strict = builder.GetTranslationModifier().WithStrictElementOperators(true);

			// Where nothing is combined, the configured strategy stays: it may read the collections in fewer statements. It is
			// set on the root context alone, which makes the collections explicit loads without overriding inner markers.
			if (!QueryRunner.CanCombineCommands(builder.DataContext))
			{
				var configured = strict.EagerLoadingStrategy ?? builder.DataContext.Options.LinqOptions.DefaultEagerLoadingStrategy;

				builder.PushTranslationModifier(strict, false);

				var configuredContext = new ScalarSelectContext(strict.WithEagerLoadingStrategy(configured), builder, null, body, selectQuery, isQueryBatch: true);

				builder.PopTranslationModifier();

				return BuildSequenceResult.FromContext(configuredContext);
			}

			// The Default strategy is the one that combines, so it is pushed build-wide too: nested collections resolve their
			// strategy from the builder, and a configured default or an inner With*LoadStrategy marker would otherwise split
			// them back out of the batch.
			builder.PushTranslationModifier(strict.WithEagerLoadingStrategy(EagerLoadingStrategy.Default), false);

			var context = new ScalarSelectContext(builder.GetTranslationModifier(), builder, null, body, selectQuery, isQueryBatch: true);

			builder.PopTranslationModifier();

			return BuildSequenceResult.FromContext(context);
		}

		// A task created by a part starts while the row is read: a query of its own, outside the batch, that nothing observes.
		// Checked when the batch is built, so a cached batch pays nothing for it.
		static void ThrowIfPartCreatesTask(Expression body)
		{
			if (CreatesTask(body))
				throw new LinqToDBException(ErrorHelper.Error_QueryBatch_TaskPart);
		}

		// Shared with QueryBatchBuilder.Set, which refuses the same parts when they are added.
		internal static bool CreatesTask(Expression body)
		{
			return body.Find(static e => IsTask(e.Type) && !IsHeldTask(e)) != null;
		}

		static bool IsTask(Type type)
		{
			return typeof(Task).IsAssignableFrom(type)
				|| type == typeof(ValueTask)
				|| type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>);
		}

		// A task held by a captured variable, a field of the data context, a static field, an array element, a compiled-query
		// argument or a completion source, or a completed task, starts nothing when it is read.
		static bool IsHeldTask(Expression expression)
		{
			while (true)
			{
				switch (expression)
				{
					// Expose turns a captured data context into a query root.
					case ConstantExpression or ParameterExpression or SqlQueryRootExpression:
						return true;

					case MemberExpression { Member: FieldInfo } field:
						if (field.Expression == null)
							return true;

						expression = field.Expression;
						break;

					case MemberExpression { Member: PropertyInfo property } member when ReturnsExistingTask(property):
						if (member.Expression == null)
							return true;

						expression = member.Expression;
						break;

					// A user-defined conversion is a call, which may start the task.
					case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs, Method: null } convert:
						expression = convert.Operand;
						break;

					case BinaryExpression { NodeType: ExpressionType.ArrayIndex } element:
						expression = element.Left;
						break;

					default:
						return false;
				}
			}
		}

		// The non-generic TaskCompletionSource and ValueTask.CompletedTask exist from .NET 5, so they are matched by name.
		static bool ReturnsExistingTask(PropertyInfo property)
		{
			var declaringType = property.DeclaringType;

			return property.Name switch
			{
				nameof(TaskCompletionSource<>.Task) =>
					(declaringType is { IsGenericType: true } && declaringType.GetGenericTypeDefinition() == typeof(TaskCompletionSource<>))
					|| string.Equals(declaringType?.FullName, "System.Threading.Tasks.TaskCompletionSource", StringComparison.Ordinal),
				nameof(Task.CompletedTask) =>
					declaringType == typeof(Task) || declaringType == typeof(ValueTask),
				_ => false,
			};
		}

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		internal sealed class ScalarSelectContext : BuildContextBase
		{
			public override MappingSchema MappingSchema   => Builder.MappingSchema;
			public override Expression    Expression      => Body;

			// db.Select(() => expr) is a single scalar row (SELECT expr) — it must scalarize to (SELECT expr)
			// when used in a scalar context (e.g. a window ORDER BY key), not be treated as a collection.
			public override bool          IsSingleElement => true;

			public Expression Body         { get; }

			// The root of a QueryBatch: each part of it is a query of its own.
			public bool       IsQueryBatch { get; }

			public ScalarSelectContext(TranslationModifier translationModifier, ExpressionBuilder builder, IBuildContext? parent, Expression body, SelectQuery selectQuery, bool isQueryBatch)
				: base(translationModifier, builder, body.Type, selectQuery)
			{
				Parent       = parent;
				Body         = body;
				IsQueryBatch = isQueryBatch;

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
				return new ScalarSelectContext(TranslationModifier, Builder, null, context.CloneExpression(Body), context.CloneElement(SelectQuery), IsQueryBatch);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);

				SetElementSelection(query);
			}

			// One row by construction, read as an element: an element terminal that finds nothing empties the row, which then
			// reports as First on an empty source does.
			public override void SetElementSelection<T>(Query<T> query)
			{
				if (IsQueryBatch)
				{
					SetQueryBatchElementSelection(query);
					return;
				}

				query.GetElement = (db, expr, preambles) =>
					query.GetResultEnumerable(db, expr, preambles).First();

				query.GetElementAsync = async (db, expr, preambles, token) =>
					await query.GetResultEnumerable(db, expr, preambles).FirstAsync(token).ConfigureAwait(false);

				query.GetEagerElement = (db, expr, ps) =>
					query.GetEagerEnumerable(db, expr, ps).Enumerable.First();

				query.GetEagerElementAsync = async (db, expr, ps, token) =>
				{
					var (enumerable, _, _) = await query.GetEagerEnumerableAsync(db, expr, ps, token).ConfigureAwait(false);
					return await enumerable.FirstAsync(token).ConfigureAwait(false);
				};
			}

			// A part's Single reads its rows into the batch row, so more than one batch row means a part found more than one.
			static void SetQueryBatchElementSelection<T>(Query<T> query)
			{
				query.GetElement = (db, expr, preambles) =>
					query.GetResultEnumerable(db, expr, preambles).Single();

				query.GetElementAsync = async (db, expr, preambles, token) =>
					await query.GetResultEnumerable(db, expr, preambles).SingleAsync(token).ConfigureAwait(false);

				query.GetEagerElement = (db, expr, ps) =>
					query.GetEagerEnumerable(db, expr, ps).Enumerable.Single();

				query.GetEagerElementAsync = async (db, expr, ps, token) =>
				{
					var (enumerable, _, _) = await query.GetEagerEnumerableAsync(db, expr, ps, token).ConfigureAwait(false);
					return await enumerable.SingleAsync(token).ConfigureAwait(false);
				};
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}
		}
	}
}
