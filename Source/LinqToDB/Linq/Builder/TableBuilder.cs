using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Reflection;
	using Extensions;
	using SqlQuery;

	partial class TableBuilder : ISequenceBuilder
	{
		int ISequenceBuilder.BuildCounter { get; set; }

		enum BuildContextType
		{
			None,
			TableConstant,
			GetTableMethod,
			MemberAccess,
			Association,
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

						var attr = builder.GetTableFunctionAttribute(mc.Method);

						if (attr != null)
							return BuildContextType.TableFunctionAttribute;

						if (mc.IsAssociation(builder.MappingSchema))
						{
							parentContext = builder.GetContext(buildInfo.Parent, expression);
							if (parentContext != null)
								return BuildContextType.Association;
						}

						break;
					}

				case ExpressionType.MemberAccess:

					if (typeof(ITable<>).IsSameOrParentOf(expression.Type))
						return BuildContextType.MemberAccess;

					// Looking for association.
					//
					if (buildInfo.IsSubQuery/* && buildInfo.SelectQuery.From.Tables.Count == 0*/)
					{
						parentContext = buildInfo.Parent;
						if (expression.GetLevel(builder.MappingSchema) == 1)
							parentContext = builder.GetContext(parentContext, expression);
						//builder.GetContext(buildInfo.Parent, expression);
						if (parentContext != null)
							parentContext = parentContext.GetContext(expression, 0, new BuildInfo(buildInfo, expression, new SelectQuery()));
						if (parentContext != null)
							return BuildContextType.Association;
					}

					break;

				case ExpressionType.Parameter:
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
					}
			}

			return BuildContextType.None;
		}

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return FindBuildContext(builder, buildInfo, out var _) != BuildContextType.None;
		}

		IBuildContext ApplyQueryFilters(ExpressionBuilder builder, BuildInfo buildInfo, MemberInfo? memberInfo, TableContext tableContext)
		{
			var entityType = tableContext.ObjectType;
			if (builder.IsFilterDisabled(entityType))
				return tableContext;

			var ed = builder.MappingSchema.GetEntityDescriptor(entityType);
			var filterFunc = ed.QueryFilterFunc;
			if (filterFunc == null)
				return tableContext;

			if (memberInfo == null)
			{
				memberInfo = Methods.LinqToDB.GetTable.MakeGenericMethod(entityType);
			}

			var fakeQuery = ExpressionQueryImpl.CreateQuery(entityType, builder.DataContext, null);

			// Here we tell for Equality Comparer to compare optimized expressions 
			//
			builder.AddQueryableMemberAccessors(new AccessorMember(memberInfo), builder.DataContext, (mi, dc) =>
			{
				var filtered      = (IQueryable)filterFunc.DynamicInvoke(fakeQuery, dc)!;

				// here we use light version of optimization, only for comparing trees
				var optimizationContext = new ExpressionTreeOptimizationContext(dc);
				var optimizedExpr = optimizationContext.ExposeExpression(filtered.Expression);
				    optimizedExpr = optimizationContext.ExpandQueryableMethods(optimizedExpr);
				return optimizedExpr;
			});

			var filtered = (IQueryable)filterFunc.DynamicInvoke(fakeQuery, builder.DataContext)!;
			var optimized = filtered.Expression;

			optimized = builder.ConvertExpressionTree(optimized);
			optimized = builder.ConvertExpression(optimized);

			var refExpression = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(entityType), tableContext);
			var replaced = optimized.Replace(fakeQuery.Expression, refExpression);
			if (replaced == optimized)
				throw new LinqException("Could not correct query result for processing.");

			var context   = builder.BuildSequence(new BuildInfo(buildInfo, replaced));
			return context;

		}

		public IBuildContext? BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var type = FindBuildContext(builder, buildInfo, out var parentContext);

			switch (type)
			{
				case BuildContextType.None                   : return null;
				case BuildContextType.TableConstant:
					{
						return ApplyQueryFilters(builder, buildInfo, null,
							new TableContext(builder, buildInfo, ((IQueryable)buildInfo.Expression.EvaluateExpression()!).ElementType));
					}
				case BuildContextType.GetTableMethod         :
				case BuildContextType.MemberAccess           :
					{
						return ApplyQueryFilters(builder, buildInfo, null,
							new TableContext(builder, buildInfo,
								buildInfo.Expression.Type.GetGenericArguments()[0]));
					}
				case BuildContextType.Association            :
				{
					//TODO: Temporary workaround
					if (parentContext is GroupByBuilder.GroupByContext)
						return parentContext!.GetContext(null, 0, buildInfo);

					var ctx = builder.GetContext(parentContext, buildInfo.Expression);

					return ctx!.GetContext(buildInfo.Expression, 0, buildInfo);
				}
				case BuildContextType.TableFunctionAttribute : return new TableContext    (builder, buildInfo);
				case BuildContextType.AsCteMethod            : return BuildCteContext     (builder, buildInfo);
				case BuildContextType.CteConstant            : return BuildCteContextTable(builder, buildInfo);
				case BuildContextType.FromSqlMethod          : return BuildRawSqlTable(builder, buildInfo, false);
				case BuildContextType.FromSqlScalarMethod    : return BuildRawSqlTable(builder, buildInfo, true);
			}

			throw new InvalidOperationException();
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			if (buildInfo.InAggregation)
				return false;
			return true;
		}
	}
}
