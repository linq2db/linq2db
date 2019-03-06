using System;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class TableSource : BaseClause, IQuerySource
	{
		public TableSource([NotNull] Type itemType, string itemName)
		{
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			ItemName = itemName;
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

		public bool DoesContainMember(MemberInfo memberInfo)
		{
			throw new NotImplementedException();
		}

		public ISqlExpression ConvertToSql(ISqlTableSource tableSource, Expression expression)
		{
			var table = (SqlTable)tableSource;

			var ma = (MemberExpression)expression;
			SqlField field;
			if (!table.Fields.TryGetValue(ma.Member.Name, out field))
				throw new LinqToDBException($"Can not find field for expression '{ma}'");

			return field;
		}

	}
}
