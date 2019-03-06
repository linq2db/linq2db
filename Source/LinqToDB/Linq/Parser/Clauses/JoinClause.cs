using System;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class JoinClause : BaseClause, IQuerySource
	{
		public IQuerySource Inner { get; }
		[NotNull]
		public Expression Condition { get; }
		public Expression OuterKeySelector { get; }
		public Expression InnerKeySelector { get; }

		public JoinClause([NotNull] string itemName, [NotNull] Type itemType, [NotNull] IQuerySource inner,
			[NotNull] Expression outerKeySelector,
			[NotNull] Expression innerKeySelector)
		{
			ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			Inner = inner ?? throw new ArgumentNullException(nameof(inner));
			OuterKeySelector = outerKeySelector ?? throw new ArgumentNullException(nameof(outerKeySelector));
			InnerKeySelector = innerKeySelector ?? throw new ArgumentNullException(nameof(innerKeySelector));
			Condition = Expression.Equal(OuterKeySelector, InnerKeySelector);
			QuerySourceId = QuerySourceHelper.GetNexSourceId();
		}

		public JoinClause([NotNull] string itemName, [NotNull] Type itemType, [NotNull] IQuerySource inner,
			[NotNull] Expression condition)
		{
			ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			Inner = inner ?? throw new ArgumentNullException(nameof(inner));
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
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

		public bool DoesContainMember(MemberInfo memberInfo)
		{
			throw new NotImplementedException();
		}

		public ISqlExpression ConvertToSql(ISqlTableSource tableSource, Expression ma)
		{
			return Inner.ConvertToSql(tableSource, ma);
		}

	}
}
