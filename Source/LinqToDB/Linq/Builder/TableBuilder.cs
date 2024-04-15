using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;
	using Interceptors;

	sealed partial class TableBuilder : ISequenceBuilder
	{
		int ISequenceBuilder.BuildCounter { get; set; }

		enum BuildContextType
		{
			None,
			GetTableMethod,
			MemberAccess,
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
							case "GetTable":
								{
									if (typeof(ITable<>).IsSameOrParentOf(expression.Type))
										return BuildContextType.GetTableMethod;
									break;
								}

							case "TableFromExpression":
							{
								if (typeof(ITable<>).IsSameOrParentOf(expression.Type))
									return BuildContextType.TableFromExpression;
								break;
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

				case ExpressionType.MemberAccess:

					if (typeof(ITable<>).IsSameOrParentOf(expression.Type))
						return BuildContextType.MemberAccess;

					break;

				/*case ExpressionType.Parameter:
					{
						if (buildInfo.IsSubQuery && buildInfo.SelectQuery.From.Tables.Count == 0)
						{
							// It should be handled by GroupByElementBuilder
							//
							if (typeof(IGrouping<,>).IsSameOrParentOf(expression.Type))
								break;

							parentContext = builder.GetContext(buildInfo.Parent, expression);
							if (parentContext != null)
							{
								return BuildContextType.Association;
							}
						}

						break;
					}*/
			}

			return BuildContextType.None;
		}

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return FindBuildContext(builder, buildInfo, out var _) != BuildContextType.None;
		}

		static Expression CallGetTable<T>(IDataContext dataContext)
			where T : class
		{
			return dataContext.GetTable<T>().Expression;
		}

		static BuildSequenceResult ApplyQueryFilters(ExpressionBuilder builder, MappingSchema mappingSchema,
			BuildInfo buildInfo, MemberInfo? memberInfo, TableContext tableContext)
		{
			var entityType = tableContext.ObjectType;
			if (builder.IsFilterDisabled(entityType))
				return BuildSequenceResult.FromContext(tableContext);

			var ed = mappingSchema.GetEntityDescriptor(entityType, builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
			var filterFunc = ed.QueryFilterFunc;
			if (filterFunc == null)
				return BuildSequenceResult.FromContext(tableContext);

			var refExpression = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(entityType), tableContext);

			var        dcParam = filterFunc.Parameters[1];
			Expression dcExpr  = SqlQueryRootExpression.Create(mappingSchema, dcParam.Type);

			var filterLambda = Expression.Lambda(filterFunc.Body.Replace(dcParam, dcExpr), filterFunc.Parameters[0]);
			filterLambda = (LambdaExpression)builder.ConvertExpressionTree(filterLambda);

			if (builder.DataContext is IInterceptable<IQueryExpressionInterceptor> { Interceptor: { } interceptor })
				filterLambda = (LambdaExpression)interceptor.ProcessExpression(filterLambda, new QueryExpressionArgs(builder.DataContext, filterLambda, QueryExpressionArgs.ExpressionKind.QueryFilter));

			Expression sequenceExpr = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(entityType), refExpression, filterLambda);

			var context   = builder.BuildSequence(new BuildInfo(buildInfo, sequenceExpr));
			return BuildSequenceResult.FromContext(context);
		}

		static MappingSchema GetRootMappingSchema(ExpressionBuilder builder, Expression expression)
		{
			if (expression is SqlQueryRootExpression root)
				return root.MappingSchema;

			if (expression is NewExpression ne && ne.Arguments.Count > 0)
			{
				return GetRootMappingSchema(builder, ne.Arguments[0]);
			}

			var dc = builder.EvaluateExpression<IDataContext>(expression);

			if (dc != null)
				return dc.MappingSchema;

			throw new LinqException($"Could not retrieve DataContext information from expression '{expression}'");
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

					var tableContext = new TableContext(builder, mappingSchema, buildInfo, buildInfo.Expression.Type.GetGenericArguments()[0]);
					return ApplyQueryFilters(builder, mappingSchema, buildInfo, null, AddTableInScope(tableContext));
				}
				case BuildContextType.MemberAccess           :
				{
					var me            = (MemberExpression)buildInfo.Expression;
					var mappingSchema = GetRootMappingSchema(builder, me.Expression!);

					var tableContext = new TableContext(builder, mappingSchema, buildInfo, buildInfo.Expression.Type.GetGenericArguments()[0]);
					return ApplyQueryFilters(builder, mappingSchema, buildInfo, null, AddTableInScope(tableContext));
				}
				case BuildContextType.TableFunctionAttribute :
				{
					var mappingSchema = builder.MappingSchema;

					return BuildSequenceResult.FromContext(new TableContext(builder, mappingSchema, buildInfo));
				}
				case BuildContextType.TableFromExpression :
				{
					var mappingSchema = builder.MappingSchema;

					var mc = (MethodCallExpression)buildInfo.Expression;

					var bodyMethod = mc.Arguments[1].UnwrapLambda().Body;

					return BuildSequenceResult.FromContext(new TableContext(builder, mappingSchema, new BuildInfo(buildInfo, bodyMethod)));
				}
				case BuildContextType.AsCteMethod            : return BuildCteContext     (builder, buildInfo);
				case BuildContextType.GetCteMethod           : return BuildRecursiveCteContextTable (builder, buildInfo);
				case BuildContextType.FromSqlMethod          : return BuildRawSqlTable(builder, buildInfo, false);
				case BuildContextType.FromSqlScalarMethod    : return BuildRawSqlTable(builder, buildInfo, true);
			}

			TableContext AddTableInScope(TableContext context)
			{
				builder.TablesInScope?.Add(context);
				return context;
			}

			throw new InvalidOperationException();
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

	}
}
