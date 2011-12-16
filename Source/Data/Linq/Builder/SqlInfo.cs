using System;
using System.Reflection;

using LinqToDB.Sql;

namespace LinqToDB.Data.Linq.Builder
{
	public class SqlInfo
	{
		public ISqlExpression Sql;
		public SqlQuery       Query;
		public int            Index = -1;
		public MemberInfo     Member;
	}
}
