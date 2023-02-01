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

		public IQueryElement PerformClone(IQueryElement element)
		{
			return ProcessElement(element);
		}
	}
}
