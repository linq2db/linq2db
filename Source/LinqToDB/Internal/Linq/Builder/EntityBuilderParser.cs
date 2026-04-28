using System;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Walks an entity-builder configure-lambda body and populates an
	/// <see cref="EntityBuilderConfig"/>. Dispatch is by method-name string so the parser handles
	/// every variant of <c>I*Builder&lt;T&gt;</c> uniformly.
	/// </summary>
	static class EntityBuilderParser
	{
		/// <summary>
		/// Parse the body of a configure-lambda. Recognises
		/// <c>Set</c> / <c>Ignore</c> / <c>When</c> / <c>DoNothing</c>; throws on any other method
		/// name. Standalone callers ignore the <c>When</c> / <c>DoNothing</c> fields on the result
		/// since the standalone interfaces don't expose those at compile time.
		/// </summary>
		public static EntityBuilderConfig Parse(LambdaExpression configureLambda, ParameterExpression entityParm)
		{
			var cfg  = new EntityBuilderConfig(entityParm);
			var expr = configureLambda.Body;

			while (expr is MethodCallExpression mc)
			{
				switch (mc.Method.Name)
				{
					case nameof(IEntityInsertBuilder<,>.Set):
						cfg.Set.Add((Canonicalise(mc.Arguments[0].UnwrapLambda(), entityParm), mc.Arguments[1].UnwrapLambda()));
						break;
					case nameof(IEntityInsertBuilder<,>.Ignore):
						cfg.Ignore.Add(Canonicalise(mc.Arguments[0].UnwrapLambda(), entityParm));
						break;
					case nameof(IUpsertInsertBuilder<>.When):
						cfg.When = mc.Arguments[0].UnwrapLambda();
						break;
					case nameof(IUpsertInsertBuilder<>.DoNothing):
						cfg.DoNothing = true;
						break;
					default:
						throw new LinqToDBException(
							$"Unexpected method '{mc.Method.Name}' inside entity-builder configure expression.");
				}

				expr = mc.Object!;
			}

			if (expr is not ParameterExpression)
				throw new LinqToDBException(
					"Entity-builder configure expression chain must start with the builder parameter; got " + expr.GetType().Name);

			return cfg;
		}

		/// <summary>
		/// Rewrite a field-selector lambda <c>x =&gt; x.Col</c> so its body references the shared
		/// <paramref name="entityParm"/>. Two field selectors that referred to different source
		/// parameters now produce structurally-equal expressions, so
		/// <see cref="ExpressionEqualityComparer"/> can match them.
		/// </summary>
		public static Expression Canonicalise(LambdaExpression fieldLambda, ParameterExpression entityParm)
			=> fieldLambda.GetBody(entityParm);

		/// <summary>
		/// True when <paramref name="call"/> is the entity-builder 3-arg shape
		/// <c>(ITable&lt;T&gt;, T, Expression&lt;Func&lt;TBuilder, TBuilder&gt;&gt;)</c> where
		/// <c>TBuilder</c>'s open generic is <paramref name="expectedReceiverDef"/>.
		/// </summary>
		public static bool IsEntityBuilderShape(MethodCallExpression call, Type expectedReceiverDef)
		{
			if (call.Arguments.Count != 3)
				return false;

			var configureArg = call.Arguments[2];
			if (!configureArg.Type.IsGenericType || configureArg.Type.GetGenericTypeDefinition() != typeof(Expression<>))
				return false;

			var funcType = configureArg.Type.GetGenericArguments()[0];
			if (!funcType.IsGenericType || funcType.GetGenericTypeDefinition() != typeof(Func<,>))
				return false;

			var receiverType = funcType.GetGenericArguments()[0];
			return receiverType.IsGenericType && receiverType.GetGenericTypeDefinition() == expectedReceiverDef;
		}
	}
}
