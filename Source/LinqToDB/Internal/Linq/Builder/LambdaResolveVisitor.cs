using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	sealed class LambdaResolveVisitor : ExpressionVisitorBase
	{
		readonly IBuildContext _context;
		readonly BuildPurpose  _buildPurpose;
		readonly bool          _includingEager;
		bool                   _inLambda;

		public ExpressionBuilder Builder => _context.Builder;

		public LambdaResolveVisitor(IBuildContext context, BuildPurpose buildPurpose, bool includingEager)
		{
			_context        = context;
			_buildPurpose   = buildPurpose;
			_includingEager = includingEager;
		}

		internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
		{
			if (_includingEager)
				return base.VisitSqlEagerLoadExpression(node);
			return node;
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			if (_inLambda)
			{
				if (null != node.Find(e => e is ContextRefExpression))
				{
					var expanded = Builder.BuildExpandExpression(node);
					if (!ExpressionEqualityComparer.Instance.Equals(expanded, node))
					{
						return Visit(expanded);
					}

					var expr = Builder.BuildSqlExpression(_context, node, _buildPurpose, BuildFlags.None);

					if (expr is SqlPlaceholderExpression)
						return expr;
				}

				return node;
			}

			return base.VisitMember(node);
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			if (_inLambda)
			{
				if (null != node.Find(e => e is ContextRefExpression))
				{
					var expanded = Builder.BuildExpandExpression(node);
					if (!ExpressionEqualityComparer.Instance.Equals(expanded, node))
					{
						return Visit(expanded);
					}

					var expr = Builder.BuildSqlExpression(_context, node, _buildPurpose, BuildFlags.None);

					if (expr is SqlPlaceholderExpression)
						return expr;
				}

				return node;
			}

			return base.VisitMethodCall(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			var save = _inLambda;
			_inLambda = true;

			var newNode = base.VisitLambda(node);

			_inLambda = save;

			return newNode;
		}
	}
}
