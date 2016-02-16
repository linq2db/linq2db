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
		public class PropertyAttribute : ExpressionAttribute
		{
			public PropertyAttribute()
				: base(null)
			{
			}

			public PropertyAttribute(string name)
				: base(name)
			{
			}

			public PropertyAttribute(string configuraion, string name)
				: base(configuraion, name)
			{
			}

			public string Name
			{
				get { return Expression;  }
				set { Expression = value; }
			}

			public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				return new SqlExpression(member.GetMemberType(), Name ?? member.Name, SqlQuery.Precedence.Primary) { CanBeNull = CanBeNull };
			}
		}
	}
}
