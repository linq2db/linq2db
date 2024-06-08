using System;

namespace LinqToDB.SqlQuery
{
	using Mapping;

	public class SqlGetValue
	{
		public SqlGetValue(ISqlExpression sql, Type valueType, ColumnDescriptor? columnDescriptor, Func<object, object>? getValueFunc)
		{
			Sql              = sql;
			ValueType        = valueType;
			ColumnDescriptor = columnDescriptor;
			GetValueFunc     = getValueFunc;
		}

		public ISqlExpression        Sql              { get; }
		public Type                  ValueType        { get; }
		public ColumnDescriptor?     ColumnDescriptor { get; }
		public Func<object, object>? GetValueFunc     { get; }

		public SqlGetValue WithSql(ISqlExpression sql)
		{
			if (ReferenceEquals(sql, Sql))
				return this;

			return new SqlGetValue(sql, ValueType, ColumnDescriptor, GetValueFunc);
		}
	}
}
