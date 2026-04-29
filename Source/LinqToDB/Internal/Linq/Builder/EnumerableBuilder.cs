using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Linq;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.AsQueryable))]
	[BuildsExpression(ExpressionType.Constant, ExpressionType.Call, ExpressionType.MemberAccess, ExpressionType.NewArrayInit)]
	sealed class EnumerableBuilder : ISequenceBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.LinqToDB.AsQueryableConfigured);

		public static bool CanBuild(Expression expr, ExpressionBuilder builder)
		{
			if (expr.NodeType == ExpressionType.NewArrayInit)
				return true;

			if (!typeof(IEnumerable<>).IsSameOrParentOf(expr.Type))
				return false;

			if (typeof(IEnumerable<>).GetGenericType(expr.Type) is null)
				return false;

			return expr.NodeType switch
			{
				ExpressionType.MemberAccess => CanBuildMemberChain(((MemberExpression)expr).Expression),
				ExpressionType.Constant     => ((ConstantExpression)expr).Value is IEnumerable,
				ExpressionType.Call         => builder.CanBeEvaluatedOnClient(expr),
				_ => false,
			};

			static bool CanBuildMemberChain(Expression? expr)
			{
				while (expr is { NodeType: ExpressionType.MemberAccess })
					expr = ((MemberExpression)expr).Expression;

				return expr is null or { NodeType: ExpressionType.Constant };
			}
		}

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			// Configured 3-arg form: source.AsQueryable(dataContext, configure).
			if (buildInfo.Expression is MethodCallExpression mc && mc.IsSameGenericMethod(Methods.LinqToDB.AsQueryableConfigured))
				return BuildConfigured(builder, mc, buildInfo);

			var collectionType = typeof(IEnumerable<>).GetGenericType(buildInfo.Expression.Type) ??
			                     throw new InvalidOperationException();

			if (buildInfo.Expression is NewArrayExpression)
			{
				if (buildInfo.Parent == null)
					return BuildSequenceResult.Error(buildInfo.Expression);

				var expressions = ((NewArrayExpression)buildInfo.Expression).Expressions.Select(e =>
						builder.UpdateNesting(buildInfo.Parent!, builder.BuildSqlExpression(buildInfo.Parent, e)))
					.ToArray();

				var dynamicContext = new EnumerableContextDynamic(
					builder.GetTranslationModifier(),
					buildInfo.Parent,
					builder,
					expressions,
					buildInfo.SelectQuery,
					collectionType.GetGenericArguments()[0]);

				return BuildSequenceResult.FromContext(dynamicContext);
			}

			if (builder.CanBeEvaluatedOnClient(buildInfo.Expression))
			{
				var param = builder.ParametersContext.BuildParameter(buildInfo.Parent, buildInfo.Expression, null,
					buildParameterType : ParametersContext.BuildParameterType.InPredicate);

				if (param != null)
				{
					var enumerableContext = new EnumerableContext(builder.GetTranslationModifier(), builder, param, buildInfo.SelectQuery, collectionType.GetGenericArguments()[0]);

					return BuildSequenceResult.FromContext(enumerableContext);
				}
			}

			return BuildSequenceResult.Error(buildInfo.Expression);
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		#region Configured 3-arg AsQueryable

		static BuildSequenceResult BuildConfigured(ExpressionBuilder builder, MethodCallExpression mc, BuildInfo buildInfo)
		{
			var elementType = mc.Method.GetGenericArguments()[0];
			var sourceArg   = mc.Arguments[0];
			var configureArg = mc.Arguments[2];

			// The configured overload expects a materialised IEnumerable<T>. Traverse the source
			// expression first to resolve closures / context refs, then verify it can be evaluated on
			// the client. An inline array that references outer query state (e.g.
			// `from t in db.Person from v in new[] { new Row { Id = t.ID } }.AsQueryable(db, b => b.Parameterize())`)
			// cannot be compiled — reject with a clear error; the user should use the 2-arg
			// AsQueryable(IDataContext) overload, which has EnumerableContextDynamic for per-element
			// expressions.
			var traversedSource = builder.BuildTraverseExpression(sourceArg);
			if (!builder.CanBeEvaluatedOnClient(traversedSource))
				return BuildSequenceResult.Error(mc, "AsQueryable configure: source could not be evaluated on the client; ensure the source is a materialised IEnumerable<T> (use the 2-arg AsQueryable(IDataContext) overload for sources referencing outer query state).");

			var configureLambda = configureArg.UnwrapLambda();
			if (!TryParseConfigure(elementType, configureLambda, out var defaultForceParameter, out var rowParameter, out var excepted, out var parseError))
				return BuildSequenceResult.Error(mc, parseError);

			var param = builder.ParametersContext.BuildParameter(buildInfo.Parent, traversedSource, null,
				buildParameterType: ParametersContext.BuildParameterType.InPredicate);

			if (param == null)
				return BuildSequenceResult.Error(mc);

			var parameterization = new EnumerableParameterizationConfig(defaultForceParameter, rowParameter, excepted);

			var enumerableContext = new EnumerableContext(
				builder.GetTranslationModifier(),
				builder,
				param,
				buildInfo.SelectQuery,
				elementType,
				parameterization);

			return BuildSequenceResult.FromContext(enumerableContext);
		}

		static bool TryParseConfigure(
			Type                                            elementType,
			LambdaExpression                                configureLambda,
			out bool                                        defaultForceParameter,
			out ParameterExpression?                        rowParameter,
			out IReadOnlyList<MemberExpression>?            excepted,
			out string                                      error)
		{
			// Initial value is unreachable in practice — the interface design forces every chain
			// through Parameterize() or Inline() before Except is available — but we still need
			// a defined value before the loop runs.
			defaultForceParameter = true;
			rowParameter          = null;
			excepted              = null;
			error                 = string.Empty;

			List<MemberExpression>? exceptedList = null;

			var builderParameter = configureLambda.Parameters[0];
			var current          = configureLambda.Body;

			while (current is MethodCallExpression call)
			{
				switch (call.Method.Name)
				{
					case nameof(IAsQueryableBuilder<>.Parameterize):
						defaultForceParameter = true;
						current = call.Object ?? call.Arguments[0];
						break;

					case nameof(IAsQueryableBuilder<>.Inline):
						defaultForceParameter = false;
						current = call.Object ?? call.Arguments[0];
						break;

					case nameof(IAsQueryableExceptBuilder<>.Except):
					{
						var membersArg = call.Arguments[call.Arguments.Count - 1];
						if (membersArg is not NewArrayExpression nae)
						{
							error = "AsQueryable configure: Except(...) argument must be a member-selector array literal.";
							return false;
						}

						exceptedList ??= new List<MemberExpression>();
						rowParameter ??= Expression.Parameter(elementType, "p");

						foreach (var item in nae.Expressions)
						{
							var lambda = item.UnwrapLambda();

							// Substitute the per-selector lambda parameter with our shared rowParameter so
							// every Excepted entry has the same root, then strip the implicit boxing
							// Convert that Expression<Func<T, object?>> adds.
							var rerooted = lambda.GetBody(rowParameter).UnwrapConvert();

							if (rerooted is not MemberExpression memberAccess)
							{
								error = $"AsQueryable configure: Except(...) selector must be a member access on the lambda parameter; got '{lambda.Body}'.";
								return false;
							}

							Expression leaf = memberAccess;
							while (leaf is MemberExpression me)
								leaf = me.Expression!;

							if (!ReferenceEquals(leaf, rowParameter))
							{
								error = $"AsQueryable configure: Except(...) selector must be a member access on the lambda parameter; got '{lambda.Body}'.";
								return false;
							}

							exceptedList.Add(memberAccess);
						}

						current = call.Object ?? call.Arguments[0];
						break;
					}

					default:
						error = $"AsQueryable configure: unsupported method '{call.Method.Name}' in chain.";
						return false;
				}
			}

			if (current != builderParameter)
			{
				error = "AsQueryable configure: chain root must be the lambda parameter.";
				return false;
			}

			excepted = exceptedList;
			return true;
		}

		#endregion
	}
}
