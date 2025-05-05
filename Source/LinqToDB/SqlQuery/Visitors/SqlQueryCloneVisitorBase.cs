using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery.Visitors
{
	public class SqlQueryCloneVisitorBase : SqlQueryVisitor
	{
		public SqlQueryCloneVisitorBase() : base(VisitMode.Transform, null)
		{
		}

		public void RegisterReplacements(IReadOnlyDictionary<IQueryElement, IQueryElement> replacements)
		{
			AddReplacements(replacements);
		}

		protected override bool ShouldReplace(IQueryElement element)
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
