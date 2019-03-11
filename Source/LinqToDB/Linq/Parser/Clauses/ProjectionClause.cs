using System;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class ProjectionClause : BaseClause, IQuerySource
	{
		public Expression ProjectionExpression { get; }

		public ProjectionClause([JetBrains.Annotations.NotNull] Type itemType, string itemName, [JetBrains.Annotations.NotNull] Expression projectionExpression)
		{
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			ItemName = itemName;
			ProjectionExpression = projectionExpression ?? throw new ArgumentNullException(nameof(projectionExpression));
			QuerySourceId = QuerySourceHelper.GetNexSourceId();
		}

		public int QuerySourceId { get; }
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

		public bool DoesContainMember(MemberInfo memberInfo, MappingSchema mappingSchema)
		{
			throw new NotImplementedException();
		}

		public ISqlExpression ConvertToSql(ISqlTableSource tableSource, Expression ma)
		{
			throw new NotImplementedException();
		}

	}
}
