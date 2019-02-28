using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class ProjectionClause : BaseClause, IQuerySource
	{
		public Expression ProjectionExpression { get; }

		public ProjectionClause([NotNull] Type itemType, string itemName, [NotNull] Expression projectionExpression)
		{
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			ItemName = itemName;
			ProjectionExpression = projectionExpression ?? throw new ArgumentNullException(nameof(projectionExpression));
		}

		public Type ItemType { get; }
		public string ItemName { get; }

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			return func(this);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this);
		}

		public ISqlExpression ConvertToSql(ISqlTableSource tableSource, MemberExpression ma)
		{
			throw new NotImplementedException();
		}

	}
}
