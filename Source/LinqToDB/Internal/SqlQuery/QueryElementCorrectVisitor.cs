using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.SqlQuery
{
	public class QueryElementCorrectVisitor : QueryElementVisitor
	{
		readonly QueryElementVisitor _visitor;
		readonly IQueryElement       _toReplace;
		readonly IQueryElement       _replaceBy;

		public QueryElementCorrectVisitor(VisitMode visitMode, QueryElementVisitor visitor, IQueryElement toReplace, IQueryElement replaceBy) : base(visitMode)
		{
			_visitor   = visitor;
			_toReplace = toReplace;
			_replaceBy = replaceBy;
		}

		public override IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement)
		{
			return _visitor.NotifyReplaced(newElement, oldElement);
		}

		public override VisitMode GetVisitMode(IQueryElement element)
		{
			return _visitor.GetVisitMode(element);
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (ReferenceEquals(element, _toReplace))
			{
				return _replaceBy;
			}

			return base.Visit(element);
		}
	}

}
