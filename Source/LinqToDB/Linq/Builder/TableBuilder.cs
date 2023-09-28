using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using Reflection;

	sealed partial class TableBuilder : ISequenceBuilder
	{
		int ISequenceBuilder.BuildCounter { get; set; }

		enum BuildContextType
		{
			None,
			TableConstant,
			GetTableMethod,
			MemberAccess,
			TableFunctionAttribute,
			AsCteMethod,
			CteConstant,
			FromSqlMethod,
			FromSqlScalarMethod
		}

		static BuildContextType FindBuildContext(ExpressionBuilder builder, BuildInfo buildInfo, out IBuildContext? parentContext)
		{
			parentContext = null;

			var expression = buildInfo.Expression;

			switch (expression.NodeType)
			{
				case ExpressionType.Constant:
					{
						var c = (ConstantExpression)expression;

						if (c.Value is IQueryable queryable)
						{
							if (typeof(CteTable<>).IsSameOrParentOf(c.Value.GetType()))
								return BuildContextType.CteConstant;

							// Avoid collision with ArrayBuilder
							var elementType = queryable.ElementType;
							if (builder.MappingSchema.IsScalarType(elementType) && typeof(EnumerableQuery<>).IsSameOrParentOf(c.Value.GetType()))
								break;

							if (queryable.Expression.NodeType == ExpressionType.NewArrayInit)
								break;

							return BuildContextType.TableConstant;
						}

						break;
					}

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

							case "AsCte":
								return BuildContextType.AsCteMethod;

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

		static MethodInfo _callGetTable = MemberHelper.MethodOfGeneric((IDataContext dc) => CallGetTable<object>(dc));

		static Expression CallGetTable<T>(IDataContext dataContext) 
			where T : class
		{
			return dataContext.GetTable<T>().Expression;
		}

		static IBuildContext ApplyQueryFilters(ExpressionBuilder builder, MappingSchema mappingSchema,
			BuildInfo buildInfo, MemberInfo? memberInfo, TableContext tableContext)
		{
			var entityType = tableContext.ObjectType;
			if (builder.IsFilterDisabled(entityType))
				return tableContext;

			var ed = mappingSchema.GetEntityDescriptor(entityType, builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
			var filterFunc = ed.QueryFilterFunc;
			if (filterFunc == null)
				return tableContext;

			var refExpression = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(entityType), tableContext);

			var        dcParam = filterFunc.Parameters[1];
			Expression dcExpr  = SqlQueryRootExpression.Create(mappingSchema, dcParam.Type);

			var filterLambda = Expression.Lambda(filterFunc.Body.Replace(dcParam, dcExpr), filterFunc.Parameters[0]);
			filterLambda = (LambdaExpression)builder.ConvertExpressionTree(filterLambda);

			Expression sequenceExpr = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(entityType), refExpression, filterLambda);

			var context   = builder.BuildSequence(new BuildInfo(buildInfo, sequenceExpr));
			return context;
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


		public IBuildContext? BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var type = FindBuildContext(builder, buildInfo, out var parentContext);

			switch (type)
			{
				case BuildContextType.None                   : return null;
				case BuildContextType.TableConstant:
				{
					throw new NotImplementedException(); // Set correct MappingSchema
					var tableContext = new TableContext(builder, builder.MappingSchema, buildInfo, builder.EvaluateExpression<IQueryable>(buildInfo.Expression)!.ElementType);
					return ApplyQueryFilters(builder, null, buildInfo, null, AddTableInScope(tableContext));
				}
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
					var mappingSchema = GetRootMappingSchema(builder, me.Expression);

					var tableContext = new TableContext(builder, mappingSchema, buildInfo, buildInfo.Expression.Type.GetGenericArguments()[0]);
					return ApplyQueryFilters(builder, mappingSchema, buildInfo, null, AddTableInScope(tableContext));
				}
				case BuildContextType.TableFunctionAttribute :
				{
					var mappingSchema = builder.MappingSchema;

					if (buildInfo.Expression is MethodCallExpression mc)
					{
						if (mc.Method.IsStatic)
						{
							mappingSchema = GetRootMappingSchema(builder, mc.Arguments[0]);
						}
						else
						{
							mappingSchema = GetRootMappingSchema(builder, mc.Object!);
						}
					}

					return new TableContext(builder, mappingSchema, buildInfo);
				};
				case BuildContextType.AsCteMethod            : return BuildCteContext     (builder, buildInfo);
				case BuildContextType.CteConstant            : return BuildCteContextTable(builder, buildInfo);
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
