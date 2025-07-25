using System;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public class SqlQueryCloneVisitor<TContext> : SqlQueryCloneVisitorBase
	{
		TContext                            _context   = default!;
		Func<TContext, IQueryElement, bool> _cloneFunc = default!;

		public IQueryElement Clone(IQueryElement element, TContext context, Func<TContext, IQueryElement, bool> cloneFunc)
		{
			_context   = context;
			_cloneFunc = cloneFunc;

			return PerformClone(element);
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_cloneFunc = null!;
			_context   = default!;
		}

		protected override bool ShouldReplace(IQueryElement element)
		{
			if (base.ShouldReplace(element) && _cloneFunc(_context, element))
			{
				return true;
			}

			return false;
		}

	}
}
