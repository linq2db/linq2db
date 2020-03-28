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

			public PropertyAttribute(string configuration, string name)
				: base(configuration, name)
			{
			}

			public string? Name
			{
				get => Expression;
				set => Expression = value;
			}

			public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				return new SqlExpression(member.GetMemberType(), Name ?? member.Name, SqlQuery.Precedence.Primary)
				{
					CanBeNull = GetCanBeNull(new ISqlExpression[0])
				};
			}
		}
	}
}
