using System;
using System.Linq.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class ArrayQueryClause : BaseClause, IQuerySource
	{
		public ArrayQueryClause(Type itemType, string itemName, object values)
		{
			ItemType = itemType;
			ItemName = itemName;
			Values = values;
		}

		public Type ItemType { get; }
		public string ItemName { get; }

		public object Values { get; }

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			return func(this);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this);
		}

		public ISqlExpression ConvertToSql(ISqlTableSource tableSource, Expression ma)
		{
			throw new NotImplementedException();
		}
	}
}
