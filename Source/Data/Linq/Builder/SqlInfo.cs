using System;
using System.Reflection;

namespace LinqToDB.Data.Linq.Builder
{
	using Data.Sql;

	public class SqlInfo
	{
		public ISqlExpression Sql;
		public SqlQuery       Query;
		public int            Index = -1;
		public MemberInfo     Member;
	}
}
