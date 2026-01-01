using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	/// <summary>
	/// Search for element in query using search condition predicate.
	/// Do not visit provided element.
	/// </summary>
	public class SqlQueryFindExceptVisitor<TContext> : QueryElementVisitor
	{
		TContext                            _context  = default!;
		Func<TContext, IQueryElement, bool> _findFunc = default!;
		IQueryElement                       _skip     = default!;
		IQueryElement?                      _found;

		public SqlQueryFindExceptVisitor() : base(VisitMode.ReadOnly)
		{
		}

		public IQueryElement? Find(TContext context, IQueryElement root, IQueryElement skip, Func<TContext, IQueryElement, bool> findFunc)
		{
			_context  = context;
			_findFunc = findFunc;
			_skip     = skip;
			_found    = null;

			Visit(root);

			return _found;
		}

		public override void Cleanup()
		{
			_context  = default!;
			_findFunc = null!;
			_skip     = null!;
			_found    = null;

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

			if (ReferenceEquals(_skip, element))
				return element;

			return base.Visit(element);
		}
	}
}
