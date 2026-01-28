using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public class SqlQueryFindVisitor<TContext> : QueryElementVisitor
	{
		TContext                            _context  = default!;
		Func<TContext, IQueryElement, bool> _findFunc = default!;
		IQueryElement?                      _found;

		public SqlQueryFindVisitor() : base(VisitMode.ReadOnly)
		{
		}

		public IQueryElement? Find(TContext context, IQueryElement root, Func<TContext, IQueryElement, bool> findFunc)
		{
			_context  = context;
			_findFunc = findFunc;
			_found    = null;

			Visit(root);

			return _found;
		}

		public override void Cleanup()
		{
			_found    = null;
			_findFunc = null!;
			_context  = default!;

			base.Cleanup();
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return null;

			if (_found != null)
				return element;

			if (_findFunc(_context, element))
			{
				_found = element;
				return element;
			}

			return base.Visit(element);
		}
	}
}
