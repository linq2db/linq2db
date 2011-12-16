using System;
using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.Sql;

namespace LinqToDB.Data.Linq
{
	[SerializableAttribute]
	[AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class SqlPropertyAttribute : SqlFunctionAttribute
	{
		public SqlPropertyAttribute()
		{
		}

		public SqlPropertyAttribute(string name)
			: base(name)
		{
		}

		public SqlPropertyAttribute(string sqlProvider, string name)
			: base(sqlProvider, name)
		{
		}

		public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
		{
			return new SqlExpression(member.GetMemberType(), Name ?? member.Name, Precedence.Primary);
		}
	}
}
