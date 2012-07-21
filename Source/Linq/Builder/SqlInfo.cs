using System;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using SqlBuilder;

	public class SqlInfo
	{
		public ISqlExpression Sql;
		public SqlQuery       Query;
		public int            Index = -1;
		public MemberInfo     Member;
	}
}
