using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class SelectClause : BaseClause, IQuerySource
	{
		public Expression Selector { get; set; }

		public SelectClause([NotNull] Type itemType, [NotNull] string itemName, [NotNull] Expression selector)
		{
			Selector = selector ?? throw new ArgumentNullException(nameof(selector));
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
		}

		public SelectClause([NotNull] Expression selector) : this(selector.Type, "", selector)
		{
			
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

		public ISqlExpression ConvertToSql(ISqlTableSource tableSource, Expression ma)
		{
			throw new NotImplementedException();
		}
	}
}
