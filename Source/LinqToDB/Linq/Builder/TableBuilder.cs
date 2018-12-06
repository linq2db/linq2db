using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
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
			CteConstant
		}

		static BuildContextType FindBuildContext(ExpressionBuilder builder, BuildInfo buildInfo, out IBuildContext parentContext)
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
							if (typeof(ITable<>).IsSameOrParentOf(expression.Type))
									return BuildContextType.GetTableMethod;
								break;

							case "AsCte":
								return BuildContextType.AsCteMethod;
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
					if (buildInfo.IsSubQuery && buildInfo.SelectQuery.From.Tables.Count == 0)
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
							parentContext = builder.GetContext(buildInfo.Parent, expression);
							if (parentContext != null)
								return BuildContextType.Association;
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

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var type = FindBuildContext(builder, buildInfo, out var parentContext);

			switch (type)
			{
				case BuildContextType.None                   : return null;
				case BuildContextType.TableConstant          : return new TableContext(builder, buildInfo, ((IQueryable)buildInfo.Expression.EvaluateExpression()).ElementType);
				case BuildContextType.GetTableMethod         :
				case BuildContextType.MemberAccess           : return new TableContext(builder, buildInfo, buildInfo.Expression.Type.GetGenericArgumentsEx()[0]);
				case BuildContextType.Association            : return parentContext.GetContext(buildInfo.Expression, 0, buildInfo);
				case BuildContextType.TableFunctionAttribute : return new TableContext    (builder, buildInfo);
				case BuildContextType.AsCteMethod            : return BuildCteContext     (builder, buildInfo);
				case BuildContextType.CteConstant            : return BuildCteContextTable(builder, buildInfo);
			}

			throw new InvalidOperationException();
		}

		public SequenceConvertInfo Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
