using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class JoinClause : BaseClause, IQuerySource
	{
		public IQuerySource Outer { get; }
		public Expression OuterKeySelector { get; }
		public Expression InnerKeySelector { get; }

		public JoinClause([NotNull] string itemName, [NotNull] Type itemType, [NotNull] IQuerySource outer,
			[NotNull] Expression outerKeySelector,
			[NotNull] Expression innerKeySelector)
		{
			ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			Outer = outer ?? throw new ArgumentNullException(nameof(outer));
			OuterKeySelector = outerKeySelector ?? throw new ArgumentNullException(nameof(outerKeySelector));
			InnerKeySelector = innerKeySelector ?? throw new ArgumentNullException(nameof(innerKeySelector));
		}
		
		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			return func(this);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this);
		}

		public Type ItemType { get; }
		public string ItemName { get; }

		public ISqlExpression ConvertToSql(ISqlTableSource tableSource, MemberExpression ma)
		{
			throw new NotImplementedException();
		}

	}
}
