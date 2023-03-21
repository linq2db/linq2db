using System.Collections.Generic;

namespace LinqToDB.SqlQuery.Visitors
{
	public class SqlQueryCloneVisitorBase : SqlQueryVisitor
	{
		public SqlQueryCloneVisitorBase() : base(VisitMode.Transform)
		{
		}

		public void RegisterReplacements(IReadOnlyDictionary<IQueryElement, IQueryElement> replacements)
		{
			AddReplacements(replacements);
		}

		public override IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement)
		{
			AddReplacement(oldElement, newElement);
			return base.NotifyReplaced(newElement, oldElement);
		}

		public override bool ShouldReplace(IQueryElement element)
		{
			if (base.ShouldReplace(element))
				return true;

			if (element.ElementType == QueryElementType.SqlParameter)
				return false;

			return true;
		}

		public IQueryElement PerformClone(IQueryElement element)
		{
			return ProcessElement(element);
		}
	}
}
