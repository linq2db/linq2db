using System;
using System.Reflection;

using JetBrains.Annotations;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	using Extensions;
	using SqlQuery;

	partial class Sql
	{
		[PublicAPI]
		[Serializable]
		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
		public class FunctionAttribute : ExpressionAttribute
		{
			public FunctionAttribute()
				: base(null)
			{
			}

			public FunctionAttribute(string name)
				: base(name)
			{
			}

			public FunctionAttribute(string name, params int[] argIndices)
				: base(name, argIndices)
			{
			}

			public FunctionAttribute(string configuration, string name)
				: base(configuration, name)
			{
			}

			public FunctionAttribute(string configuration, string name, params int[] argIndices)
				: base(configuration, name, argIndices)
			{
			}

			public string Name
			{
				get { return Expression;  }
				set { Expression = value; }
			}

			public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				return new SqlFunction(member.GetMemberType(), Name ?? member.Name, ConvertArgs(member, args)) { CanBeNull = CanBeNull };
			}
		}
	}
}
