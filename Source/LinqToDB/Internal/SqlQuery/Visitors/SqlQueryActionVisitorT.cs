using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Common;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public class SqlQueryActionVisitor<TContext> : QueryElementVisitor
	{
		TContext                        _context     = default!;
		Action<TContext, IQueryElement> _visitAction = default!;
		HashSet<IQueryElement>?         _visited;

		public SqlQueryActionVisitor() : base(VisitMode.ReadOnly)
		{
		}

		public IQueryElement Visit(TContext context, IQueryElement root, bool visitAll, Action<TContext, IQueryElement> visitAction)
		{
			_context     = context;
			_visitAction = visitAction;

			_visited = visitAll ? null : new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

			return Visit(root);
		}

		public void Cleanup()
		{
			_visitAction = null!;
			_visited     = null;
			_context     = default!;
		}

		[return: NotNullIfNotNull(nameof(element))]
		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return null;

			if (_visited != null && _visited.Contains(element))
			{
				return element;
			}

			var result = base.Visit(element);

			_visitAction(_context, element);
			_visited?.Add(element);

			return result;
		}
	}
}
