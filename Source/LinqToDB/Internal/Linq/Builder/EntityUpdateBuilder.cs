using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Translates the entity-builder Update overload
	/// <c>Update&lt;T&gt;(this ITable&lt;T&gt;, T item, Expression&lt;Func&lt;IEntityUpdateBuilder&lt;T&gt;, IEntityUpdateBuilder&lt;T&gt;&gt;&gt; configure)</c>
	/// into the existing
	/// <c>Update&lt;T&gt;(this IQueryable&lt;T&gt;, Expression&lt;Func&lt;T, bool&gt;&gt;, Expression&lt;Func&lt;T, T&gt;&gt;)</c>
	/// shape with a PK-match predicate. <see cref="UpdateBuilder"/> handles the resulting call.
	/// </summary>
	/// <remarks>
	/// Only the sync method name is registered — the async entry point on <see cref="LinqExtensions"/>
	/// embeds the sync method-info into the captured expression tree and dispatches via
	/// <c>ExecuteAsync</c>, mirroring the rest of LinqToDB.
	/// </remarks>
	[BuildsMethodCall(nameof(LinqExtensions.Update))]
	sealed class EntityUpdateBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable && IsEntityUpdateShape(call);

		/// <summary>
		/// True when <paramref name="call"/> is the entity-builder shape
		/// <c>(ITable&lt;T&gt;, T, Expression&lt;Func&lt;IEntityUpdateBuilder&lt;T&gt;, IEntityUpdateBuilder&lt;T&gt;&gt;&gt;)</c>.
		/// </summary>
		public static bool IsEntityUpdateShape(MethodCallExpression call)
			=> EntityBuilderParser.IsEntityBuilderShape(call, typeof(IEntityUpdateBuilder<>));

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

			var pkColumns = entityDescriptor.Columns.Where(c => c.IsPrimaryKey).ToList();
			if (pkColumns.Count == 0)
				return BuildSequenceResult.Error(
					methodCall,
					$"Entity Update requires the '{entityType.Name}' table to have a primary key — none is declared.");

			// Build PK predicate: t => t.Pk1 == item.Pk1 && t.Pk2 == item.Pk2 && ...
			var tParm = Expression.Parameter(entityType, "t");

			Expression? predicateBody = null;
			foreach (var pk in pkColumns)
			{
				var lhs = pk.MemberAccessor.GetGetterExpression(tParm);
				var rhs = pk.MemberAccessor.GetGetterExpression(itemArg);
				var eq  = Expression.Equal(lhs, rhs);
				predicateBody = predicateBody == null ? eq : Expression.AndAlso(predicateBody, eq);
			}

			var predicateLambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(entityType, typeof(bool)), predicateBody!, tParm);

			// Build (t, s) => new T { Col = ..., ... } via the shared setter-builder.
			var tsLambda = EntitySetterBuilder.BuildUpdateSetter(
				entityType, entityDescriptor, entityParm,
				setOverrides: cfg.Set,
				ignoreList:   cfg.Ignore);

			// Substitute s with the item constant to get t => new T { … }; rebase t to share the predicate's parameter.
			var tsTargetParm = tsLambda.Parameters[0];
			var tsSourceParm = tsLambda.Parameters[1];

			var setterBody   = tsLambda.Body.Replace(tsSourceParm, itemArg).Replace(tsTargetParm, tParm);
			var setterLambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(entityType, entityType), setterBody, tParm);

			// Synthesise q.Update(p, s). UpdateBuilder picks it up.
			var synth = Expression.Call(null,
				Methods.LinqToDB.Update.UpdatePredicateSetter.MakeGenericMethod(entityType),
				tableArg, Expression.Quote(predicateLambda), Expression.Quote(setterLambda));

			return builder.TryBuildSequence(new BuildInfo(buildInfo, synth));
		}
	}
}
