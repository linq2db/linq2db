using System.Linq.Expressions;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Read-only validation pass for the <c>GuardImplicitEagerLoading</c> option. Throws
	/// <see cref="LinqToDBException"/> when a <see cref="SqlEagerLoadExpression"/> is reached that is not
	/// inside a <see cref="MarkerType.ExplicitEagerLoad"/> marker subtree — i.e. an implicit collection
	/// projection rather than an explicit <c>LoadWith</c>/<c>ThenLoad</c> load. Everything within a marker
	/// is treated as explicit, including a collection nested in a <c>LoadWith</c>/<c>ThenLoad</c>
	/// load-function's complex select (the user wrote that projection deliberately).
	/// </summary>
	sealed class ImplicitEagerLoadGuardVisitor : ExpressionVisitorBase
	{
		bool _allowNextEagerLoad;

		public override Expression VisitMarkerExpression(MarkerExpression node)
		{
			if (node.MarkerType != MarkerType.ExplicitEagerLoad)
				return base.VisitMarkerExpression(node);

			var save = _allowNextEagerLoad;
			_allowNextEagerLoad = true;
			var result = base.VisitMarkerExpression(node);
			_allowNextEagerLoad = save;
			return result;
		}

		internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
		{
			if (!_allowNextEagerLoad)
				throw new LinqToDBException(ErrorHelper.Error_ImplicitEagerLoadingNotAllowed);

			// Everything inside an ExplicitEagerLoad marker subtree is explicit — including collections
			// nested in a LoadWith/ThenLoad load-function's complex select — so the flag stays set while
			// descending into the sequence.
			return base.VisitSqlEagerLoadExpression(node);
		}
	}
}
