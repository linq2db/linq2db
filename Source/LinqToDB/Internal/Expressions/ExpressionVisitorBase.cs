using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public abstract class ExpressionVisitorBase : ExpressionVisitor
	{
#if DEBUG
		[DebuggerStepThrough]
		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			return base.Visit(node);
		}
#endif

		public virtual Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
		{
			return node;
		}

		public virtual Expression VisitChangeTypeExpression(ChangeTypeExpression node)
		{
			return node.Update(Visit(node.Expression)!);
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
			return node.Update(Visit(node.SequenceExpression), Visit(node.Predicate));
		}

		internal virtual Expression VisitSqlErrorExpression(SqlErrorExpression node)
		{
			return node;
		}

		internal virtual SqlGenericConstructorExpression.Assignment VisitSqlGenericAssignment(
			SqlGenericConstructorExpression.Assignment assignment)
		{
			return assignment.WithExpression(Visit(assignment.Expression));
		}

		internal virtual SqlGenericConstructorExpression.Parameter VisitSqlGenericParameter(
			SqlGenericConstructorExpression.Parameter parameter)
		{
			return parameter.WithExpression(Visit(parameter.Expression));
		}

		public virtual Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
		{
			var assignments = Visit(node.Assignments, VisitSqlGenericAssignment);

			node = node.ReplaceAssignments(assignments);

			var parameters = Visit(node.Parameters, VisitSqlGenericParameter);

			node = node.ReplaceParameters(parameters);

			return node;
		}

		internal virtual Expression VisitSqlGenericParamAccessExpression(SqlGenericParamAccessExpression node)
		{
			return node;
		}

		internal virtual Expression VisitSqlReaderIsNullExpression(SqlReaderIsNullExpression node)
		{
			return node.Update((SqlPlaceholderExpression)Visit(node.Placeholder)!);
		}

		internal virtual Expression VisitSqlPathExpression(SqlPathExpression node)
		{
			return node;
		}

		public virtual void Cleanup()
		{
		}

		public virtual Expression VisitMarkerExpression(MarkerExpression node)
		{
			return node.Update(Visit(node.InnerExpression));
		}

		public virtual Expression VisitTagExpression(TagExpression node)
		{
			return node.Update(Visit(node.InnerExpression), node.Tag);
		}

		public virtual Expression VisitSqlDefaultIfEmptyExpression(SqlDefaultIfEmptyExpression node)
		{
			return node.Update(Visit(node.InnerExpression), VisitAndConvert(node.NotNullExpressions, nameof(VisitSqlDefaultIfEmptyExpression)));
		}

		public virtual Expression VisitSqlValidateExpression(SqlValidateExpression node)
		{
			return node.Update(Visit(node.InnerExpression));
		}

		public virtual Expression VisitSqlQueryRootExpression(SqlQueryRootExpression node)
		{
			return node;
		}

		public virtual Expression VisitConstantPlaceholder(ConstantPlaceholderExpression node)
		{
			return node;
		}
	}
}
