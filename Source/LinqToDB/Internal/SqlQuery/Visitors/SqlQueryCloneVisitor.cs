using System;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public class SqlQueryCloneVisitor : SqlQueryCloneVisitorBase
	{
		Func<IQueryElement, bool>? _cloneFunc;

		public IQueryElement Clone(IQueryElement element, Func<IQueryElement, bool>? cloneFunc)
		{
			_cloneFunc = cloneFunc;

			return PerformClone(element);
		}

		public override void Cleanup()
		{
			base.Cleanup();
			_cloneFunc = null;
		}

		protected override bool ShouldReplace(IQueryElement element)
		{
			if (element.ElementType == QueryElementType.SqlParameter)
			{
				return false;
			}

			if (_cloneFunc != null)
			{
				return _cloneFunc(element);
			}

			return true;
		}

		protected internal override IQueryElement VisitSqlCteTable(SqlCteTable element)
		{
			if (element.Cte == null || GetReplacement(element.Cte, out var _))
				return base.VisitSqlCteTable(element);

			if (!ShouldReplace(element))
				return element;

			// CTE clause is shared between all CTE tables, so we need to clone it before cloning table itself.

			var newCteFields    = CopyCteFields(element.Cte.Fields);
			var newCteClause = new CteClause(null, newCteFields, element.ObjectType, element.Cte.IsRecursive, element.Cte.Name);

			var newTableFields = CopyCteTableFields(element.Fields);
			var newElement     = new SqlCteTable(element, newTableFields, newCteClause);

			NotifyReplaced(newCteClause, element.Cte);
			NotifyReplaced(newElement,   element);

			var body = Visit(element.Cte.Body);

			// correcting references
			foreach (var cteField in newCteFields)
			{
				cteField.Column = (SqlColumn?)Visit(cteField.Column);
			}

			foreach (var tableField in newTableFields)
			{
				tableField.CteField = (SqlCteField?)Visit(tableField.CteField);
			}

			newCteClause.Body = (SelectQuery?)body;
			newElement.Cte    = newCteClause;

			return newElement;
		}
	}
}
