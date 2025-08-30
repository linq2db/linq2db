using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("AsCte", "GetCte", "FromSql", "FromSqlScalar", CanBuildName = nameof(CanBuildKnownMethods))]
	[BuildsMethodCall("GetTable", "TableFromExpression", CanBuildName = nameof(CanBuildTableMethods))]
	[BuildsExpression(ExpressionType.Call, CanBuildName = nameof(CanBuildAttributedMethods))]
	sealed partial class TableBuilder : ISequenceBuilder
	{
		public static bool CanBuildKnownMethods()
			=> true;

		public static bool CanBuildTableMethods(MethodCallExpression call)
			=> typeof(ITable<>).IsSameOrParentOf(call.Type);

		public static bool CanBuildAttributedMethods(Expression expr, ExpressionBuilder builder)
			=> ((MethodCallExpression)expr).Method.GetTableFunctionAttribute(builder.MappingSchema) != null;

		enum BuildContextType
		{
			None,
			GetTableMethod,
			TableFunctionAttribute,
			TableFromExpression,
			AsCteMethod,
			GetCteMethod,
			FromSqlMethod,
			FromSqlScalarMethod
		}

		static BuildContextType FindBuildContext(ExpressionBuilder builder, BuildInfo buildInfo, out IBuildContext? parentContext)
		{
			parentContext = null;

			var expression = buildInfo.Expression;

			switch (expression.NodeType)
			{
				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)expression;

					switch (mc.Method.Name)
					{
						case "GetTable" 
							when typeof(ITable<>).IsSameOrParentOf(expression.Type):
						{
							return BuildContextType.GetTableMethod;
						}

						case "TableFromExpression"
							when typeof(ITable<>).IsSameOrParentOf(expression.Type):
						{
							return BuildContextType.TableFromExpression;
						}

						case "AsCte":
							return BuildContextType.AsCteMethod;

						case "GetCte":
							return BuildContextType.GetCteMethod;

						case "FromSql":
							return BuildContextType.FromSqlMethod;

						case "FromSqlScalar":
							return BuildContextType.FromSqlScalarMethod;
					}

					var attr = mc.Method.GetTableFunctionAttribute(builder.MappingSchema);

					if (attr != null)
						return BuildContextType.TableFunctionAttribute;

					break;
				}
			}

			return BuildContextType.None;
		}

		static Expression ApplyQueryFilters(ExpressionBuilder builder, MappingSchema mappingSchema, Type entityType, Expression tableExpression)
		{
			if (builder.IsFilterDisabled(entityType) || builder.IsFilterDisabledForExpression(tableExpression))
				return tableExpression;

			var testEd = mappingSchema.GetEntityDescriptor(entityType, builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

			if (testEd.QueryFilterLambda == null && testEd.QueryFilterFunc == null)
				return tableExpression;

			Expression filteredExpression;

			if (testEd.QueryFilterFunc == null)
			{
				// shortcut for simple case. We know that MappingSchema is read only and can be sure that comparing cache will not require complex logic.

				var dcParam = testEd.QueryFilterLambda!.Parameters[1];
				var dcExpr  = SqlQueryRootExpression.Create(mappingSchema, dcParam.Type);

				var filterLambda = Expression.Lambda(testEd.QueryFilterLambda.Body.Replace(dcParam, dcExpr), testEd.QueryFilterLambda.Parameters[0]);

				// to avoid recursion
				filteredExpression = Expression.Call(Methods.LinqToDB.DisableFilterInternal.MakeGenericMethod(entityType), tableExpression);

				filteredExpression = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(entityType), filteredExpression, Expression.Quote(filterLambda));
				filteredExpression = ExpressionBuilder.ExposeExpression(filteredExpression, builder.DataContext, builder.OptimizationContext, builder.ParameterValues, optimizeConditions: true, compactBinary: true);

				if (builder.DataContext is IInterceptable<IQueryExpressionInterceptor> { Interceptor: { } interceptor })
					filteredExpression = interceptor.ProcessExpression(filteredExpression, new QueryExpressionArgs(builder.DataContext, filteredExpression, QueryExpressionArgs.ExpressionKind.QueryFilter));
			}
			else
			{
				// Closure should capture mappingSchema, entityType and tableExpression only. Used in EqualsToVisitor
				filteredExpression = builder.ParametersContext.RegisterDynamicExpressionAccessor(tableExpression, builder.DataContext, mappingSchema, (dc, ms) =>
				{
					var ed = ms.GetEntityDescriptor(entityType, dc.Options.ConnectionOptions.OnEntityDescriptorCreated);

					var filterLambdaExpr = ed.QueryFilterLambda;
					var filterFunc       = ed.QueryFilterFunc;

					// to avoid recursion
					Expression sequenceExpr = Expression.Call(Methods.LinqToDB.DisableFilterInternal.MakeGenericMethod(entityType), tableExpression);

					if (filterLambdaExpr != null)
					{
						var dcParam = filterLambdaExpr.Parameters[1];
						var dcExpr  = SqlQueryRootExpression.Create(ms, dcParam.Type);

						var filterLambda = Expression.Lambda(filterLambdaExpr.Body.Replace(dcParam, dcExpr), filterLambdaExpr.Parameters[0]);

						sequenceExpr = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(entityType), sequenceExpr, Expression.Quote(filterLambda));
					}

					if (filterFunc != null)
					{
						var query    = ExpressionQueryImpl.CreateQuery(entityType, dc, sequenceExpr);
						var filtered = filterFunc.DynamicInvokeExt<IQueryable>(query, dc);

						sequenceExpr = filtered.Expression;
					}

					if (dc is IInterceptable<IQueryExpressionInterceptor> { Interceptor: { } interceptor })
						sequenceExpr = interceptor.ProcessExpression(sequenceExpr, new QueryExpressionArgs(dc, sequenceExpr, QueryExpressionArgs.ExpressionKind.QueryFilter));
					// Optimize conditions and compact binary expressions
					var optimizationContext = new ExpressionTreeOptimizationContext(dc);
					sequenceExpr = ExpressionBuilder.ExposeExpression(sequenceExpr, dc, optimizationContext, null, optimizeConditions : true, compactBinary : true);

					return sequenceExpr;
				});
			}

			return filteredExpression;
		}

		static MappingSchema GetRootMappingSchema(ExpressionBuilder builder, Expression expression)
		{
			if (expression is SqlQueryRootExpression root)
				return root.MappingSchema;

			if (expression is NewExpression { Arguments.Count: > 0 } ne)
				return GetRootMappingSchema(builder, ne.Arguments[0]);

			var dc = builder.EvaluateExpression<IDataContext>(expression);

			if (dc != null)
				return dc.MappingSchema;

			throw new LinqToDBException($"Could not retrieve DataContext information from expression '{expression}'");
		}

		BuildSequenceResult BuildTableWithAppliedFilters(ExpressionBuilder builder, BuildInfo buildInfo, MappingSchema mappingSchema, Expression tableExpression)
		{
			var entityType      = tableExpression.Type.GetGenericArguments()[0];
			var applied         = ApplyQueryFilters(builder, mappingSchema, entityType, tableExpression);

			if (!ReferenceEquals(applied, tableExpression))
			{
				return builder.TryBuildSequence(new BuildInfo(buildInfo, applied));
			}

			var tableContext = new TableContext(builder.GetTranslationModifier(), builder, mappingSchema, buildInfo, entityType);
			builder.TablesInScope?.Add(tableContext);
			return BuildSequenceResult.FromContext(tableContext);
		}

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var type = FindBuildContext(builder, buildInfo, out var parentContext);

			switch (type)
			{
				case BuildContextType.None                   : return BuildSequenceResult.NotSupported();

				case BuildContextType.GetTableMethod         :
				{
					var mc = (MethodCallExpression)buildInfo.Expression;
					var mappingSchema = GetRootMappingSchema(builder, mc.Arguments[0]);

					return BuildTableWithAppliedFilters(builder, buildInfo, mappingSchema, buildInfo.Expression);
				}

				case BuildContextType.TableFunctionAttribute :
				{
					var mappingSchema = builder.MappingSchema;

					return BuildSequenceResult.FromContext(new TableContext(builder.GetTranslationModifier(), builder, mappingSchema, buildInfo));
				}

				case BuildContextType.TableFromExpression    :
				{
					var mappingSchema = builder.MappingSchema;

					var mc = (MethodCallExpression)buildInfo.Expression;

					var bodyMethod = mc.Arguments[1].UnwrapLambda().Body;

					return BuildSequenceResult.FromContext(new TableContext(builder.GetTranslationModifier(), builder, mappingSchema, new BuildInfo(buildInfo, bodyMethod)));
				}
				case BuildContextType.AsCteMethod            : return BuildCteContext     (builder, buildInfo);
				case BuildContextType.GetCteMethod           : return BuildRecursiveCteContextTable (builder, buildInfo);
				case BuildContextType.FromSqlMethod          : return BuildRawSqlTable(builder, buildInfo, false);
				case BuildContextType.FromSqlScalarMethod    : return BuildRawSqlTable(builder, buildInfo, true);
			}

			throw new InvalidOperationException();
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

	}
}
