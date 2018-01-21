using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
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

						if (c.Value is IQueryable)
						{
							if (typeof(CteTable<>).IsSameOrParentOf(c.Value.GetType()))
								return BuildContextType.CteConstant;

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

		static IBuildContext BuildCteContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			Expression bodyExpr;
			IQueryable query = null;
			string     name  = null;

			switch (methodCall.Arguments.Count)
			{
				case 1 :
					bodyExpr = methodCall.Arguments[0].Unwrap();
					break;
				case 2 :
					bodyExpr = methodCall.Arguments[0].Unwrap();
					name     = methodCall.Arguments[1].EvaluateExpression() as string;
					break;
				case 3 :
					query    = methodCall.Arguments[0].EvaluateExpression() as IQueryable;
					bodyExpr = methodCall.Arguments[1].Unwrap();
					name     = methodCall.Arguments[2].EvaluateExpression() as string;
					break;
				default:
					throw new InvalidOperationException();
			}

			builder.RegisterCte(query, bodyExpr, () => new CteClause(null, bodyExpr.Type.GetGenericArgumentsEx()[0], name));

			var cte = builder.BuildCte(bodyExpr,
				cteClause =>
				{
					var info      = new BuildInfo(buildInfo, bodyExpr, new SelectQuery());
					var sequence  = builder.BuildSequence(info);

					if (cteClause == null)
						cteClause = new CteClause(sequence.SelectQuery, bodyExpr.Type.GetGenericArgumentsEx()[0], name);
					else
					{
						cteClause.Body = sequence.SelectQuery;
						cteClause.Name = name;
					}

					return Tuple.Create(cteClause, sequence);
				}
			);

			var cteBuildInfo = new BuildInfo(buildInfo, bodyExpr, buildInfo.SelectQuery);
			var cteContext   = new CteTableContext(builder, cteBuildInfo, cte.Item1, bodyExpr);

			return cteContext;
		}

		static CteTableContext BuildCteContextTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var queryable    = (IQueryable)buildInfo.Expression.EvaluateExpression();
			var cteInfo      = builder.RegisterCte(queryable, null, () => new CteClause(null, queryable.ElementType, ""));
			var cteBuildInfo = new BuildInfo(buildInfo, cteInfo.Item3, buildInfo.SelectQuery);
			var cteContext   = new CteTableContext(builder, cteBuildInfo, cteInfo.Item1, cteInfo.Item3);

			return cteContext;
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
