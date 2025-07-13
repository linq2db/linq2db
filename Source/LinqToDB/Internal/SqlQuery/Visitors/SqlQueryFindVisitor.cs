using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public class SqlQueryFindVisitor : QueryElementVisitor
	{
		Func<IQueryElement, bool> _findFunc = default!;
		IQueryElement?            _found;

		public SqlQueryFindVisitor() : base(VisitMode.ReadOnly)
		{
		}

		public IQueryElement? Find(IQueryElement root, Func<IQueryElement, bool> findFunc)
		{
			_findFunc = findFunc;
			_found    = null;

			Visit(root);

			return _found;
		}

		public void Cleanup()
		{
			_found    = null;
			_findFunc = null!;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return null;

			if (_found != null)
				return element;

			if (_findFunc(element))
			{
				_found = element;
				return element;
			}

			return base.Visit(element);
		}
	}
}
