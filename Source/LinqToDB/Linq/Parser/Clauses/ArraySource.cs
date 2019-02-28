using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class ArraySource : BaseClause, IQuerySource
	{
		public ArraySource([NotNull] Type itemType, string itemName, [NotNull] Expression arrayExpression)
		{
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			ItemName = itemName;
			ArrayExpression = arrayExpression ?? throw new ArgumentNullException(nameof(arrayExpression));
		}

		public Type ItemType { get; }
		public string ItemName { get; }
		public Expression ArrayExpression { get; }

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
