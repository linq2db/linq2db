using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public class ExpressionVisitorBase : ExpressionVisitor
	{
		public virtual Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
		{
			return node;
		}

		public virtual Expression VisitChangeTypeExpression(ChangeTypeExpression node)
		{
			return node.Update(Visit(node.Expression)!);
		}

		internal virtual Expression VisitContextConstructionExpression(ContextConstructionExpression node)
		{
			return node.Update(node.BuildContext, Visit(node.InnerExpression)!);
		}

		internal virtual Expression VisitContextRefExpression(ContextRefExpression node)
		{
			return node;
		}

		internal virtual Expression VisitConvertFromDataReaderExpression(ConvertFromDataReaderExpression node)
		{
			return node;
		}

		public virtual Expression VisitDefaultValueExpression(DefaultValueExpression node)
		{
			return node;
		}

		internal virtual Expression VisitSqlAdjustTypeExpression(SqlAdjustTypeExpression node)
		{
			return node.Update(Visit(node.Expression)!);
		}

		internal virtual Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
		{
			return node;
		}

		internal virtual Expression VisitSqlErrorExpression(SqlErrorExpression node)
		{
			return node;
		}

		internal virtual Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
		{
			var assignments = Visit(node.Assignments,
				a => a.WithExpression(Visit(a.Expression)!));

			if (!ReferenceEquals(assignments, node.Assignments))
			{
				node = node.ReplaceAssignments(assignments.ToList());
			}

			var parameters = Visit(node.Parameters,
				p => p.WithExpression(Visit(p.Expression)!));

			if (!ReferenceEquals(parameters, node.Parameters))
			{
				node = node.ReplaceParameters(parameters.ToList());
			}

			return node;
		}

		internal virtual Expression VisitSqlGenericParamAccessExpression(SqlGenericParamAccessExpression node)
		{
			return node.Update(Visit(node.Constructor)!);
		}

		internal virtual Expression VisitSqlReaderIsNullExpression(SqlReaderIsNullExpression node)
		{
			return node.Update((SqlPlaceholderExpression)Visit(node.Placeholder)!);
		}

		public virtual void Cleanup()
		{
		}
	}
}
