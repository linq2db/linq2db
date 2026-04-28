using System;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Translates the entity-builder Insert overload
	/// <c>Insert&lt;T&gt;(this ITable&lt;T&gt;, T item, Expression&lt;Func&lt;IEntityInsertBuilder&lt;T&gt;, IEntityInsertBuilder&lt;T&gt;&gt;&gt; configure)</c>
	/// into the existing <c>Insert&lt;T&gt;(this ITable&lt;T&gt;, Expression&lt;Func&lt;T&gt;&gt;)</c> shape,
	/// then defers to <see cref="InsertBuilder"/> for the actual SQL generation.
	/// </summary>
	/// <remarks>
	/// Only the sync method name is registered — the async entry point on <see cref="LinqExtensions"/>
	/// embeds the sync method-info into the captured expression tree and dispatches via
	/// <c>ExecuteAsync</c>, mirroring the rest of LinqToDB.
	/// </remarks>
	[BuildsMethodCall(nameof(LinqExtensions.Insert))]
	sealed class EntityInsertBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable && IsEntityInsertShape(call);

		/// <summary>
		/// True when <paramref name="call"/> is the entity-builder shape
		/// <c>(ITable&lt;T&gt;, T, Expression&lt;Func&lt;IEntityInsertBuilder&lt;T&gt;, IEntityInsertBuilder&lt;T&gt;&gt;&gt;)</c>.
		/// </summary>
		public static bool IsEntityInsertShape(MethodCallExpression call)
			=> EntityBuilderParser.IsEntityBuilderShape(call, typeof(IEntityInsertBuilder<>));

		protected override BuildSequenceResult BuildMethodCall(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var entityType   = methodCall.Method.GetGenericArguments()[0];
			var tableArg     = methodCall.Arguments[0];
			var itemArg      = methodCall.Arguments[1];        // Expression.Constant(item, T)
			var configureArg = methodCall.Arguments[2];

			var configureLambda = configureArg.UnwrapLambda();
			var entityParm      = Expression.Parameter(entityType, "x");
			var cfg             = EntityBuilderParser.Parse(configureLambda, entityParm);

			var entityDescriptor = builder.MappingSchema.GetEntityDescriptor(
				entityType, builder.DataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

			// Build s => new T { Col = …, … } via the shared setter-builder.
			var sLambdaWithSource = EntitySetterBuilder.BuildInsertSetter(
				entityType, entityDescriptor, entityParm,
				setOverrides: cfg.Set,
				ignoreList:   cfg.Ignore);

			// Existing Insert<T>(ITable<T>, Expression<Func<T>>) expects a parameterless setter.
			// Substitute the source-row parameter with the item constant and re-wrap.
			var closedBody   = sLambdaWithSource.GetBody(itemArg);
			var closedSetter = Expression.Lambda(typeof(Func<>).MakeGenericType(entityType), closedBody);

			var synth = Expression.Call(null,
				Methods.LinqToDB.Insert.FromTable.Insert.MakeGenericMethod(entityType),
				tableArg, Expression.Quote(closedSetter));

			return builder.TryBuildSequence(new BuildInfo(buildInfo, synth));
		}
	}
}
