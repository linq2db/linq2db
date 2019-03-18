using System;
using System.Linq.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class JoinClause : BaseClause
	{
		public IQuerySource Inner { get; }
		[JetBrains.Annotations.NotNull]
		public Expression Condition { get; }
		public Expression OuterKeySelector { get; }
		public Expression InnerKeySelector { get; }
		public JoinType JoinType { get; }

		public JoinClause([JetBrains.Annotations.NotNull] string itemName, [JetBrains.Annotations.NotNull] Type itemType, [JetBrains.Annotations.NotNull] IQuerySource inner,
			[JetBrains.Annotations.NotNull] Expression outerKeySelector,
			[JetBrains.Annotations.NotNull] Expression innerKeySelector,
			JoinType joinType)
		{
			ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			Inner = inner ?? throw new ArgumentNullException(nameof(inner));
			OuterKeySelector = outerKeySelector ?? throw new ArgumentNullException(nameof(outerKeySelector));
			InnerKeySelector = innerKeySelector ?? throw new ArgumentNullException(nameof(innerKeySelector));
			JoinType = joinType;
			Condition = Expression.Equal(OuterKeySelector, InnerKeySelector);

			QuerySourceId = QuerySourceHelper.GetNexSourceId();
		}

		public JoinClause([JetBrains.Annotations.NotNull] string itemName, [JetBrains.Annotations.NotNull] Type itemType, [JetBrains.Annotations.NotNull] IQuerySource inner,
			[JetBrains.Annotations.NotNull] Expression condition, JoinType joinType)
		{
			ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			Inner = inner ?? throw new ArgumentNullException(nameof(inner));
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
			JoinType = joinType;

			QuerySourceId = QuerySourceHelper.GetNexSourceId();
		}
		
		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			return func(this);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this);
		}

		public int QuerySourceId { get; }
		public Type ItemType { get; }
		public string ItemName { get; }

	}
}
