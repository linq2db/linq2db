using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder;

using LinqToDB.Expressions;
using Reflection;
using Extensions;

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

					var attr = mc.Method.GetTableFunctionAttribute(builder.MappingSchema);

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
					parentContext = builder.GetContext(buildInfo.Parent, expression);
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
		builder.AddQueryableMemberAccessors((filterFunc, fakeQuery), new AccessorMember(memberInfo), builder.DataContext, static (context, mi, dc) =>
		{
			var filtered      = (IQueryable)context.filterFunc.DynamicInvoke(context.fakeQuery, dc)!;

			// here we use light version of optimization, only for comparing trees
			var optimizationContext = new ExpressionTreeOptimizationContext(dc);
			var optimizedExpr       = ExpressionBuilder.CorrectDataConnectionReference(filtered.Expression, ExpressionBuilder.DataContextParam);

			optimizedExpr = optimizationContext.ExposeExpression(optimizedExpr);
			optimizedExpr = optimizationContext.ExpandQueryableMethods(optimizedExpr);

			return optimizedExpr;
		});

		var filtered  = (IQueryable)filterFunc.DynamicInvoke(fakeQuery, builder.DataContext)!;
		var optimized = ExpressionBuilder.CorrectDataConnectionReference(filtered.Expression, ExpressionBuilder.DataContextParam);

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
						AddTableInScope(new(builder, buildInfo, ((IQueryable)buildInfo.Expression.EvaluateExpression()!).ElementType)));
				}
			case BuildContextType.GetTableMethod         :
			case BuildContextType.MemberAccess           :
				{
					return ApplyQueryFilters(builder, buildInfo, null,
						AddTableInScope(new(builder, buildInfo,
							buildInfo.Expression.Type.GetGenericArguments()[0])));
				}
			case BuildContextType.Association            : return parentContext!.GetContext(buildInfo.Expression, 0, buildInfo);
			case BuildContextType.TableFunctionAttribute : return AddTableInScope(new (builder, buildInfo));
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

	public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
	{
		return null;
	}

	public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
	{
		return true;
	}
}
