using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public sealed class QueryElementReplacingVisitor : QueryElementVisitor
	{
		IDictionary<IQueryElement, IQueryElement> _replacements = default!;
		IQueryElement[]                           _toIgnore     = default!;

		public QueryElementReplacingVisitor() : base(VisitMode.Modify)
		{
		}

		public IQueryElement Replace(
			IQueryElement                             element, 
			IDictionary<IQueryElement, IQueryElement> replacements,
			params IQueryElement[]                    toIgnore)
		{
			_replacements = replacements;
			_toIgnore     = toIgnore;

			return Visit(element);
		}

		public void Cleanup()
		{
			_replacements = default!;
			_toIgnore     = default!;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element != null)
			{
				if (_toIgnore.Contains(element))
					return element;

				if (_replacements.TryGetValue(element, out var replacement))
					return replacement;
			}

			return base.Visit(element);
		}

		// CteClause reference not visited by main dispatcher
		protected override IQueryElement VisitCteClauseReference(CteClause element)
		{
			if (_replacements.TryGetValue(element, out var replacement))
				return replacement;

			return base.VisitCteClauseReference(element);
		}

	}
}
