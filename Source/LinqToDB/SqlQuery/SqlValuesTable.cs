using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using Mapping;

	public class SqlValuesTable : SqlTable
	{
		public SqlValuesTable(MappingSchema mappingSchema, Type objectType, string name, ISqlExpression sqlExpression)
			: base(mappingSchema, objectType, name)
		{
			SqlExpression = sqlExpression;
		}

		public          ISqlExpression   SqlExpression { get; }
		public override QueryElementType ElementType  => QueryElementType.SqlValuesTable;
		public override SqlTableType     SqlTableType => SqlTableType.Values;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb.Append(Name);
		}

		#region IQueryElement Members

		public string SqlText =>
			((IQueryElement) this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
			.ToString();

		#endregion
	}
}
