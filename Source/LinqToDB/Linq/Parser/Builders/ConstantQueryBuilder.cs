using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Expressions;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class ConstantQueryBuilder : BaseBuilder
	{
		public override bool CanBuild(Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Constant:
					{
						var type = ((ConstantExpression)expression).Value?.GetType();
						return type != null && typeof(IQueryable<>).IsSameOrParentOf(type);
					}
				case ExpressionType.MemberAccess:
					{
						var ma = (MemberExpression)expression;
						return ma.Expression.NodeType == ExpressionType.Constant;
					}
				default:
					return false;
			}
		}

		public override Sequence BuildSequence(ModelParser builder, ParseBuildInfo parseBuildInfo, Expression expression)
		{
			var query = expression.EvaluateExpression();
			var itemType = query.GetType().GetGenericArgumentsEx()[0];

			if (typeof(Table<>).IsSameOrParentOf(query.GetType()))
			{
				parseBuildInfo.Sequence.AddClause(new TableSource(itemType, ""));
			}
			else
			{
				parseBuildInfo.Sequence.AddClause(new ArrayQueryClause(itemType, "", query));
			}

			return parseBuildInfo.Sequence;
		}
	}
}
