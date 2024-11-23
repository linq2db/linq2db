using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	abstract class CanBeEvaluatedOnClientCheckVisitorBase : ExpressionVisitorBase
	{
		protected bool _inMethod;
		protected bool _canBeEvaluated;

		Stack<ReadOnlyCollection<ParameterExpression>>? _allowedParameters;
		protected ExpressionTreeOptimizationContext     _optimizationContext = default!;

		public override void Cleanup()
		{
			_canBeEvaluated      = true;
			_inMethod            = false;
			_optimizationContext = default!;

			_allowedParameters?.Clear();

			base.Cleanup();
		}

		public override Expression? Visit(Expression? node)
		{
			if (!_canBeEvaluated)
				return node;

			return base.Visit(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			if (!_inMethod)
			{
				_canBeEvaluated = false;
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
			_canBeEvaluated = false;
			return node;
		}

		internal override Expression VisitSqlErrorExpression(SqlErrorExpression node)
		{
			_canBeEvaluated = false;
			return node;
		}

		public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
		{
			_canBeEvaluated = false;
			return node;
		}

		internal override Expression VisitSqlGenericParamAccessExpression(SqlGenericParamAccessExpression node)
		{
			_canBeEvaluated = false;
			return node;
		}

		internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
		{
			_canBeEvaluated = false;
			return node;
		}

		internal override SqlGenericConstructorExpression.Assignment VisitSqlGenericAssignment(SqlGenericConstructorExpression.Assignment assignment)
		{
			_canBeEvaluated = false;
			return assignment;
		}

		internal override SqlGenericConstructorExpression.Parameter VisitSqlGenericParameter(SqlGenericConstructorExpression.Parameter parameter)
		{
			_canBeEvaluated = false;
			return parameter;
		}

		public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
		{
			_canBeEvaluated = false;
			return node;
		}

		public override Expression VisitSqlQueryRootExpression(SqlQueryRootExpression node)
		{
			_canBeEvaluated = false;
			return node;
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			if (!_canBeEvaluated)
				return node;

			if (_optimizationContext.IsServerSideOnly(node))
				_canBeEvaluated = false;

			var save = _inMethod;
			_inMethod = true;

			base.VisitMethodCall(node);

			_inMethod = save;

			return node;
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
				_canBeEvaluated = false;

			return node;
		}
	}
}
