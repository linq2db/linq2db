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

		protected override IQueryElement VisitCteClause(CteClause element)
		{
			var newCte = new CteClause(element.ObjectType, element.IsRecursive, element.Name);

			// avoid recursive CTE stack overflow
			NotifyReplaced(newCte, element);

			var body         = (SelectQuery?)Visit(element.Body);
			var clonedFields = CopyFields(element.Fields);

			newCte.Init(body, clonedFields);

			return newCte;
		}

		protected override IQueryElement VisitSqlCteTable(SqlCteTable element)
		{
			var clause = element.Cte != null ? (CteClause)Visit(element.Cte) : null;

			var ext = VisitElements(element.SqlQueryExtensions, VisitMode.Transform);

			var newFields = CopyFields(element.Fields);
			var newTable  = new SqlCteTable(element, newFields, clause)
			{
				SqlQueryExtensions = element.SqlQueryExtensions != ext ? ext : ext?.ToList()
			};

			return NotifyReplaced(newTable, element);
		}
	}
}
