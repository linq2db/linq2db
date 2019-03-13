using System;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Linq.Parser;
using LinqToDB.Linq.Parser.Clauses;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Generator
{
	public class SetQuerySource : IQuerySource
	{
		public BaseSetClause QuerySource { get; }
		public int QuerySourceId { get; }
		public Type ItemType => QuerySource.ItemType;
		public string ItemName => QuerySource.ItemName;

		public SetQuerySource([JetBrains.Annotations.NotNull] BaseSetClause querySource)
		{
			QuerySource = querySource ?? throw new ArgumentNullException(nameof(querySource));

			QuerySourceId = QuerySourceHelper.GetNexSourceId();
		}

		public bool DoesContainMember(MemberInfo memberInfo, MappingSchema mappingSchema)
		{
			throw new NotImplementedException();
		}

		public ISqlExpression ConvertToSql(ISqlTableSource tableSource, Expression expression)
		{
			throw new NotImplementedException();
		}
	}
}
