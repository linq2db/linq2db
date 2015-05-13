using System;
using System.Reflection;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	using Extensions;
	using SqlQuery;

	partial class Sql
	{
		[Serializable]
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class PropertyAttribute : FunctionAttribute
		{
			public PropertyAttribute()
			{
			}

			public PropertyAttribute(string name)
				: base(name)
			{
			}

			public PropertyAttribute(string sqlProvider, string name)
				: base(sqlProvider, name)
			{
			}

			public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				return new SqlExpression(member.GetMemberType(), Name ?? member.Name, PrecedenceLevel.Primary);
			}
		}
	}
}
