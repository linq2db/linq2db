using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder.Visitors
{
	public abstract class CanBeEvaluatedOnClientCheckVisitorBase : ExpressionVisitorBase
	{
		protected bool InMethod;
		protected bool CanBeEvaluated;

		protected ExpressionTreeOptimizationContext     OptimizationContext = default!;

		Stack<ReadOnlyCollection<ParameterExpression>>? _allowedParameters;

		public override void Cleanup()
		{
			CanBeEvaluated      = true;
			InMethod            = false;
			OptimizationContext = default!;

			_allowedParameters?.Clear();

			base.Cleanup();
		}

		public override Expression? Visit(Expression? node)
		{
			if (!CanBeEvaluated)
				return node;

			return base.Visit(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			if (!InMethod)
			{
				CanBeEvaluated = false;
				return node;
			}

			_allowedParameters ??= new();

			_allowedParameters.Push(node.Parameters);

			_ = base.VisitLambda(node);

			_allowedParameters.Pop();

			return node;
		}

		internal override Expression VisitContextRefExpression(ContextRefExpression node)
		{
			CanBeEvaluated = false;
			return node;
		}

		internal override Expression VisitSqlErrorExpression(SqlErrorExpression node)
		{
			CanBeEvaluated = false;
			return node;
		}

		public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
		{
			CanBeEvaluated = false;
			return node;
		}

		internal override Expression VisitSqlGenericParamAccessExpression(SqlGenericParamAccessExpression node)
		{
			CanBeEvaluated = false;
			return node;
		}

		internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
		{
			CanBeEvaluated = false;
			return node;
		}

		internal override SqlGenericConstructorExpression.Assignment VisitSqlGenericAssignment(SqlGenericConstructorExpression.Assignment assignment)
		{
			CanBeEvaluated = false;
			return assignment;
		}

		internal override SqlGenericConstructorExpression.Parameter VisitSqlGenericParameter(SqlGenericConstructorExpression.Parameter parameter)
		{
			CanBeEvaluated = false;
			return parameter;
		}

		public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
		{
			CanBeEvaluated = false;
			return node;
		}

		public override Expression VisitSqlQueryRootExpression(SqlQueryRootExpression node)
		{
			CanBeEvaluated = false;
			return node;
		}

		protected override Expression VisitExtension(Expression node)
		{
			if (node.CanReduce)
			{
				Visit(node.Reduce());
				return node;
			}

			CanBeEvaluated = false;

			return node;
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			if (!CanBeEvaluated)
				return node;

			if (OptimizationContext?.IsServerSideOnly(node) == true)
				CanBeEvaluated = false;

			var save = InMethod;
			InMethod = true;

			base.VisitMethodCall(node);

			InMethod = save;

			return node;
		}

		protected override Expression VisitDefault(DefaultExpression node)
		{
			if (node.Type == typeof(void))
			{
				CanBeEvaluated = false;
				return node;
			}

			return base.VisitDefault(node);
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			var isAllowed = false;
			if (_allowedParameters != null)
			{
				foreach (var allowedList in _allowedParameters)
				{
					if (allowedList.Contains(node))
					{
						isAllowed = true;
						break;
					}
				}
			}

			if (!isAllowed)
				CanBeEvaluated = false;

			return node;
		}
	}
}
